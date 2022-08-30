using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Extensions;
using Abp.Reflection;
using ElmahCore;
using log4net.Util;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json.Linq;
using NHibernate.Util;
using Shesha.Configuration.Runtime;
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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.DynamicEntities
{
    public class EntityModelBinder : IEntityModelBinder, ITransientDependency
    {
        private IDynamicRepository _dynamicRepository;
        private IMetadataProvider _metadataProvider;
        private IIocManager _iocManager;

        public EntityModelBinder(
            IDynamicRepository dynamicRepository,
            IMetadataProvider metadataProvider,
            IIocManager iocManager
            )
        {
            _dynamicRepository = dynamicRepository;
            _metadataProvider = metadataProvider;
            _iocManager = iocManager;
        }

        public bool BindProperties(
            JObject jobject,
            object entity,
            List<ValidationResult> validationResult,
            string propertyName = null)
        {
            var properties = entity.GetType().StripCastleProxyType()
                .GetProperties()
                .Where(p => p.CanWrite && p.Name != "Id")
                .ToList();

            validationResult ??= new List<ValidationResult>();

            foreach (var jproperty in jobject.Properties().ToList().Where(x => x.Name != "id"))
            {
                try
                {
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
                                case DataTypes.ReferenceListItem:
                                case DataTypes.Boolean:
                                case DataTypes.Guid:
                                //case DataTypes.Enum: // Enum binded as integer
                                    object parsedValue = null;
                                    result = Parser.TryParseToValueType(jproperty.Value.ToString(), property.PropertyType, out parsedValue, isDateOnly: propType.DataType == DataTypes.Date);
                                    if (result)
                                    {
                                        property.SetValue(entity, parsedValue);
                                    }
                                    break;
                                case DataTypes.Object:
                                    if (jproperty.Value is JObject childSimplyObject)
                                    {
                                        var newObject = Activator.CreateInstance(property.PropertyType);
                                        // create a new object
                                        if (BindProperties(childSimplyObject, newObject, validationResult, jproperty.Name))
                                            property.SetValue(entity, newObject);
                                    }
                                    else
                                    {
                                        property.SetValue(entity, null);
                                    }
                                    break;
                                case DataTypes.EntityReference:
                                    if (jproperty.Value is JObject childObject)
                                    {
                                        // Get the rules of cascade update
                                        var cascadeAttr = property.GetCustomAttribute<CascadeUpdateRulesAttribute>()
                                            ?? property.PropertyType.GetCustomAttribute<CascadeUpdateRulesAttribute>();

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
                                                if (!BindProperties(childObject, newChildEntity, validationResult, jproperty.Name))
                                                    break;
                                            }

                                            if (childEntity != newChildEntity)
                                            {
                                                property.SetValue(entity, newChildEntity);
                                                if (childEntity != null && (cascadeAttr?.DeleteUnreferenced ?? false))
                                                {
                                                    DeleteUnreferencedEntity(childEntity, validationResult);
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
                                                if (!BindProperties(childObject, childEntity, validationResult, jproperty.Name))
                                                    break;

                                                if (cascadeAttr?.CascadeRuleEntityFinder != null)
                                                {
                                                    // try to select entity by key fields
                                                    if (Activator.CreateInstance(cascadeAttr.CascadeRuleEntityFinder) is ICascadeRuleEntityFinder finder)
                                                    {
                                                        finder.IocManager = _iocManager;
                                                        var foundEntity = finder.FindEntity(new CascadeRuleEntityFinderInfo(childEntity));
                                                        if (foundEntity != null)
                                                        {
                                                            if (BindProperties(childObject, foundEntity, validationResult, jproperty.Name))
                                                                property.SetValue(entity, childEntity);
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
                                                    DeleteUnreferencedEntity(childEntity, validationResult);
                                                }
                                            }
                                        }
                                    }
                                    break;
                                #region // case DataTypes.MultiValueReferenceList:
                                /*case DataTypes.MultiValueReferenceList:
                                    var propertyValue = jproperty.Value.ToString();
                                    // Removing the redundant ',' from the hidden element.
                                    if (propertyValue.EndsWith(","))
                                        propertyValue = propertyValue.Substring(0, propertyValue.Length - 1);
                                    else if (propertyValue.StartsWith(","))
                                        propertyValue = propertyValue.Substring(1, propertyValue.Length - 1);
                                    else
                                        propertyValue.Replace(",,", ",");

                                    // Adding up the selected values to save to the property. (Each individual values should be a bitflag number)
                                    var valComponents = propertyValue.Split(',');
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
                                    result = entity.SetPropertyValue(property, totalVal.ToString());
                                    break;*/
                                #endregion
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

        private bool DeleteUnreferencedEntity(object entity, List<ValidationResult> validationResult)
        {
            return true;
        }
    }
}
