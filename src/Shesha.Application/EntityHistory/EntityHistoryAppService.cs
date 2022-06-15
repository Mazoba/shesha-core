using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Configuration;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.EntityHistory;
using Abp.Events.Bus.Entities;
using Abp.ObjectMapping;
using Abp.Reflection;
using Microsoft.AspNetCore.Mvc;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Intercept;
using NHibernate.Proxy;
using Shesha.Domain;
using Shesha.Domain.Attributes;
using Shesha.Extensions;
using Shesha.NHibernate.Session;
using Shesha.Reflection;
using Shesha.Services;
using Shesha.Utilities;
using Shesha.Web.DataTable;

namespace Shesha.EntityHistory
{
    /// <summary>
    /// Entity history application service
    /// </summary>
    public class EntityHistoryAppService : ApplicationService, IEntityHistoryAppService
    {

        private readonly IRepository<Person, Guid> PersonRepository;
        private readonly IDynamicRepository DynamicRepository;
        private readonly IRepository<EntityHistoryEvent, Guid> EventRepository;
        private readonly IRepository<EntityChangeSet, long> EntityChangeSetRepository;
        private readonly IRepository<EntityChange, long> EntityChangeRepository;
        private readonly IRepository<EntityPropertyChange, long> EntityPropertyChangeRepository;
        private readonly IRepository<EntityHistoryItem, long> EntityHistoryItemRepository;

        private readonly IRepository<Setting, long> _settingRepository;

        private readonly IObjectMapper Mapper;
        private readonly ISessionFactory SessionFactory;
        private readonly ITypeFinder TypeFinder;

        public EntityHistoryAppService(
            IRepository<Person, Guid> personRepository,
            IDynamicRepository dynamicRepository,
            IRepository<EntityHistoryEvent, Guid> eventRepository,
            IRepository<EntityChangeSet, long> entityChangeSetRepository,
            IRepository<EntityChange, long> entityChangeRepository,
            IRepository<EntityPropertyChange, long> entityPropertyChangeRepository,
            IRepository<EntityHistoryItem, long> entityHistoryItemRepository,
            IRepository<Setting, long> settingRepository,
            IObjectMapper mapper,
            ITypeFinder typeFinder,
            ISessionFactory sessionFactory)
        {
            PersonRepository = personRepository;
            DynamicRepository = dynamicRepository;
            EventRepository = eventRepository;
            EntityChangeSetRepository = entityChangeSetRepository;
            EntityPropertyChangeRepository = entityPropertyChangeRepository;
            EntityChangeRepository = entityChangeRepository;
            EntityHistoryItemRepository = entityHistoryItemRepository;
            _settingRepository = settingRepository;
            Mapper = mapper;
            TypeFinder = typeFinder;
            SessionFactory = sessionFactory;
        }

        /// <summary>
        /// Custom Index table configuration 
        /// </summary>
        public static DataTableConfig IndexTableFull()
        {
            var table = new DataTableConfig<EntityHistoryItemDto, int>("EntityHistoryFull_Index");

            table.AddProperty(e => e.EventText, p => p.WidthPixels(185));
            table.AddProperty(e => e.ExtendedDescription);
            table.AddProperty(e => e.UserFullName, p => p.WidthPixels(130));
            table.AddProperty(e => e.CreationTime, p => p.WidthPixels(160));
            table.AddProperty(e => e.EntityTypeFullName, c => c.Visible(false).AllowShowHide(false));
            table.AddProperty(e => e.EntityId, c => c.Visible(false).AllowShowHide(false));

            return table;
        }

        /// <summary>
        /// Returns data for the DateTable control
        /// </summary>
        [HttpPost]
        public async Task<DataTableData> GetData(DataTableGetDataInput input, CancellationToken cancellationToken)
        {
            // disable SoftDeleteFilter to allow get deleted entities
            CurrentUnitOfWork.DisableFilter(AbpDataFilters.SoftDelete);

            var tableConfig = IndexTableFull();
            var entityId = input.Filter.FirstOrDefault(f => f.RealPropertyName == "EntityId")?.Filter.ToString();
            var entityTypeFullName =
                input.Filter.FirstOrDefault(f => f.RealPropertyName == "EntityTypeFullName")?.Filter.ToString();

            var itemType = TypeFinder.Find(t => t.FullName == entityTypeFullName)?.FirstOrDefault();

            var history = new List<EntityHistoryItemDto>();

            var maxDate = DateTime.MaxValue;

            // Add entity history
            history.AddRange(GetEntityAudit(itemType, entityId, out maxDate));

            // Add many-to-many related entities
            history.AddRange(GetManyToManyEntitiesAudit(itemType, entityId));

            // Add many-to-one related entities
            history.AddRange(GetManyToOneEntitiesAudit(itemType, entityId));
            
            // Add child audited properties
            history.AddRange(GetChildEntitiesAudit(itemType, entityId));

            // Add generic child entities
            history.AddRange(GetGenericEntitiesAudit(itemType, entityId));

            if (maxDate != DateTime.MaxValue)
            {
                history = history.Where(x => x.CreationTime <= maxDate).ToList();
            }

            var totalRowsBeforeFilter = history.Count();

            // Dynamic filter
            if (!string.IsNullOrEmpty(input.QuickSearch))
            {
                var properties = tableConfig.AuthorizedColumns.Where(x => x.IsVisible).Select(x => x.FilterPropertyName)
                    .ToArray();
                if (properties.Length > 0)
                {
                    history = history.LikeDynamic(properties, input.QuickSearch).ToList();
                }
            }

            var totalRows = history.Count();

            var totalPages = (int)Math.Ceiling((double)history.Count() / input.PageSize);

            var takeCount = input.PageSize > -1 ? input.PageSize : int.MaxValue;
            var skipCount = Math.Max(0, (input.CurrentPage - 1) * takeCount);

            history = history.OrderBy(x => x.CreationTime).ToList();

            // Dynamic order by property name
            var sort = input.Sorting.FirstOrDefault();
            if (sort != null)
            {
                history = history.OrderByDynamic(sort.Id, sort.Desc ? "desc" : "asc").ToList();
            }

            if (skipCount > history.Count) skipCount = 0;

            history = history.Skip(skipCount).Take(takeCount).ToList();

            var dataRows = new List<Dictionary<string, object>>();
            var authorizedColumns =
                tableConfig.Columns //.Where(c => c.AuthorizationRules == null || c.AuthorizationRules.IsAuthorized())
                    .ToList();

            foreach (var item in history)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var row = new Dictionary<string, object>();
                foreach (var col in authorizedColumns)
                {
                    var value = item.GetType().GetProperty(col.PropertyName)?.GetValue(item);
                    row.Add(col.PropertyName, value);
                }

                dataRows.Add(row);
            }

            var result = new DataTableData
            {
                TotalRowsBeforeFilter = totalRowsBeforeFilter,
                TotalRows = totalRows,
                TotalPages = totalPages,
                Rows = dataRows
            };

            return result;
        }

        private List<EntityHistoryItemDto> GetChildEntitiesAudit(Type itemType, string entityId)
        {
            var list = new List<EntityHistoryItemDto>();
            // Check if child audited properties should be displayed
            var childAuditedProperties = itemType?.GetProperties()
                .Where(p => p.GetCustomAttribute<DisplayChildAuditTrailAttribute>() != null).ToList();

            if (childAuditedProperties?.Any() ?? false)
            {
                var item = DynamicRepository.Get(itemType, entityId);
                foreach (var childAuditedProperty in childAuditedProperties)
                {
                    var childItem = childAuditedProperty.GetValue(item);
                    if (childItem != null)
                    {
                        var attr = childAuditedProperty.GetCustomAttribute<DisplayChildAuditTrailAttribute>();
                        var propDisplayName = childAuditedProperty.GetCustomAttribute<DisplayAttribute>()?.Name ??
                                              childAuditedProperty.Name.ToFriendlyName();
                        var childType = childItem.GetType();
                        if (childType.HasInterface(typeof(INHibernateProxy)) ||
                            childType.HasInterface(typeof(IFieldInterceptorAccessor)))
                        {
                            if (childType.BaseType != null)
                            {
                                // unproxy
                                childType = childType.BaseType;
                            }
                        }

                        var childId = childType.GetProperty("Id")?.GetValue(childItem)?.ToString();
                        list.AddRange(GetEntityAudit(childType, childId, propDisplayName, attr.AuditedFields));
                    }
                }
            }

            return list;
        }

        private List<EntityHistoryItemDto> GetEntityAudit(Type entityType, string entityId, string childName = "", string[] fields = null)
        {
            var fakeDate = DateTime.MaxValue;
            return GetEntityAudit(entityType, entityId, out fakeDate, childName, fields);
        }

        private List<EntityHistoryItemDto> GetEntityAudit(Type entityType, string entityId, out DateTime maxDateTime, string childName = "", string[] fields = null)
        {

            maxDateTime = DateTime.MaxValue;
            var stopsAttrs = entityType?.GetCustomAttributes<PropertyChangeToStopAuditTrailAttribute>()
                .ToDictionary(x => x.PropertyName, x => x.PropertyValue)
                ?? new Dictionary<string, string>();
            
            var list = new List<EntityHistoryItemDto>();
            List<EntityChange> changes;
            if (entityType != null)
            {
                changes = EntityChangeRepository.GetAllList(x =>
                    x.EntityId == entityId && x.EntityTypeFullName == entityType.FullName);
            }
            else
            {
                changes = EntityChangeRepository.GetAllList(x => x.EntityId == entityId);
            }

            foreach (var entityChange in changes)
            {
                var changeSet = EntityChangeSetRepository.Get(entityChange.EntityChangeSetId);
                var username = GetPersonByUserId(changeSet?.UserId);

                entityType ??= TypeFinder.Find(t => t.FullName == entityChange.EntityTypeFullName)?.FirstOrDefault();

                var changeEvents = EventRepository.GetAllList(x => x.EntityPropertyChange == null && x.EntityChange == entityChange);
                foreach (var entityHistoryEvent in changeEvents)
                {
                    
                    if (fields != null && !fields.Contains(entityHistoryEvent.EventType)) continue;
                    
                    list.Add(new EntityHistoryItemDto()
                    {
                        HistoryItemType = (int?)EntityHistoryItemType.Event,
                        CreationTime = entityChange.ChangeTime,
                        EntityId = entityId,
                        EntityTypeFullName = entityType?.FullName,
                        EventType = entityHistoryEvent.EventType,
                        EventText = (entityHistoryEvent.EventName ?? $"{childName} Event").Trim(),
                        ExtendedDescription = entityHistoryEvent.Description,
                        UserFullName = username?.FullName ?? $"UserId: {changeSet?.UserId}"
                    });
                }

                var properties = EntityPropertyChangeRepository.GetAllList(x => x.EntityChangeId == entityChange.Id);

                var propsDescr = new List<string>();
                foreach (var propertyChange in properties)
                {
                    if (fields != null && !fields.Contains(propertyChange.PropertyName)) continue;

                    var propertyEvents = EventRepository.GetAllList(x => x.EntityPropertyChange == propertyChange);
                    var propDescription = "";

                    var propAsEvent = propertyEvents.FirstOrDefault(x =>
                        x.EventType == EntityHistoryCommonEventTypes.PROPERTY_CHANGE_AS_EVENT);
                    if (propAsEvent != null)
                    {
                        // Add separate event
                        list.Add(new EntityHistoryItemDto()
                        {
                            HistoryItemType = (int?)EntityHistoryItemType.Updated,
                            CreationTime = entityChange.ChangeTime,
                            EntityId = entityId,
                            EntityTypeFullName = entityType?.FullName,
                            EventType = propAsEvent.EventType,
                            EventText = (propAsEvent.EventName ?? $"{childName} Update").Trim(),
                            ExtendedDescription = propAsEvent.Description,
                            UserFullName = username?.FullName ?? $"UserId: {changeSet?.UserId}"
                        });

                        // To the next property
                        continue;
                    }

                    var propAsDescription = propertyEvents.FirstOrDefault(x =>
                        x.EventType == EntityHistoryCommonEventTypes.PROPERTY_CHANGE_FRIENDLY_TEXT
                        || x.EventType == EntityHistoryCommonEventTypes.PROPERTY_CHANGE_USER_TEXT);
                    if (propAsDescription != null)
                    {
                        propsDescr.Add(propAsDescription.Description);
                        continue;
                    }

                    var prop_ = entityType?.GetProperty(propertyChange.PropertyName);
                    var propName = prop_ != null 
                        ? ReflectionHelper.GetDisplayName(entityType.GetProperty(propertyChange.PropertyName)) ?? propertyChange.PropertyName
                        : propertyChange.PropertyName;

                    propDescription =
                        $"`{propName}` was changed from {propertyChange.OriginalValue} to {propertyChange.NewValue}";

                    foreach (var propEvent in propertyEvents.Where(x =>
                        x.EventType != EntityHistoryCommonEventTypes.PROPERTY_CHANGE_FRIENDLY_TEXT
                        && x.EventType != EntityHistoryCommonEventTypes.PROPERTY_CHANGE_USER_TEXT
                        && x.EventType != EntityHistoryCommonEventTypes.PROPERTY_CHANGE_AS_EVENT
                        ))
                    {
                        propDescription = propDescription +
                            (!string.IsNullOrEmpty(propEvent.Description)
                                ? $"({propEvent.Description})"
                                : "");
                    }

                    propsDescr.Add(propDescription);

                    if (stopsAttrs.Count > 0 
                        && stopsAttrs.GetValueOrDefault(propertyChange.PropertyName, "") == propertyChange.NewValue.Trim('"').Trim('\'').Trim('`')
                        && (
                            maxDateTime == DateTime.MaxValue
                            || maxDateTime > entityChange.ChangeTime
                        ))
                    {
                        maxDateTime = entityChange.ChangeTime;
                    }
                }

                var description = string.Join("; ", propsDescr.Where(x => !string.IsNullOrEmpty(x)));

                if (string.IsNullOrEmpty(description) && entityChange.ChangeType == EntityChangeType.Updated) continue;

                var itemEventText = "";

                switch (entityChange.ChangeType)
                {
                    case EntityChangeType.Created: itemEventText = string.IsNullOrEmpty(childName) ? "Created" : "Added"; break;
                    case EntityChangeType.Updated: itemEventText = "Updated"; break;
                    case EntityChangeType.Deleted: itemEventText = string.IsNullOrEmpty(childName) ? "Deleted" : "Removed"; break;
                }

                if (fields == null || fields.Contains(itemEventText))
                {
                    list.Add(new EntityHistoryItemDto()
                    {
                        HistoryItemType = entityChange.ChangeType == EntityChangeType.Created
                            ? (int?) EntityHistoryItemType.Created
                            : (int?) EntityHistoryItemType.Updated,
                        CreationTime = entityChange.ChangeTime,
                        EntityId = entityId,
                        EntityTypeFullName = entityType?.FullName,
                        EventType = "",
                        EventText = ($"{childName} {itemEventText}").Trim(),
                        ExtendedDescription = entityChange.ChangeType == EntityChangeType.Created ? "" : description,
                        UserFullName = username?.FullName ?? $"UserId: {changeSet?.UserId}"
                    });
                }
            }

            if (fields == null || fields.Contains("Created"))
            {
                // If there is no Created record then get the create date stored in ICreationAudited entity
                if (list.All(x => x.HistoryItemType != (int?)EntityHistoryItemType.Created))
                {
                    try
                    {
                        if (Parser.CanParseId(entityId, entityType) &&
                            DynamicRepository.Get(entityType, entityId) is ICreationAudited obj)
                        {
                            var createdBy = GetPersonByUserId(obj.CreatorUserId);
                            list.Add(new EntityHistoryItemDto()
                            {
                                HistoryItemType = (int?) EntityHistoryItemType.Created,
                                CreationTime = obj.CreationTime,
                                EntityTypeFullName = entityType?.FullName,
                                EntityId = entityId,
                                EventText = string.IsNullOrEmpty(childName) ? "Created" : "Added",
                                UserFullName = createdBy?.FullName ?? $"UserId: {obj.CreatorUserId}"
                            });
                        }
                    }
                    catch
                    {
                        // hide exception
                    }
                }
            }

            return list;
        }

        private List<EntityHistoryItemDto> GetManyToManyEntitiesAudit(Type itemType, string entityId)
        {
            var attrs = itemType.GetCustomAttributes<DisplayManyToManyAuditTrailAttribute>().ToList();

            var list = new List<EntityHistoryItemDto>();
            if (!attrs.Any()) return list;

            foreach (var attr in attrs)
            {
                var manyToManyType = attr.ManyToManyEntityType;

                var ownField = !string.IsNullOrEmpty(attr.OwnEntityField)
                    ? manyToManyType.GetProperty(attr.OwnEntityField)
                    : null;

                if (ownField == null)
                {
                    var ownFields = manyToManyType.GetProperties().Where(x => itemType.IsAssignableFrom(x.PropertyType)).ToList();
                    if (ownFields.Count() > 1)
                        throw new Exception($"Found more then 1 field with parent type {itemType.FullName}");
                    ownField = ownFields.FirstOrDefault();
                    if (ownField == null)
                        throw new Exception($"Filed with parent type {itemType.FullName} not found in many-to-many type {manyToManyType.FullName}");
                }

                var relatedType = attr.RelatedEntityType ?? manyToManyType.GetProperty(attr.RelatedEntityField)?.PropertyType;

                if (relatedType == null)
                    throw new Exception($"Related type of property {attr.RelatedEntityField} not found");

                var relatedNameField = string.IsNullOrEmpty(attr.NameField)
                    ? manyToManyType.GetProperties()
                          .FirstOrDefault(x => x.GetCustomAttribute<EntityDisplayNameAttribute>() != null)?.Name ?? ""
                    : attr.NameField;

                var relatedTypeFullName = relatedType.FullName;

                var displayName = string.IsNullOrEmpty(attr.DisplayName)
                    ? relatedType.GetCustomAttribute<EntityAttribute>()?.FriendlyName
                    : attr.DisplayName;
                displayName = string.IsNullOrEmpty(displayName)
                    ? relatedType.Name.ToFriendlyName()
                    : displayName;

                var criteria = new FilterCriteria(FilterCriteria.FilterMethod.Hql);
                criteria.FilterClauses.Add($"ent.{ownField.Name}.Id = '{entityId}'");
                var session = SessionFactory.GetCurrentSession();

                //var hql = $@"Select rel from {manyToManyType.Name} as rel where rel.{ownField.Name}.Id = '{entityId}'";
                //var hq = session.CreateQuery(hql).List<object>();

                var q = session.CreateQuery(manyToManyType, criteria);

                var childItems = q.List<object>()
                    .Select(x =>
                        new Relation()
                        {
                            Id = manyToManyType.GetProperty("Id")?.GetValue(x)?.ToString(),
                            RelatedObject = x as IFullAudited,
                            InnerObject = manyToManyType.GetProperty(attr.RelatedEntityField)?.GetValue(x) as IFullAudited,
                        })
                    .Select(x =>
                    {
                        x.InnerObjectId = relatedType.GetProperty("Id")?.GetValue(x.InnerObject)?.ToString();
                        x.Name = GetEntityName(x.InnerObject, relatedNameField);
                        return x;
                    })
                    .ToList();

                var userIds = childItems.Select(x => x.RelatedObject.CreatorUserId).ToList();
                userIds.AddRange(childItems.Select(x => x.RelatedObject.DeleterUserId));
                userIds.AddRange(childItems.Select(x => x.InnerObject.DeleterUserId));
                userIds = userIds.Distinct().Where(x => x != null).ToList();
                var persons = session.QueryOver<Person>().Where(x => x.User.Id.IsIn(userIds)).List();

                foreach (var childItem in childItems)
                {
                    var createdBy = GetPersonByUserIdInternal(persons, childItem.RelatedObject.CreatorUserId);
                    list.Add(new EntityHistoryItemDto()
                    {
                        HistoryItemType = (int?)EntityHistoryItemType.Added,
                        CreationTime = childItem.RelatedObject.CreationTime,
                        EntityTypeFullName = relatedTypeFullName,
                        EntityId = childItem.Id,
                        EventText = $"`{displayName}` added",
                        ExtendedDescription = childItem.Name,
                        UserFullName = createdBy?.FullName ?? $"UserId: {childItem.RelatedObject.CreatorUserId}"
                    });

                    if (childItem.RelatedObject.IsDeleted)
                    {
                        var deletedBy = GetPersonByUserIdInternal(persons, childItem.RelatedObject.DeleterUserId);
                        list.Add(new EntityHistoryItemDto()
                        {
                            HistoryItemType = (int?)EntityHistoryItemType.Removed,
                            CreationTime = childItem.RelatedObject.DeletionTime,
                            EntityTypeFullName = relatedTypeFullName,
                            EntityId = childItem.Id,
                            EventText = $"`{displayName}` removed",
                            ExtendedDescription = childItem.Name,
                            UserFullName = deletedBy?.FullName ?? $"UserId: {childItem.RelatedObject.DeleterUserId}"
                        });
                    }
                    else
                    {
                        if (childItem.InnerObject.IsDeleted)
                        {
                            var deletedBy = GetPersonByUserIdInternal(persons, childItem.InnerObject.DeleterUserId);
                            list.Add(new EntityHistoryItemDto()
                            {
                                HistoryItemType = (int?)EntityHistoryItemType.Removed,
                                CreationTime = childItem.InnerObject.DeletionTime,
                                EntityTypeFullName = relatedTypeFullName,
                                EntityId = childItem.Id,
                                EventText = $"`{displayName}` removed",
                                ExtendedDescription = $"`{childItem.Name}` was deleted",
                                UserFullName = deletedBy?.FullName ?? $"UserId: {childItem.InnerObject.DeleterUserId}"
                            });
                        }
                    }

                    var fields = new List<string>() { "Updated" };
                    fields.AddRange(attr.AuditedFields ?? new string[0]);

                    list.AddRange(GetEntityAudit(attr.AnyRelatedEntityType ? null : relatedType, childItem.InnerObjectId, displayName, fields.ToArray()));
                }
            }

            return list;
        }

        private List<EntityHistoryItemDto> GetManyToOneEntitiesAudit(Type itemType, string entityId)
        {
            var attrs = itemType.GetCustomAttributes<DisplayManyToOneAuditTrailAttribute>().ToList();

            var list = new List<EntityHistoryItemDto>();
            if (!attrs.Any()) return list;

            foreach (var attr in attrs)
            {
                var manyToOneType = attr.ManyToOneEntityType;
                var manyToOneTypeFullName = manyToOneType.FullName;

                var ownField = !string.IsNullOrEmpty(attr.RelatedEntityField)
                    ? manyToOneType.GetProperty(attr.RelatedEntityField)
                    : null;

                if (ownField == null)
                {
                    var ownFields = manyToOneType.GetProperties().Where(x => itemType.IsAssignableFrom(x.PropertyType)).ToList();
                    if (ownFields.Count() > 1)
                        throw new Exception($"Found more then 1 field with parent type {itemType.FullName}");
                    ownField = ownFields.FirstOrDefault();
                    if (ownField == null)
                        throw new Exception($"Filed with parent type {itemType.FullName} not found in many-to-many type {manyToOneType.FullName}");
                }

                var relatedEntityField = ownField.Name;

                var entityIdJsonString = entityId.ToInt64() == 0 ? $"\"{entityId}\"" : entityId;

                var displayName = string.IsNullOrEmpty(attr.DisplayName)
                    ? manyToOneType.GetCustomAttribute<EntityAttribute>()?.FriendlyName
                    : attr.DisplayName;
                displayName = string.IsNullOrEmpty(displayName)
                    ? manyToOneType.Name.ToFriendlyName()
                    : displayName;

                var relatedNameField = string.IsNullOrEmpty(attr.NameField)
                    ? manyToOneType.GetProperties()
                          .FirstOrDefault(x => x.GetCustomAttribute<EntityDisplayNameAttribute>() != null)?.Name ?? ""
                    : attr.NameField;


                var changesAdded = EntityHistoryItemRepository
                    .GetAllList(x => 
                        x.NewValue == entityIdJsonString 
                        && x.PropertyName == relatedEntityField
                        && x.EntityTypeFullName == manyToOneTypeFullName
                        );

                foreach (var entityHistoryItem in changesAdded)
                {
                    var relatedObject = DynamicRepository.Get(manyToOneType, entityHistoryItem.EntityId);

                    var name = GetEntityName(relatedObject, relatedNameField);

                    list.Add(new EntityHistoryItemDto()
                    {
                        HistoryItemType = (int?)EntityHistoryItemType.Added,
                        CreationTime = entityHistoryItem.CreationTime,
                        EntityTypeFullName = manyToOneTypeFullName,
                        EntityId = entityHistoryItem.EntityId,
                        EventText = $"`{displayName}` added",
                        ExtendedDescription = name,
                        UserFullName = string.IsNullOrEmpty(entityHistoryItem.UserFullName) 
                            ? $"UserId: {entityHistoryItem.UserId}"
                            : entityHistoryItem.UserFullName
                    });
                }

                var changesRemoved = EntityHistoryItemRepository
                    .GetAllList(x =>
                        x.OriginalValue == entityIdJsonString
                        && x.PropertyName == relatedEntityField
                        && x.EntityTypeFullName == manyToOneTypeFullName
                    );

                foreach (var entityHistoryItem in changesRemoved)
                {
                    var relatedObject = DynamicRepository.Get(manyToOneType, entityHistoryItem.EntityId);

                    var name = GetEntityName(relatedObject, relatedNameField);
                    ;

                    list.Add(new EntityHistoryItemDto()
                    {
                        HistoryItemType = (int?)EntityHistoryItemType.Removed,
                        CreationTime = entityHistoryItem.CreationTime,
                        EntityTypeFullName = manyToOneTypeFullName,
                        EntityId = entityHistoryItem.EntityId,
                        EventText = $"`{displayName}` removed",
                        ExtendedDescription = name,
                        UserFullName = string.IsNullOrEmpty(entityHistoryItem.UserFullName)
                            ? $"UserId: {entityHistoryItem.UserId}"
                            : entityHistoryItem.UserFullName
                    });
                }

                var criteria = new FilterCriteria(FilterCriteria.FilterMethod.Hql);
                criteria.FilterClauses.Add($"ent.{relatedEntityField}.Id = '{entityId}'");
                var session = SessionFactory.GetCurrentSession();
                var q = session.CreateQuery(manyToOneType, criteria);

                var childItems = q.List<object>()
                    .Select(x =>
                        new Relation()
                        {
                            Id = manyToOneType.GetProperty("Id")?.GetValue(x)?.ToString(),
                            RelatedObject = x as IFullAudited,
                            Name = GetEntityName(x, relatedNameField)
                        })
                    .ToList();

                var userIds = childItems.Select(x => x.RelatedObject.CreatorUserId).ToList();
                userIds.AddRange(childItems.Select(x => x.RelatedObject.DeleterUserId));
                userIds = userIds.Distinct().Where(x => x != null).ToList();
                var persons = session.QueryOver<Person>().Where(x => x.User.Id.IsIn(userIds)).List();

                foreach (var childItem in childItems)
                {
                    if (
                        list.Where(x =>
                                x.EntityId == childItem.Id
                                && x.EntityTypeFullName == manyToOneTypeFullName)
                            .OrderBy(x => x.CreationTime)
                            .FirstOrDefault()?.HistoryItemType != (int?)EntityHistoryItemType.Added
                    )
                    {
                        var createdBy = GetPersonByUserIdInternal(persons, childItem.RelatedObject.CreatorUserId);
                        list.Add(new EntityHistoryItemDto()
                        {
                            HistoryItemType = (int?) EntityHistoryItemType.Added,
                            CreationTime = childItem.RelatedObject.CreationTime,
                            EntityTypeFullName = manyToOneTypeFullName,
                            EntityId = childItem.Id,
                            EventText = $"`{displayName}` added",
                            ExtendedDescription = childItem.Name,
                            UserFullName = createdBy?.FullName ?? $"UserId: {childItem.RelatedObject.CreatorUserId}"
                        });
                    }

                    if (childItem.RelatedObject.IsDeleted)
                    {
                        if (
                            list.Where(x =>
                                    x.EntityId == childItem.Id
                                    && x.EntityTypeFullName == manyToOneTypeFullName)
                                .OrderBy(x => x.CreationTime)
                                .FirstOrDefault()?.HistoryItemType != (int?)EntityHistoryItemType.Removed
                        )
                        {
                            var deletedBy = GetPersonByUserIdInternal(persons, childItem.RelatedObject.DeleterUserId);
                            list.Add(new EntityHistoryItemDto()
                            {
                                HistoryItemType = (int?) EntityHistoryItemType.Removed,
                                CreationTime = childItem.RelatedObject.DeletionTime,
                                EntityTypeFullName = manyToOneTypeFullName,
                                EntityId = childItem.Id,
                                EventText = $"`{displayName}` removed",
                                ExtendedDescription = $"`{childItem.Name}` was deleted",
                                UserFullName = deletedBy?.FullName ?? $"UserId: {childItem.RelatedObject.DeleterUserId}"
                            });
                        }
                    }

                    var fields = new List<string>() {"Updated"};
                    fields.AddRange(attr.AuditedFields ?? new string[0]);

                    list.AddRange(GetEntityAudit(manyToOneType, childItem.Id, childItem.Name, fields.ToArray()));
                }
            }

            return list;
        }


        private List<EntityHistoryItemDto> GetAuditedAsEvents(string entityTypeFullName, string entityId)
        {
            var events = EventRepository.GetAllList(x =>
                    x.EntityChange != null
                    && x.EntityChange.EntityId == entityId
                    && x.EntityChange.EntityTypeFullName == entityTypeFullName
                    && x.EventType == EntityHistoryCommonEventTypes.PROPERTY_CHANGE_AS_EVENT
                    && x.EntityPropertyChange != null
                );

            return events.Select(x =>
                {
                    var ecs = EntityChangeSetRepository.Get(x.EntityChange.EntityChangeSetId);
                    return new EntityHistoryItemDto()
                    {
                        CreationTime = x.EntityChange.ChangeTime,
                        ExtendedDescription = x.Description,
                        EntityId = entityId,
                        EntityTypeFullName = entityTypeFullName,
                        EventText = x.EventName,
                        HistoryItemType = (int?)EntityHistoryItemType.Event,
                        UserFullName = GetPersonByUserId(ecs?.UserId)?.FullName ?? ecs?.ToString()
                    };
                }
            ).ToList();
        }

        private List<EntityHistoryItemDto> GetGenericEntitiesAudit(Type itemType, string entityId)
        {
            var attrs = itemType.GetCustomAttributes<DisplayGenericEntitesAuditTrailAttribute>();

            var history = new List<EntityHistoryItemDto>();
            if (!attrs.Any()) return history;

            var typeShortAlias = itemType.GetCustomAttribute<EntityAttribute>()?.TypeShortAlias ?? itemType.FullName;

            foreach (var attr in attrs)
            {
                var childType = attr.EntityType;

                var ownerIdField = string.IsNullOrEmpty(attr.OwnerIdField) ? "OwnerId" : attr.OwnerIdField;
                var ownerTypeField = string.IsNullOrEmpty(attr.OwnerTypeField) ? "OwnerType" : attr.OwnerTypeField;

                var childNameField = string.IsNullOrEmpty(attr.NameField)
                    ? childType.GetProperties()
                          .FirstOrDefault(x => x.GetCustomAttribute<EntityDisplayNameAttribute>() != null)?.Name ?? ""
                    : attr.NameField;

                var displayName = string.IsNullOrEmpty(attr.DisplayName)
                    ? childType.GetCustomAttribute<EntityAttribute>()?.FriendlyName
                    : attr.DisplayName;
                displayName = string.IsNullOrEmpty(displayName)
                    ? childType.Name.ToFriendlyName()
                    : displayName;

                var childTypeFullName = childType.FullName;

                var criteria = new FilterCriteria(FilterCriteria.FilterMethod.Hql);
                criteria.FilterClauses.Add($"ent.{ownerIdField} = '{entityId}'");
                criteria.FilterClauses.Add($"ent.{ownerTypeField} = '{typeShortAlias}'");
                var session = SessionFactory.GetCurrentSession();
                var q = session.CreateQuery(childType, criteria);
                var childItems = q.List<object>()
                    .Select(x =>
                        new Relation()
                        {
                            Id = childType.GetProperty("Id")?.GetValue(x)?.ToString(),
                            RelatedObject = x as IFullAudited,
                            Name = GetEntityName(x, childNameField)
                        })
                    .ToList();

                var userIds = childItems.Select(x => x.RelatedObject.CreatorUserId).ToList();
                userIds.AddRange(childItems.Select(x => x.RelatedObject.DeleterUserId));
                userIds = userIds.Distinct().Where(x => x != null).ToList();
                var persons = session.QueryOver<Person>().Where(x => x.User.Id.IsIn(userIds)).List();

                foreach (var childItem in childItems)
                {
                    var createdBy = GetPersonByUserIdInternal(persons, childItem.RelatedObject.CreatorUserId);
                    history.Add(new EntityHistoryItemDto()
                    {
                        HistoryItemType = (int?)EntityHistoryItemType.Added,
                        CreationTime = childItem.RelatedObject.CreationTime,
                        EntityTypeFullName = childTypeFullName,
                        EntityId = childItem.Id,
                        EventText = $"`{displayName}` added",
                        ExtendedDescription = childItem.Name,
                        UserFullName = createdBy?.FullName ?? $"UserId: {childItem.RelatedObject.CreatorUserId}"
                    });

                    if (childItem.RelatedObject.IsDeleted)
                    {
                        var deletedBy = GetPersonByUserIdInternal(persons, childItem.RelatedObject.DeleterUserId);
                        history.Add(new EntityHistoryItemDto()
                        {
                            HistoryItemType = (int?)EntityHistoryItemType.Deleted,
                            CreationTime = childItem.RelatedObject.DeletionTime,
                            EntityTypeFullName = childTypeFullName,
                            EntityId = childItem.Id,
                            EventText = $"`{displayName}` deleted",
                            ExtendedDescription = childItem.Name,
                            UserFullName = deletedBy?.FullName ?? $"UserId: {childItem.RelatedObject.DeleterUserId}"
                        });
                    }
                }
            }

            return history;
        }

        private string GetEntityName(object entity, string fieldName)
        {
            if (entity == null) return "";

            var type = entity.GetType();
            return string.IsNullOrEmpty(fieldName)
                ? entity.ToString()
                : string.IsNullOrEmpty(type.GetProperty(fieldName)?.GetValue(entity)?.ToString())
                    ? entity.ToString()
                    : type.GetProperty(fieldName)?.GetValue(entity)?.ToString();
        }

        private Person GetPersonByUserIdInternal(IList<Person> list, long? userId)
        {
            return userId != null
                ? list.FirstOrDefault(x => x.User != null && x.User.Id == userId)
                : null;

        }

        private Person GetPersonByUserId(long? userId)
        {
            return userId != null
                ? PersonRepository.GetAll().FirstOrDefault(x => x.User != null && x.User.Id == userId)
                : null;

        }

        private class Relation
        {
            public string Id { get; set; }
            public IFullAudited RelatedObject { get; set; }
            public IFullAudited InnerObject { get; set; }
            public string InnerObjectId { get; set; }
            public string Name { get; set; }
        }
    }
}