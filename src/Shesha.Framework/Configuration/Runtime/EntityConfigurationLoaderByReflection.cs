﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using NHibernate.Util;
using Shesha.Domain;
using Shesha.Domain.Attributes;
using Shesha.Extensions;
using Shesha.Reflection;
using Shesha.Utilities;

namespace Shesha.Configuration.Runtime
{
    /// <summary>
    /// Loads entity configuration information using reflection.
    /// </summary>
    public class EntityConfigurationLoaderByReflection
    {
        public void LoadConfiguration([NotNull]EntityConfiguration config)
        {
            LoadEntityConfiguration(config);

            // Loading property configuration.
            var ownProps = config.EntityType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance).ToList();
            var props = config.EntityType.GetProperties().ToList();

            var propsWithoutDuplicates = props.GroupBy(p => p.Name, (name, duplicates) =>
                duplicates
                    .Select(dp => new { IsOwn = ownProps.Contains(dp), Prop = dp })
                    .OrderByDescending(dp => dp.IsOwn ? 1 : 0)
                    .Select(dp => dp.Prop)
                    .FirstOrDefault()
                ).ToList();
            if (propsWithoutDuplicates.Count < props.Count)
            {
                var duplicates = props.Where(p => !propsWithoutDuplicates.Contains(p)).ToList();
                props = propsWithoutDuplicates;
            }

            foreach (var prop in props)
            {
                try
                {
                    var propConfig = new PropertyConfiguration(config.EntityType);
                    LoadPropertyConfiguration(prop, propConfig);
                    config.Properties.Add(prop.Name, propConfig);
                }
                catch
                {
                    throw;
                }
            }
        }

        private void LoadEntityConfiguration(EntityConfiguration config)
        {
            var entityAtt = config.EntityType.GetUniqueAttribute<EntityAttribute>();

            // Defaulting the values.
            config.ControllerName = config.EntityType.Name;
            config.DetailsActionName = "Details";
            config.CreateActionName = "Create";
            config.EditActionName = "Edit";
            config.InactivateActionName = "Inactivate";
            config.DeleteActionName = "Delete";

            if (entityAtt != null)
            {
                config.FriendlyName = string.IsNullOrEmpty(entityAtt.FriendlyName)
                                          ? config.EntityType.Name // Fall back to type name when friendly name is not specified
                                          : entityAtt.FriendlyName;

                /* todo: review CRUD functionality and uncomment/remove
                config.ControllerName = !string.IsNullOrWhiteSpace(entityAtt.ControllerName) ? entityAtt.ControllerName : config.ControllerName;
                config.DetailsActionName = !string.IsNullOrWhiteSpace(entityAtt.DetailsActionName) ? entityAtt.DetailsActionName : config.DetailsActionName;
                config.CreateActionName = !string.IsNullOrWhiteSpace(entityAtt.CreateActionName) ? entityAtt.CreateActionName : config.CreateActionName;
                config.EditActionName = !string.IsNullOrWhiteSpace(entityAtt.EditActionName) ? entityAtt.EditActionName : config.EditActionName;
                config.InactivateActionName = !string.IsNullOrWhiteSpace(entityAtt.InactivateActionName) ? entityAtt.InactivateActionName : config.InactivateActionName;
                config.DeleteActionName = !string.IsNullOrWhiteSpace(entityAtt.DeleteActionName) ? entityAtt.DeleteActionName : config.DeleteActionName;
                config.DrillToView = entityAtt.DrillToView;
                */
            }

            /*
            config.CreatedUserPropertyInfo = ReflectionHelper.FindPropertyWithUniqueAttribute(config.EntityType, typeof(CreatedUserAttribute), typeof(string));
            config.CreatedTimestampPropertyInfo = ReflectionHelper.FindPropertyWithUniqueAttribute(config.EntityType, typeof(CreatedTimestampAttribute), typeof(DateTime?));
            config.LastUpdatedUserPropertyInfo = ReflectionHelper.FindPropertyWithUniqueAttribute(config.EntityType, typeof(LastUpdatedUserAttribute), typeof(string));
            config.LastUpdatedTimestampPropertyInfo = ReflectionHelper.FindPropertyWithUniqueAttribute(config.EntityType, typeof(LastUpdatedTimestampAttribute), typeof(DateTime?));
            config.DefaultSortOrderPropertyInfo = ReflectionHelper.FindPropertyWithUniqueAttribute(config.EntityType, typeof(SearchOrderAttribute), typeof(DateTime?));
            */

            config.TypeShortAlias = GetTypeShortAlias(config.EntityType);

            LoadChangeLoggingConfiguration(config);
            config.DisplayNamePropertyInfo = GetDisplayNamePropertyInfo(config.EntityType);
        }

        private static void LoadPropertyConfiguration(PropertyInfo prop, PropertyConfiguration propConfig)
        {
            propConfig.PropertyInfo = prop;
            propConfig.GeneralType = GetGeneralDataType(prop);
            propConfig.Category = prop.GetAttribute<CategoryAttribute>()?.Category;

            switch (propConfig.GeneralType)
            {
                case GeneralDataType.Numeric:
                case GeneralDataType.ReferenceList:
                    var refListAtt = ReflectionHelper.GetPropertyAttribute<ReferenceListAttribute>(prop, true);
                    if (refListAtt == null)
                    {
                        if (prop.PropertyType.IsEnum && prop.PropertyType.HasAttribute<ReferenceListAttribute>())
                            refListAtt = prop.PropertyType.GetAttribute<ReferenceListAttribute>();
                        else
                        {
                            var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType);
                            if (underlyingType != null && underlyingType.IsEnum &&
                                underlyingType.HasAttribute<ReferenceListAttribute>())
                                refListAtt = underlyingType.GetAttribute<ReferenceListAttribute>();
                        }
                    }

                    if (refListAtt != null)
                    {
                        propConfig.ReferenceListName = refListAtt.ReferenceListName;
                        propConfig.ReferenceListNamespace = refListAtt.Namespace;
                        propConfig.ReferenceListOrderByName = refListAtt.OrderByName;
                    }
                    break;
                case GeneralDataType.Enum:
                    var enumType = prop.PropertyType;
                    if (enumType.IsNullable())
                        enumType = Nullable.GetUnderlyingType(prop.PropertyType);
                    propConfig.EnumType = enumType;
                    break;
                case GeneralDataType.MultiValueReferenceList:
                    var mvRefListAtt = ReflectionHelper.GetPropertyAttribute<MultiValueReferenceListAttribute>(prop, true);
                    propConfig.ReferenceListName = mvRefListAtt.ReferenceListName;
                    propConfig.ReferenceListNamespace = mvRefListAtt.Namespace;
                    break;
                case GeneralDataType.EntityReference:
                    propConfig.EntityReferenceType = prop.PropertyType;
                    break;
                default:
                    break;
            }

            propConfig.Label = GetPropertyLabel(prop);

            LoadChangeLoggingPropertyConfiguration(prop, propConfig);
        }

        private static PropertyInfo GetDisplayNamePropertyInfo(Type type)
        {
            var displayNamePropInfo = ReflectionHelper.FindPropertyWithUniqueAttribute(type, typeof(EntityDisplayNameAttribute));

            if (displayNamePropInfo == null)
            {
                // Will automatically look for any properties called 'Name' or 'DisplayName'.
                displayNamePropInfo = ReflectionHelper.GetProperty(type, "Name");

                if (displayNamePropInfo == null)
                    displayNamePropInfo = ReflectionHelper.GetProperty(type, "DisplayName");
            }

            return displayNamePropInfo;
        }

        private static string GetTypeShortAlias(Type entityType)
        {
            var att = entityType.GetUniqueAttribute<EntityAttribute>();

            if (att == null
                || string.IsNullOrEmpty(att.TypeShortAlias))
            {
                return null;
            }
            else
            {
                return att.TypeShortAlias;
            }
        }

        private void LoadChangeLoggingConfiguration(EntityConfiguration config)
        {
            return;
            /* todo: check ABP audit logging and uncomment/remove
            var propertiesNeedToLogChangesFor = config.EntityType.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(prop => prop.GetCustomAttributes(typeof(LogChangesAttribute), true).Length > 0).ToList(); // BindingFlags.DeclaredOnly - removed to log changes for inherited properties

            if (!propertiesNeedToLogChangesFor.Any())
            {
                config.HasPropertiesNeedToLogChangesFor = false;
            }
            else
            {
                config.HasPropertiesNeedToLogChangesFor = true;
                if (string.IsNullOrEmpty(config.TypeShortAlias))
                    throw new ConfigurationException(string.Format("Properties have been marked for Audit on entity '{0}' but a TypeShortAlias which is required for auditing has not been assigned for the entity. Tip: Apply the Entity attribute to the entity class to assign the TypeShortAlias.", config.EntityType.FullName));

                foreach (var prop in propertiesNeedToLogChangesFor)
                {
                    foreach (var attribute in prop.GetCustomAttributes(typeof(LogChangesAttribute), true).Cast<LogChangesAttribute>())
                    {
                        // Ensuring that the LogChanges attribute is not applied to Collections/Lists.
                        if (ReflectionHelper.IsCollectionType(prop.PropertyType))
                        {
                            throw new ConfigurationException(string.Format("Property '{0}' on entity '{1}' cannot be marked with LogChange Attribute as logging of changes to a collection is not supported.", prop.Name, config.EntityType.FullName));
                        }

                        var changeLogConfig = config.ChangeLogConfigurations.FirstOrDefault(o => o.Namespace == attribute.Namespace);

                        if (changeLogConfig == null)
                        {
                            changeLogConfig = new EntityConfiguration.PropertySetChangeLoggingConfiguration();
                            changeLogConfig.Namespace = attribute.Namespace;
                            config.ChangeLogConfigurations.Add(changeLogConfig);
                        }

                        if (changeLogConfig.AuditedProperties.FirstOrDefault(propLoggingConfig => propLoggingConfig == prop.Name) != null)
                        {
                            throw new ConfigurationException(string.Format("Property '{0}' on entity '{1}' cannot have more than one LogChange Attribute.", prop.Name, config.EntityType.FullName));
                        }
                        else
                        {
                            changeLogConfig.AuditedProperties.Add(prop.Name);
                        }
                    }
                }
            }
            */
        }

        public static GeneralDataType GetGeneralDataType(PropertyInfo propInfo)
        {
            if (IsPropertyStoredFile(propInfo))
                return GeneralDataType.StoredFile;
            if (propInfo.PropertyType.IsEntityType())
                return GeneralDataType.EntityReference;

            if (propInfo.HasAttribute<MultiValueReferenceListAttribute>())
                return GeneralDataType.MultiValueReferenceList;

            if (propInfo.IsReferenceListProperty())
                return GeneralDataType.ReferenceList;

            if (propInfo.PropertyType.IsEnum)
                return GeneralDataType.Enum;
            var underlyingPropType = ReflectionHelper.GetUnderlyingTypeIfNullable(propInfo.PropertyType);

            if (underlyingPropType == typeof(string))
            {
                return GeneralDataType.Text;
            }
            else if (underlyingPropType == typeof(DateTime))
            {
                var dataTypeAtt = ReflectionHelper.GetPropertyAttribute<DataTypeAttribute>(propInfo);

                if (dataTypeAtt != null &&
                    dataTypeAtt.GetDataTypeName().Equals("Date", StringComparison.InvariantCultureIgnoreCase))
                {
                    return GeneralDataType.Date;
                }
                else
                {
                    return GeneralDataType.DateTime;
                }
            }
            else if (underlyingPropType == typeof(TimeSpan))
            {
                return GeneralDataType.Time;
            }
            else if (underlyingPropType == typeof(bool))
            {
                return GeneralDataType.Boolean;
            }
            else if (underlyingPropType == typeof(Guid))
            {
                return GeneralDataType.Guid;
            }
            else if (underlyingPropType == typeof(int)
                     || underlyingPropType == typeof(long)
                     || underlyingPropType == typeof(short)
                     || underlyingPropType == typeof(Single)
                     || underlyingPropType == typeof(Double)
                     || underlyingPropType == typeof(decimal)
                )
            {
                return GeneralDataType.Numeric;
            }
            else if (underlyingPropType.IsSubtypeOfGeneric(typeof(IList<>)) ||  underlyingPropType.IsSubtypeOfGeneric(typeof(ICollection<>)))
            {
                return GeneralDataType.List;
            }
            {
                /*
                Logger.WriteLog(LogLevel.ERROR,
                    string.Format("Property: {0}, type '{1}' not accounted for.", propInfo.Name,
                        propInfo.PropertyType.FullName));
                */
                return GeneralDataType.Text;
            }

        }

        private static bool IsPropertyStoredFile(PropertyInfo propInfo)
        {
            return typeof(StoredFile).IsAssignableFrom(propInfo.PropertyType);
        }

        private static string GetPropertyLabel(PropertyInfo propInfo)
        {
            try
            {
                var labelAtt = ReflectionHelper.GetPropertyAttribute<DisplayAttribute>(propInfo);

                if (labelAtt != null && !string.IsNullOrWhiteSpace(labelAtt.Name))
                {
                    return labelAtt.Name;
                }
                else
                {
                    return propInfo.Name.SplitUpperCaseToString();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"An error occured whilst trying to retrieve label of property '{propInfo.Name}' on type of '{propInfo.DeclaringType.FullName}'.", ex);
            }
        }

        private static void LoadChangeLoggingPropertyConfiguration(PropertyInfo propInfo, PropertyConfiguration propConfig)
        {
            propConfig.LogChanges = false;
            /* todo: review ABP logging and uncomment/remove
            var att = ReflectionHelper.GetPropertyAttribute<LogChangesAttribute>(propInfo);
            if (att == null)
            {
                propConfig.LogChanges = false;
            }
            else
            {
                propConfig.LogChanges = true;
                propConfig.FixedDescriptionOnChange = att.FixedDescription;
                propConfig.DetailPropertyOnChange = att.DetailPropertyInDescription;
                propConfig.DetailOldValueOnChange = att.DetailOldValueInDescription;
                propConfig.DetailNewValueOnChange = att.DetailNewValueInDescription;
                propConfig.AuditLogEntryNamespaceOnChange = att.Namespace;
            }
            */
        }
    }
}
