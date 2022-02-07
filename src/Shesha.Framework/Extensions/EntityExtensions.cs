﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Shesha.Configuration.Runtime;
using Shesha.Domain;
using Shesha.Domain.Attributes;
using Shesha.Reflection;
using Shesha.Services;
using Shesha.Utilities;

namespace Shesha.Extensions
{
    /// <summary>
    /// Entity extensions
    /// </summary>
    public static class EntityExtensions
    {
        /// <summary>
        /// Returns the DisplayName of the entity.
        /// </summary>
        /// <returns>Returns the DisplayName of the entity.</returns>
        public static string GetDisplayName<T>(this IEntity<T> entity)
        {
            if (entity == null)
                return "";

            var displayNamePropInfo = entity.GetType().GetEntityConfiguration().DisplayNamePropertyInfo;

            return displayNamePropInfo == null 
                ? "" 
                : entity.GetPropertyDisplayText(displayNamePropInfo.Name);
        }

        public static string GetReferenceListDisplayText<T, TValue>(this T owner, Expression<Func<T, TValue?>> expression) where TValue: struct, IConvertible
        {
            if (owner == null)
                return null;

            if (!(expression.Body is MemberExpression))
                throw new Exception("Expression should be MemberExpression");

            var memberExpression = (MemberExpression)expression.Body;

            if (memberExpression.Member is PropertyInfo prop)
            {
                var value = expression.Compile().Invoke(owner);
                if (value == null)
                    return null;
                var refListAttribute = prop.GetAttribute<ReferenceListAttribute>() ?? prop.PropertyType.GetUnderlyingTypeIfNullable().GetAttribute<ReferenceListAttribute>();
                if (refListAttribute != null)
                {
                    return StaticContext.IocManager.Resolve<IReferenceListHelper>().GetItemDisplayText(refListAttribute.Namespace, refListAttribute.ReferenceListName, value.Value.ToInt32(CultureInfo.InvariantCulture));
                }
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TId"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static object GetId<TId>(this IEntity<TId> entity)
        {
            return entity.Id;
        }

        public static object GetId(this object entity)
        {
            if (entity == null || !entity.IsEntity())
                return null;

            return entity is IEntity<Guid> guidEntity
                ? guidEntity.GetId()
                : entity is IEntity<Int64> int64Entity
                    ? int64Entity.GetId()
                    : entity is IEntity<string> stringEntity
                        ? stringEntity.GetId()
                        : null;
        }

        [Obsolete("To be reviewed")]
        public static string GetPropertyDisplayText(this object entity, string propertyName, string defaultValue = "")
        {
            try
            {
                var val = ReflectionHelper.GetPropertyValue(entity, propertyName, out object parentEntity, out var propInfo);
                if (val == null)
                    return defaultValue;

                var propConfig = parentEntity.GetType().GetEntityConfiguration()[propInfo.Name];
                var generalDataType = propConfig.GeneralType;

                entity = parentEntity;
                propertyName = propInfo.Name;

                switch (generalDataType)
                {
                    case GeneralDataType.Enum:
                        var itemValue = Convert.ToInt32(val);
                        return ReflectionHelper.GetEnumDescription(propConfig.EnumType, itemValue);
                    case GeneralDataType.ReferenceList:
                    {
                        var refListHelper = StaticContext.IocManager.Resolve<IReferenceListHelper>();
                        return refListHelper.GetItemDisplayText(propConfig.ReferenceListNamespace,  propConfig.ReferenceListName, (int)val);
                    }
                    case GeneralDataType.EntityReference:
                    {
                        var displayProperty = val.GetType().GetEntityConfiguration().DisplayNamePropertyInfo;
                        return displayProperty != null
                            ? displayProperty.GetValue(val)?.ToString()
                            : val.ToString();
                        }
                    default:
                        return GetPrimitiveTypePropertyDisplayText(val, propInfo, defaultValue);
                        //return val.ToString();
                }
                // todo: review and remove
                /*
                var propConfig = EntityConfiguration.Get(parentEntity.GetType())[propInfo.Name];
                var generalDataType = propConfig.GeneralType;

                entity = parentEntity;
                propertyName = propInfo.Name;

                switch (generalDataType)
                {
                    case GeneralDataType.Enum:
                        var itemValue = (int)val;
                        return ReflectionHelper.GetEnumDescription(propConfig.EnumType, itemValue);
                    case GeneralDataType.ReferenceList:
                        return entity.GetReferenceListItemName(propertyName, defaultValue);
                    case GeneralDataType.MultiValueReferenceList:
                        return entity.GetMultiValueReferenceListItemNames(propertyName, defaultValue);
                    case GeneralDataType.EntityReference:
                    case GeneralDataType.StoredFile:
                        var referencedEntity = entity.GetReferencedEntity(propertyName);

                        if (referencedEntity == null)
                            return defaultValue ?? "";
                        else
                            return referencedEntity.GetDisplayName();
                    default:
                        return GetPrimitiveTypePropertyDisplayText(val, propInfo, defaultValue);
                }
                */
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"An error occured whilst trying to retrieve DisplayText of property '{propertyName}' on type of '{entity.GetType().FullName}'.", ex);
            }
        }

        [Obsolete("Should use native MVC functionality and related DataAnnotations e.g. Display")]
        public static string GetPrimitiveTypePropertyDisplayText(object val, PropertyInfo propInfo, string defaultValue)
        {
            if (val == null || val.ToString().Length == 0)
            {
                if (defaultValue == null)
                    return string.Empty;
                else
                    return defaultValue;
            }

            string format = EntityExtensions.GetPropertyDisplayFormat(propInfo);

            return FormatValue(val, propInfo, format, false);
        }

        private static string GetPropertyDisplayFormat(PropertyInfo propInfo)
        {
            return propInfo.GetAttribute<DisplayFormatAttribute>(true)?.DataFormatString;
        }

        private static string FormatValue(object val, PropertyInfo propInfo, string format, bool isForEdit)
        {
            const string defaultFloatFormat = "N";
            const string defaultIntegerFormat = "D";
            const string defaultDateFormat = "dd/MM/yyyy";
            const string defaultDateTimeFormat = "dd/MM/yyyy HH:mm";

            var underlyingType = ReflectionHelper.GetUnderlyingTypeIfNullable(propInfo.PropertyType);

            if (underlyingType == typeof(string))
            {
                return string.IsNullOrEmpty(format)
                    ? val.ToString()
                    : string.Format(format, (string)val);
            }
            else if (underlyingType == typeof(DateTime))
            {
                var typedVal = (DateTime)val;
                string finalFormat;

                var dataTypeAtt = ReflectionHelper.GetPropertyAttribute<DataTypeAttribute>(propInfo);
                if (dataTypeAtt != null && dataTypeAtt.GetDataTypeName().Equals("Date", StringComparison.InvariantCultureIgnoreCase))
                {
                    finalFormat = StringHelper.FirstValue(format, defaultDateFormat);
                }
                else
                {
                    finalFormat = StringHelper.FirstValue(format, defaultDateTimeFormat);
                }
                return typedVal.ToString(finalFormat);
            }
            else if (underlyingType == typeof(int)
                || underlyingType == typeof(long)
                || underlyingType == typeof(short))
            {
                return Convert.ToInt64(val).ToString(StringHelper.FirstValue(format, defaultIntegerFormat));
            }
            else if (underlyingType == typeof(bool))
            {
                if (string.IsNullOrEmpty(format))
                    return val.ToString();

                var typedVal = (bool)val;
                var boolStrings = format.Split('|');
                if (boolStrings.Length != 2)
                    throw new Exception(
                        $"Exactly two tokens separated by '|' are expected as DisplayFormat for boolean property '{propInfo.Name}'.");
                return typedVal ? boolStrings[0] : boolStrings[1];
            }
            else if (underlyingType == typeof(decimal))
            {
                return ((decimal)val).ToString(StringHelper.FirstValue(format, defaultFloatFormat));
            }
            else if (underlyingType == typeof(Single)
                || underlyingType == typeof(Double))
            {
                return (Convert.ToDouble(val)).ToString(StringHelper.FirstValue(format, defaultFloatFormat));
            }
            else
            {
                return val.ToString();
            }
        }

        public static GeneralDataType GetGeneralDataType(this PropertyInfo propInfo)
        {
            return propInfo.DeclaringType.GetEntityConfiguration()[propInfo.Name].GeneralType;
        }

        /// <summary>
        /// Sets the value of the specified property.
        /// </summary>
        /// <param name="entity">Entity whose property is to be set.</param>
        /// <param name="propertyName">Name of the property to be set.</param>
        /// <param name="value">Value to set the property to as a string.</param>
        /// <param name="format"></param>
        /// <returns>Returns true if the property was set successfully, else returns false e.g. in case of unexpected value.</returns>
        [Obsolete("To be reviewed")]
        public static bool SetPropertyValue(this object entity, string propertyName, string value, string format = null)
        {
            var propInfo = ReflectionHelper.GetProperty(entity, propertyName, out var propertyEntity);

            string lastPropertyNameSegment = propertyName;
            if (propertyName.Contains('.'))
            {
                var lastDotIndex = propertyName.LastIndexOf('.');
                lastPropertyNameSegment = propertyName.Substring(lastDotIndex + 1, propertyName.Length - lastDotIndex - 1);
            }

            try
            {
                bool hasNewEntityIndicator;

                object parsedValue;
                var res = Parser.TryParseToTargetPropertyType(value, propInfo, out parsedValue, out hasNewEntityIndicator, format);
                if (!res)
                    return false;

                if (!hasNewEntityIndicator) // hasNewEntityIndicator will only be true for Entity properties in which case it should not be set
                    propInfo.SetValue(propertyEntity, parsedValue, null);

                return true;
            }
            catch (Exception)
            {
                //Logger.WriteLog(LogLevel.DEBUG, string.Format("Unable to set property '{0}.{1}' to value '{2}'.", propertyEntity.GetType().Name, propInfo.Name, value, ex));
                return false;
            }
        }

        public static List<T> GetFullChain<T>(this T entity, Func<T, T> parentFunc) where T : class
        {
            return entity.GetClosestChain(parentFunc, e => e == null);
        }

        public static List<T> GetClosestChain<T>(this T entity, Func<T, T> parentFunc, Func<T, bool> condition) where T : class
        {
            var chain = new List<T>();
            var currentEntity = entity;
            while (currentEntity != null)
            {
                chain.Add(currentEntity);

                if (condition.Invoke(currentEntity))
                    break;

                var parent = parentFunc.Invoke(currentEntity);

                currentEntity = !chain.Contains(parent) ? parent : null;
            }
            return chain;
        }

        public static T Closest<T>(this T entity, Func<T, T> parentFunc, Func<T, bool> condition) where T : class
        {
            if (entity == null)
                return null;
            var item = entity.GetClosestChain(parentFunc, condition).LastOrDefault();

            return condition.Invoke(item) ? item : null;
        }

        public static GeneralDataType GetGeneralPropertyType(Type type, string propertyName)
        {
            return type.GetEntityConfiguration()[propertyName].GeneralType;
        }

        /// <summary>
        /// Returns TypeShortAlias of the specified entity
        /// </summary>
        /// <exception cref="ConfigurationErrorsException">Thrown when entity has no TypeShortAlias</exception>
        public static string GetTypeShortAlias<TId>(this IEntity<TId> entity)
        {
            return entity.GetType().GetEntityConfiguration().TypeShortAlias;
        }

        /// <summary>
        /// Loads the specified entity from the DB with cast to the specified class (if needed), is useful when the entity is loaded from the DB using the base class (e.g. Employee is loaded from the DB as a Person)
        /// </summary>
        public static TChild LoadAs<TChild, TParent>(this TParent entity)
            where TParent : Entity<Guid>
            where TChild : TParent
        {
            if (entity == null)
                return null;

            if (entity is TChild casted)
                return casted;

            var service = StaticContext.IocManager.Resolve<IRepository<TChild, Guid>>();
            return service.Get(entity.Id);
        }

        /// <summary>
        /// Returns a string that represents a fully qualified entity Identifier.
        /// The identifier is in the following format: '[The entity's Type Assembly qualitfied name]|[entity Id]'
        /// </summary>
        /// <param name="entity">Entity for which a fully qualified identifier is required.</param>
        /// <returns>Returns a string that represents a fully qualified entity Identifier.</returns>
        public static string FullyQualifiedEntityId<TEntity, TId>(this TEntity entity) where TEntity: IEntity<TId>
        {
            var entityType = entity.GetType().StripCastleProxyType();
            return entityType.AssemblyQualifiedName + "|" + entity.GetId().ToString();
        }

        #region Multivalue reference list todo: review

        /// <summary>
        /// Returns a comma separated list of Reference List Item referenced by the property.
        /// </summary>
        /// <param name="propertyName">Name of the Reference List Item property.</param>
        /// <param name="defaultValue">Value to return if reference list item value is null or empty.</param>
        /// <returns>If property value is null returns the <paramref name="defaultValue"/>, else
        /// returns a comma separated list of the Rerefence List Items.</returns>
        [Obsolete("Should use native MVC functionality and related DataAnnotations e.g. Display")]
        public static string GetMultiValueReferenceListItemNames(this object entity, string propertyName, string defaultValue = null, string separator = ", ")
        {
            var items = GetMultiValueReferenceListItemNamesAr(entity, propertyName);
            if (defaultValue != null && items.Length == 0)
            {
                return defaultValue;
            }
            else
            {
                return String.Join(separator, items);
            }
        }

        /// <summary>
        /// Returns a string array of allReference List items referenced by the property.
        /// </summary>
        /// <param name="propertyName">Name of the Reference List Item property.</param>
        /// <param name="defaultValue">Value to return if reference list item value is null or empty.</param>
        /// <returns>If property value is null or zero-length string, returns a zero length array, else
        /// returns a list Rerefence List items specified by the property value.</returns>
        public static string[] GetMultiValueReferenceListItemNamesAr(this object entity, string propertyName)
        {
            string refListNamespace, refListName;
            var itemsInt = GetMultiValueReferenceListItemValuesAr(entity, propertyName, out refListNamespace, out refListName);

            var result = new string[itemsInt.Length];

            var refListHelper = StaticContext.IocManager.Resolve<IReferenceListHelper>();

            for (int i = 0; i < itemsInt.Length; i++)
            {
                if (refListName == "" && refListNamespace == "")
                    result[i] = ReflectionHelper.GetEnumDescription(ReflectionHelper.GetUnderlyingTypeIfNullable(entity.GetType().GetProperty(propertyName).PropertyType), itemsInt[i]);
                else
                    result[i] = refListHelper.GetItemDisplayText(refListNamespace, refListName, itemsInt[i]);
            }

            return result;
        }

        private static Int64[] GetMultiValueReferenceListItemValuesAr(this object entity, string propertyName, out string refListNamespace, out string refListName)
        {
            var propInfo = ReflectionHelper.GetProperty(entity, propertyName, out var propertyEntity);

            var propConfig = entity.GetType().GetEntityConfiguration()[propertyName];
            if (propConfig.GeneralType != GeneralDataType.MultiValueReferenceList)
                throw new InvalidOperationException($"GetMultiValueReferenceListItemNamesAr cannot be called on property '{propertyName}' of type '{entity.GetType().FullName}' as property is not defined as a MultiValueReferenceList property.");

            refListNamespace = propConfig.ReferenceListNamespace;
            refListName = propConfig.ReferenceListName;

            var propValue = propInfo.GetValue(propertyEntity, null);

            if (propValue == null)
                return new Int64[] { };

            long intVal;

            try
            {
                intVal = Convert.ToInt64(propValue);
            }
            catch
            {
                throw new ConfigurationException($"Property '{propertyName}' on type '{entity.GetType().FullName}' is marked as MultiValueReferenceList but it's type cannot be cast to 'int'.");
            }

            if (refListName == "" && refListNamespace == "")
            {
                return
                    ReflectionHelper.FlagEnumToListOfValues(
                        ReflectionHelper.GetUnderlyingTypeIfNullable(propInfo.PropertyType), intVal).ToArray();
            }

            return DecomposeIntoBitFlagComponents(intVal);
        }

        /// <summary>
        /// Decomposes an integers into an array of its constituent BitFlag values (i.e. 1, 2, 4, etc...).
        /// </summary>
        public static Int64[] DecomposeIntoBitFlagComponents(Int64 val)
        {
            return DecomposeIntoBitFlagComponents((long?)val);
        }

        /// <summary>
        /// Decomposes an integers into an array of its constituent BitFlag values (i.e. 1, 2, 4, etc...).
        /// </summary>
        public static Int64[] DecomposeIntoBitFlagComponents(long? val)
        {
            if (!val.HasValue || val == 0)
            {
                return new Int64[] { };
            }
            else
            {
                var resultAr = new List<Int64>();

                Int64 currentVal = 1;
                while (currentVal <= val)
                {
                    if ((val & currentVal) == currentVal)
                        resultAr.Add(currentVal);
                    currentVal = currentVal * 2;
                }

                return resultAr.ToArray();
            }
        }

        #endregion
    }
}
