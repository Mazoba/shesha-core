using System;
using System.Collections.Generic;
using System.Linq;
using Abp.Application.Services;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Reflection;
using Abp.Runtime.Session;
using AutoMapper;
using Castle.Core.Logging;
using Microsoft.AspNetCore.Mvc;
using Shesha.Configuration.Runtime;
using Shesha.Domain;
using Shesha.Domain.Enums;
using Shesha.Extensions;
using Shesha.Services;
using Shesha.Utilities;

namespace Shesha.Web.DataTable
{
    /// <summary>
    /// Data table configuration storage and access
    /// </summary>
    public class DataTableConfigurationStore: IDataTableConfigurationStore, ISingletonDependency
    {
        private readonly ITypeFinder _typeFinder;
        private IDictionary<string, Func<DataTableConfig>> _dataTableConfigurations = new Dictionary<string, Func<DataTableConfig>>();
        private readonly IEntityConfigurationStore _entityConfigurationStore;

        /// <summary>
        /// Logger
        /// </summary>
        public ILogger Logger { get; set; }

        public IocManager IocManager { get; set; }

        private readonly IMapper _mapper;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        private readonly IRepository<ShaRole, Guid> _roleRepository;

        private readonly IRepository<StoredFilter, Guid> _storedFilterRepository;
        private readonly IRepository<StoredFilterContainer, Guid> _filterContainerRepository;
        private readonly IRepository<EntityVisibility, Guid> _entityVisibilityRepository;

        public DataTableConfigurationStore(ITypeFinder typeFinder, IRepository<StoredFilter, Guid> storedFilterRepository, IRepository<StoredFilterContainer, Guid> filterContainerRepository, IRepository<ShaRole, Guid> roleRepository, IRepository<EntityVisibility, Guid> entityVisibilityRepository, IUnitOfWorkManager unitOfWorkManager, IMapper mapper, IEntityConfigurationStore entityConfigurationStore)
        {
            _typeFinder = typeFinder;

            _unitOfWorkManager = unitOfWorkManager;
            _mapper = mapper;
            _entityConfigurationStore = entityConfigurationStore;
            _roleRepository = roleRepository;

            _storedFilterRepository = storedFilterRepository;
            _filterContainerRepository = filterContainerRepository;
            _entityVisibilityRepository = entityVisibilityRepository;

            Logger = NullLogger.Instance;

            Initialise();
        }

        public void Initialise()
        {
            var controllerTypes = _typeFinder
                .Find(
                    t => (t.IsSubclassOf(typeof(ControllerBase)) || typeof(IApplicationService).IsAssignableFrom(t))&&
                         !t.ContainsGenericParameters /* can't perform late-bound operations for types with ContainsGenericParameters = true */)
                .ToList();

            foreach (var controllerType in controllerTypes)
            {
                RegisterDataTableConfigurations(controllerType);
                //RegisterEntityPickerDialogConfigurations(controller);
            }
        }

        private void RegisterDataTableConfigurations(Type controllerType)
        {
            var methods = controllerType.GetMethods()
                .Where(m => (typeof(DataTableConfig).IsAssignableFrom(m.ReturnType) && m.IsStatic && !m.GetGenericArguments().Any()) && !m.GetParameters().Any())
                .ToList();

            using (var uow = _unitOfWorkManager.Begin())
            {
                foreach (var method in methods)
                {
                    try
                    {
                        var tableConfig = method.Invoke(null, null);
                        if (tableConfig is DataTableConfig config)
                        {
                            var tableId = config.Id;
                            if (_dataTableConfigurations.ContainsKey(tableId))
                                throw new Exception(
                                    $"Table configuration with the same Id already registered, (tableId: {tableId}, controller: {controllerType.FullName})");

                            // Get all filters of the given data table 
                            var existingFilters = _filterContainerRepository.GetAll()
                                .Where(f => f.OwnerType == "" && f.OwnerId == tableId && !f.Filter.IsDeleted).Select(f => f.Filter)
                                .OrderBy(f => f.OrderIndex).ToList();
                            var maxSortIndex = existingFilters.Max(f => (int?) f.OrderIndex).GetValueOrDefault();

                            // 1. Store any new filters registered using an attribute
                            var storedFilterAttributeRegistrations =
                                method.GetCustomAttributes(typeof(StoredFilterAttribute), false);
                            foreach (StoredFilterAttribute filterRegistration in storedFilterAttributeRegistrations)
                            {
                                // Check if a filter with the same name already exists
                                var existingFilter = _storedFilterRepository.GetAll()
                                    .FirstOrDefault(f => f.Id == filterRegistration.Id);


                                // Add new definition to DB
                                var orderIndex = filterRegistration.OrderIndex;
                                if (orderIndex != null && existingFilters.Any(f => f.OrderIndex == orderIndex))
                                {
                                    // If the same index already exists, we append to the end of the list
                                    orderIndex = ++maxSortIndex;
                                }

                                if (existingFilter == null)
                                {
                                    existingFilter = new StoredFilter
                                    {
                                        Id = filterRegistration.Id,
                                        Name = filterRegistration.Name,
                                        ExpressionType = filterRegistration.ExpressionType,
                                        Expression = filterRegistration.Expression,
                                        OrderIndex = orderIndex ?? ++maxSortIndex,
                                        IsExclusive = true
                                    };
                                    _storedFilterRepository.InsertAndGetId(existingFilter);
                                }
                                else
                                {
                                    // Update data
                                    if (existingFilter.Name != filterRegistration.Name &&
                                        !string.IsNullOrEmpty(filterRegistration.Name))
                                    {
                                        existingFilter.Name = filterRegistration.Name;
                                    }

                                    if (existingFilter.ExpressionType != filterRegistration.ExpressionType)
                                    {
                                        existingFilter.ExpressionType = filterRegistration.ExpressionType;
                                    }

                                    if (existingFilter.Expression != filterRegistration.Expression &&
                                        !string.IsNullOrEmpty(filterRegistration.Expression))
                                    {
                                        existingFilter.Expression = filterRegistration.Expression;
                                    }

                                    if (existingFilter.OrderIndex != filterRegistration.OrderIndex &&
                                        filterRegistration.OrderIndex != null)
                                    {
                                        existingFilter.OrderIndex = filterRegistration.OrderIndex.GetValueOrDefault();
                                    }

                                    _storedFilterRepository.Update(existingFilter);
                                }

                                if (!existingFilter.ContainerEntities.Any(
                                    c => c.OwnerType == "" && c.OwnerId == tableId))
                                {
                                    // Add to the container (current datatable ID)
                                    var newContainer = new StoredFilterContainer
                                    {
                                        Id = Guid.NewGuid(),
                                        Filter = existingFilter,
                                        OwnerType = "",
                                        OwnerId = tableId
                                    };
                                    _filterContainerRepository.Insert(newContainer);
                                    // Add to bag
                                    existingFilter.ContainerEntities.Add(newContainer);
                                }

                                // Configure visibility
                                if (!string.IsNullOrEmpty(filterRegistration.VisibilityRoleName))
                                {
                                    var role = _roleRepository.GetAll().FirstOrDefault(r =>
                                        r.NameSpace == filterRegistration.VisibilityRoleNamespace &&
                                        r.Name == filterRegistration.Name);
                                    if (role != null)
                                    {
                                        if (!existingFilter.VisibleBy.Any(v =>
                                            v.OwnerId == role.Id.ToString() && v.OwnerType == role.GetTypeShortAlias()))
                                        {
                                            var newVisibility = new EntityVisibility
                                            {
                                                Id = Guid.NewGuid(),
                                                EntityType = existingFilter.GetTypeShortAlias(),
                                                EntityId = existingFilter.Id.ToString(),
                                                OwnerType = role.GetTypeShortAlias(),
                                                OwnerId = role.Id.ToString(),
                                                EntityAccess = RefListEntityAccess.ReadAccess
                                            };
                                            _entityVisibilityRepository.Insert(newVisibility);
                                            // Add to bag
                                            existingFilter.VisibleBy.Add(newVisibility);
                                        }
                                    }
                                    else
                                    {
                                        throw new Exception("EntityVisibility: Workflow role not found. namespace: " +
                                                            filterRegistration.VisibilityRoleNamespace + ". name: " +
                                                            filterRegistration.VisibilityRoleName);
                                    }
                                }

                            }

                            // Get the filter list from DB (this may include user filters as well as previously registered filters)
                            //config.StoredFilters = GetStoredFilters(config);
                            config.StoredFilters = existingFilters.OrderBy(f => f.OrderIndex).Select(_mapper.Map<DataTableStoredFilter>).ToList();

                            _dataTableConfigurations.Add(config.Id, () =>
                            {
                                var instance = method.Invoke(null, null) as DataTableConfig;
                                if (instance != null)
                                    instance.StoredFilters = GetStoredFilters(config);
                                return instance;
                            });

                            /* todo: the configuration now partially depends on user because of tags like {userId} in filters and
                             *       also because of Index View Selector filers visibility settings
                             *       make sure Store updates configuration for current user before sending it out to client. */
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("Error whilst loading configuration", ex);
                    }
                }
                uow.Complete();
            }
        }

        public void Register()
        {

        }

        private List<DataTableStoredFilter> GetStoredFilters(DataTableConfig config)
        {
            var filters = new List<DataTableStoredFilter>();
            var roleRepo = IocManager.Resolve<IRepository<ShaRole, Guid>>();
            var rolePersonRepo = IocManager.Resolve<IRepository<ShaRoleAppointedPerson, Guid>>();
            var containerRepo = IocManager.Resolve<IRepository<StoredFilterContainer, Guid>>();
            var mapper = IocManager.Resolve<IMapper>();
            var existingFilters = containerRepo.GetAll()
                .Where(f => f.OwnerType == "" && f.OwnerId == config.Id && !f.Filter.IsDeleted).Select(f => f.Filter)
                .OrderBy(f => f.OrderIndex).ToList();
            foreach (var filter in existingFilters)
            {
                // Security: when visibility conditions are provided, restrict the filter

                var hasAccess = true;

                if (filter.VisibleBy.Any())
                {
                    var shaRoleType = _entityConfigurationStore.Get(typeof(ShaRole))?.TypeShortAlias;
                    var visibleByRoles = filter.VisibleBy.Where(v => v.OwnerType == shaRoleType)
                        .Select(v => roleRepo.Get(v.OwnerId.ToGuid())).ToList();

                    hasAccess = false;

                    var currentUser = GetCurrentUser();
                    foreach (var role in visibleByRoles)
                    {
                        if (rolePersonRepo.GetAll().Any(c => c.Role == role && c.Person == currentUser))
                        {
                            hasAccess = true;
                            break;
                        }
                    }
                }

                if (hasAccess)
                {
                    filters.Add(mapper.Map<DataTableStoredFilter>(filter));
                }
            }

            return filters;
        }

        private Person GetCurrentUser()
        {
            var abpSession = IocManager.Resolve<IAbpSession>();
            if (abpSession.UserId == null)
                return null;

            var personService = StaticContext.IocManager.Resolve<IRepository<Person, Guid>>();
            var person = personService.FirstOrDefault(c => c.User.Id == abpSession.GetUserId());
            return person;
        }

        public DataTableConfig GetTableConfiguration(string id, bool throwNotFoundException = true)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            if (!_dataTableConfigurations.TryGetValue(id, out var configFunc))
            {
                /* todo: review and restore dynamic tables support
                var prefix = id.LeftPart('-');
                if (!string.IsNullOrWhiteSpace(prefix))
                {
                    var provider = Current.GetProvider(prefix);
                    return provider?.GetDataTableConfiguration(key);
                }
                */

                if (throwNotFoundException)
                    throw new Exception($"Could not find requested Table configuration '{id}'. Please make sure that the configuration has been registered with the {nameof(DataTableConfigurationStore)}.");

                return null;
            }
            else
            {
                return configFunc.Invoke();
            }
        }

        /// inheritDoc
        public List<string> GetTableIds()
        {
            return _dataTableConfigurations.Select(i => i.Key).ToList();
        }
    }
}
