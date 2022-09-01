using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Reflection;
using ElmahCore;
using log4net.Util;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json.Linq;
using NHibernate;
using NHibernate.Type;
using NHibernate.Util;
using Shesha.Configuration.Runtime;
using Shesha.Domain;
using Shesha.Domain.Attributes;
using Shesha.DynamicEntities.Dtos;
using Shesha.EntityHistory;
using Shesha.Extensions;
using Shesha.JsonLogic;
using Shesha.Metadata;
using Shesha.Reflection;
using Shesha.Services;
using Shesha.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Abp.Collections.Extensions;

namespace Shesha.DynamicEntities
{
    public class EntityModelBinder : IEntityModelBinder, ITransientDependency
    {
        private readonly IDynamicRepository _dynamicRepository;
        private readonly IRepository<EntityProperty, Guid> _entityPropertyRepository;
        private readonly IMetadataProvider _metadataProvider;
        private readonly IIocManager _iocManager;
        private readonly ISessionFactory _sessionFactory;
        private readonly ITypeFinder _typeFinder;

        public EntityModelBinder(
            IDynamicRepository dynamicRepository,
            IRepository<EntityProperty, Guid> entityPropertyRepository,
            IMetadataProvider metadataProvider,
            IIocManager iocManager,
            ISessionFactory sessionFactory,
            ITypeFinder typeFinder
            )
        {
            _dynamicRepository = dynamicRepository;
            _entityPropertyRepository = entityPropertyRepository;
            _metadataProvider = metadataProvider;
            _iocManager = iocManager;
            _sessionFactory = sessionFactory;
            _typeFinder = typeFinder;
        }

        public async Task<bool> BindPropertiesAsync(
            JObject jobject,
            object entity,
            List<ValidationResult> validationResult,
            string propertyName = null,
            List<string> formFields = null)
        {
            var properties = entity.GetType().StripCastleProxyType()
                .GetProperties()
                .Where(p => p.CanWrite && p.Name != "Id")
                .ToList();

            validationResult ??= new List<ValidationResult>();

            var formFieldsInternal = formFields;
            if (formFields == null)
            {
                var _formFields = jobject.Property(nameof(IHasFormFieldsList._formFields));
                var formFieldsArray = _formFields?.Value as JArray;
                formFieldsInternal = formFieldsArray?.Select(f => f.Value<string>()).ToList() ?? new List<string>();
            }

            foreach (var jproperty in jobject.Properties().ToList().Where(x => x.Name != "id" && x.Name != nameof(IHasFormFieldsList._formFields)))
            {
                try
                {
                    // Skip property if _formFields is specified and doesn't contain propertyName
                    if (formFieldsInternal.Any() && !formFieldsInternal.Any(x => x.Equals(jproperty.Name) || x.StartsWith(jproperty.Name + ".")))
                        continue;

                    var childFormFields = formFieldsInternal
                        .Where(x => x.Equals(jproperty.Name) || x.StartsWith(jproperty.Name + "."))
                        .Select(x => x.RemovePrefix(jproperty.Name))
                        .Select(x => x.RemovePrefix(".")).ToList();
                    childFormFields = childFormFields.Any() ? childFormFields : null;

                    var property = properties.FirstOrDefault(x => x.Name.ToCamelCase() == jproperty.Name);
                    if (property == null && jproperty.Name.EndsWith("Id"))
                    {
                        var idName = Shesha.Utilities.StringHelper.Left(jproperty.Name, jproperty.Name.Length - 2);
                        property = properties.FirstOrDefault(x => x.Name.ToCamelCase() == idName);
                    }
                    if (property != null)
                    {
                        var propType = _metadataProvider.GetDataType(property);

                        if (jproperty.Value.IsNullOrEmpty())
                        {
                            property.SetValue(entity, null);
                        }
                        else
                        {
                            var result = true;
                            switch (propType.DataType)
                            {
                                case DataTypes.String:
                                case DataTypes.Date:
                                case DataTypes.Time:
                                case DataTypes.DateTime:
                                case DataTypes.Number:
                                case DataTypes.Boolean:
                                case DataTypes.Guid:
                                case DataTypes.ReferenceListItem:
                                //case DataTypes.Enum: // Enum binded as integer
                                    object parsedValue = null;
                                    result = Parser.TryParseToValueType(jproperty.Value.ToString(), property.PropertyType, out parsedValue, isDateOnly: propType.DataType == DataTypes.Date);
                                    if (result)
                                    {
                                        property.SetValue(entity, parsedValue);
                                    }
                                    break;
                                case DataTypes.Array:
                                    switch (propType.DataFormat)
                                    {
                                        case ArrayFormats.ReferenceListItem:
                                            string[] valComponents;
                                            if (jproperty.Value is JArray jArray)
                                            {
                                                valComponents = jArray.Select(x => x.ToString()).ToArray();
                                            }
                                            else
                                            {
                                                var propertyValue = jproperty.Value.ToString();
                                                // Removing the redundant ',' from the hidden element.
                                                if (propertyValue.EndsWith(",")) propertyValue = propertyValue.Substring(0, propertyValue.Length - 1);
                                                else if (propertyValue.StartsWith(",")) propertyValue = propertyValue.Substring(1, propertyValue.Length - 1);
                                                else propertyValue.Replace(",,", ",");
                                                valComponents = propertyValue.Split(',');
                                            }
                                            var totalVal = 0;
                                            for (int i = 0; i < valComponents.Length; i++)
                                            {
                                                if (!string.IsNullOrEmpty(valComponents[i]))
                                                {
                                                    int val;
                                                    if (!int.TryParse(valComponents[i], out val))
                                                    {
                                                        // Try parse enum
                                                        var prop = entity.GetType().GetProperty(propertyName);
                                                        if (prop != null && prop.PropertyType.IsEnum)
                                                        {
                                                            var type = ReflectionHelper.GetUnderlyingTypeIfNullable(prop.PropertyType);
                                                            object enumVal;
                                                            try
                                                            {
                                                                enumVal = Enum.Parse(type, valComponents[i], true);
                                                            }
                                                            catch (Exception e)
                                                            {
                                                                validationResult.Add(new ValidationResult($"Value of '{jproperty.Path}' is not valid."));
                                                                break;
                                                            }
                                                            if (enumVal != null)
                                                            {
                                                                totalVal += (int)enumVal;
                                                            }
                                                        }
                                                    }
                                                    else
                                                        totalVal += val;
                                                }
                                            }
                                            object refValue = null;
                                            result = Parser.TryParseToValueType(totalVal.ToString(), property.PropertyType, out refValue);
                                            if (result)
                                            {
                                                property.SetValue(entity, refValue);
                                            }
                                            break;
                                    }
                                    break;
                                case DataTypes.Object:
                                    if (jproperty.Value is JObject childSimplyObject)
                                    {
                                        var newObject = Activator.CreateInstance(property.PropertyType);
                                        // create a new object
                                        if (await BindPropertiesAsync(childSimplyObject, newObject, validationResult, jproperty.Name, childFormFields))
                                            property.SetValue(entity, newObject);
                                    }
                                    else
                                    {
                                        property.SetValue(entity, null);
                                    }
                                    break;
                                case DataTypes.EntityReference:
                                    // Get the rules of cascade update
                                    var cascadeAttr = property.GetCustomAttribute<CascadeUpdateRulesAttribute>()
                                        ?? property.PropertyType.GetCustomAttribute<CascadeUpdateRulesAttribute>();

                                    if (jproperty.Value is JObject childObject)
                                    {
                                        var jchildId = childObject.Property("id")?.Value.ToString();
                                        if (!string.IsNullOrEmpty(jchildId))
                                        {
                                            var childEntity = property.GetValue(entity);
                                            var newChildEntity = childEntity;
                                            var childId = childEntity?.GetType().GetProperty("Id")?.GetValue(childEntity)?.ToString();

                                            // if child entity is specified
                                            if (childId?.ToLower() != jchildId?.ToLower())
                                            {
                                                // id changed
                                                newChildEntity = _dynamicRepository.Get(property.PropertyType, jchildId);

                                                if (newChildEntity == null)
                                                {
                                                    validationResult.Add(new ValidationResult($"Entity with Id='{jchildId}' not found for `{jproperty.Path}`."));
                                                    break;
                                                }
                                            }

                                            if (childObject.Properties().ToList().Where(x => x.Name != "id").Any())
                                            {
                                                if (!(cascadeAttr?.CanUpdate ?? false))
                                                {
                                                    validationResult.Add(new ValidationResult($"`{property.Name}` is not allowed to be updated."));
                                                    break;
                                                }
                                                if (!(await BindPropertiesAsync(childObject, newChildEntity, validationResult, jproperty.Name, childFormFields)))
                                                    break;
                                            }

                                            if (childEntity != newChildEntity)
                                            {
                                                property.SetValue(entity, newChildEntity);
                                                if (childEntity != null && (cascadeAttr?.DeleteUnreferenced ?? false))
                                                {
                                                    await DeleteUnreferencedEntityAsync(childEntity, entity);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // if Id is not specified
                                            if (childObject.Properties().ToList().Where(x => x.Name != "id").Any())
                                            {
                                                var childEntity = Activator.CreateInstance(property.PropertyType);
                                                // create a new object
                                                if (!(await BindPropertiesAsync(childObject, childEntity, validationResult, jproperty.Name, childFormFields)))
                                                    break;

                                                if (cascadeAttr?.CascadeEntityCreator != null)
                                                {
                                                    // try to select entity by key fields
                                                    if (Activator.CreateInstance(cascadeAttr.CascadeEntityCreator) is ICascadeEntityCreator creator)
                                                    {
                                                        creator.IocManager = _iocManager;
                                                        var data = new CascadeRuleEntityFinderInfo(childEntity);
                                                        if (!creator.VerifyEntity(data, validationResult))
                                                            break;

                                                        data._NewObject = childEntity = creator.PrepareEntity(data);

                                                        var foundEntity = creator.FindEntity(data);
                                                        if (foundEntity != null)
                                                        {
                                                            if (await BindPropertiesAsync(childObject, foundEntity, validationResult, jproperty.Name, childFormFields))
                                                                property.SetValue(entity, foundEntity);
                                                            break;
                                                        }
                                                    }
                                                }

                                                if (!(cascadeAttr?.CanCreate ?? false))
                                                {
                                                    validationResult.Add(new ValidationResult($"`{property.Name}` is not allowed to be created."));
                                                    break;
                                                }

                                                property.SetValue(entity, childEntity);
                                            }
                                            else
                                            {
                                                var childEntity = property.GetValue(entity);

                                                // remove referenced object
                                                property.SetValue(entity, null);

                                                if (childEntity != null && (cascadeAttr?.DeleteUnreferenced ?? false))
                                                {
                                                    await DeleteUnreferencedEntityAsync(childEntity, entity);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var jchildId = jproperty.Value.ToString();
                                        if (!string.IsNullOrEmpty(jchildId))
                                        {
                                            var childEntity = property.GetValue(entity);
                                            var newChildEntity = childEntity;
                                            var childId = childEntity?.GetType().GetProperty("Id")?.GetValue(childEntity)?.ToString();

                                            // if child entity is specified
                                            if (childId?.ToLower() != jchildId?.ToLower())
                                            {
                                                // id changed
                                                newChildEntity = _dynamicRepository.Get(property.PropertyType, jchildId);

                                                if (newChildEntity == null)
                                                {
                                                    validationResult.Add(new ValidationResult($"Entity with Id='{jchildId}' not found for `{jproperty.Path}`."));
                                                    break;
                                                }
                                            }

                                            if (childEntity != newChildEntity)
                                            {
                                                property.SetValue(entity, newChildEntity);
                                                if (childEntity != null && (cascadeAttr?.DeleteUnreferenced ?? false))
                                                {
                                                    await DeleteUnreferencedEntityAsync(childEntity, entity);
                                                }
                                            }
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }

                            if (!result)
                            {
                                validationResult.Add(new ValidationResult($"Value of '{jproperty.Path}' is not valid."));
                            }
                        }
                    }
                    else
                    {
                        validationResult.Add(new ValidationResult($"Property '{jproperty.Path}' not found for '{propertyName ?? entity.GetType().Name}'."));
                    }
                }
                catch (CascadeUpdateRuleException ex)
                {
                    validationResult.Add(new ValidationResult($"{ex.Message} for '{jproperty.Path}'"));
                }
                catch (Exception)
                {
                    validationResult.Add(new ValidationResult($"Value of '{jproperty.Path}' is not valid."));
                }
            }

            return !validationResult.Any();
        }

        private async Task<bool> DeleteUnreferencedEntityAsync(object entity, object parentEntity)
        {
            var typeShortAlias = entity.GetType().GetCustomAttribute<EntityAttribute>()?.TypeShortAlias ?? entity.GetType().FullName;
            var references = _entityPropertyRepository.GetAll().Where(x => x.EntityType == typeShortAlias);
            if (!references.Any())
                return false;

            var parentId = Shesha.Extensions.EntityExtensions.GetId(parentEntity);
            if (parentId == null)
                throw new CascadeUpdateRuleException("Parent object does not implement IEntity interface");

            var id = Shesha.Extensions.EntityExtensions.GetId(entity);
            if (id == null)
                throw new CascadeUpdateRuleException("Related object does not implement IEntity interface");

            var any = false;
            foreach (var reference in references)
            {
                var refType = _typeFinder.Find(x => x.Name == reference.EntityConfig.ClassName).FirstOrDefault();
                // Do not raise error becase some EntityConfig can be irrelevant
                if (refType == null) continue;

                var refParam = Expression.Parameter(refType);
                var query = Expression.Lambda(
                    Expression.Equal(
                        Expression.Property(Expression.Property(refParam, reference.Name), "Id"),
                        Expression.Constant(id is Guid ? (Guid)id : id is Int64 ? (Int64)id : id.ToString())
                        ),
                    refParam);

                var repoType = typeof(IRepository<,>).MakeGenericType(refType, refType.GetProperty("Id")?.PropertyType);
                var repo = _iocManager.Resolve(repoType);
                var where = (repoType.GetMethod("GetAll")?.Invoke(repo, null) as IQueryable).Where(query);

                var test = where.Any();

                if (refType.IsAssignableFrom(parentEntity.GetType()))
                {
                    var queryExclude = Expression.Lambda(
                        Expression.NotEqual(
                            Expression.Property(refParam, "Id"),
                            Expression.Constant(parentId is Guid ? (Guid)parentId : parentId is Int64 ? (Int64)parentId : parentId.ToString())
                            ),
                        refParam);
                    where = where.Where(queryExclude);
                }

                any = where.Any();
                if (any)
                    break;
            }

            if (!any)
            {
                await _dynamicRepository.DeleteAsync(entity);
                return true;
            }
            return false;
        }

        /*private class FormField
        {
            public static List<FormField> GetList(JProperty _formFields)
            {
                var list = new List<FormField>();
                var formFieldsArray = _formFields.Value as JArray;
                var formFields = formFieldsArray.Select(f => f.Value<string>()).ToList();
                foreach (var formField in formFields)
                {
                    var parts = formField.Split(".");
                    var field = new FormField() { Name = parts[0] };
                    list.Add(field);
                    foreach (var part in parts.Skip(1))
                    {
                        var hField = new FormField() { Name = part, Parent = field };
                        field.ch
                    }
                }
                return list;
            }

            public FormField Parent { get; set; }
            public string Name { get; set; }
            public List<FormField> FormFields { get; set; } = new List<FormField>();
        }*/
    }
}
