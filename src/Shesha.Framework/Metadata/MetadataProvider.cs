using Abp.Dependency;
using Shesha.Configuration.Runtime;
using Shesha.Domain;
using Shesha.Extensions;
using Shesha.Metadata.Dtos;
using Shesha.Reflection;
using Shesha.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.Metadata
{
    /// <summary>
    /// Metadata provider
    /// </summary>
    public class MetadataProvider: IMetadataProvider, ITransientDependency
    {
        private readonly IEntityConfigurationStore _entityConfigurationStore;

        public MetadataProvider(IEntityConfigurationStore entityConfigurationStore)
        {
            _entityConfigurationStore = entityConfigurationStore;
        }

        /// inheritedDoc
        public List<PropertyMetadataDto> GetProperties(Type containerType)
        {
            var flags = BindingFlags.Public | BindingFlags.Instance;

            var allProps = containerType.GetProperties(flags).OrderBy(p => p.Name).ToList();
            if (containerType.IsEntityType())
                allProps = allProps.Where(p => MappingHelper.IsPersistentProperty(p)).ToList();

            var allPropsMetadata = allProps.Select(p => GetPropertyMetadata(p)).ToList();

            var result = allPropsMetadata
                .OrderBy(e => e.Path)
                .ToList();

            return result;
        }

        /// inheritedDoc
        public PropertyMetadataDto GetPropertyMetadata(PropertyInfo property)
        {
            var path = property.Name;

            var entityConfig = property.DeclaringType != null && property.DeclaringType.IsEntityType()
                ? _entityConfigurationStore.Get(property.DeclaringType)
                : null;
            var epc = entityConfig?[property.Name];


            var dataType = GetDataType(property);
            var result = new PropertyMetadataDto
            {
                Path = path,
                Label = ReflectionHelper.GetDisplayName(property),
                Description = ReflectionHelper.GetDescription(property),
                IsVisible = property.GetAttribute<BrowsableAttribute>()?.Browsable ?? true,
                Required = property.HasAttribute<RequiredAttribute>(),
                Readonly = property.GetAttribute<ReadOnlyAttribute>()?.IsReadOnly ?? false,
                DataType = dataType.DataType,
                DataFormat = dataType.DataFormat,
                EntityTypeShortAlias = property.PropertyType.IsEntityType()
                    ? _entityConfigurationStore.Get(property.PropertyType)?.SafeTypeShortAlias
                    : null,
                ReferenceListName = epc?.ReferenceListName,
                ReferenceListNamespace = epc?.ReferenceListNamespace,
                EnumType = epc?.EnumType,
                OrderIndex = property.GetAttribute<DisplayAttribute>()?.GetOrder() ?? -1,
                //ConfigurableByUser = property.GetAttribute<BindableAttribute>()?.Bindable ?? true,
                //GroupName = ReflectionHelper.get(declaredProperty ?? property),
            };

            return result;
        }

        private string GetStringFormat(PropertyInfo propInfo) 
        {
            var dataTypeAtt = ReflectionHelper.GetPropertyAttribute<DataTypeAttribute>(propInfo);

            switch (dataTypeAtt?.DataType)
            {
                case DataType.Password:
                    return StringFormats.Password;
                case DataType.Text:
                    return StringFormats.Singleline;
                case DataType.MultilineText:
                    return StringFormats.Multiline;
                case DataType.Html:
                    return StringFormats.Html;
                case DataType.EmailAddress:
                    return StringFormats.EmailAddress;
                case DataType.PhoneNumber:
                    return StringFormats.PhoneNumber;
                case DataType.Url:
                    return StringFormats.Url;
                default: 
                    return StringFormats.Singleline;
            }
        }

        /// inheritedDoc
        public DataTypeInfo GetDataType(PropertyInfo propInfo)
        {
            var propType = ReflectionHelper.GetUnderlyingTypeIfNullable(propInfo.PropertyType);
            //var isNullable = ReflectionHelper.IsNullableType(propInfo.PropertyType);

            if (propType == typeof(Guid)) 
                return new DataTypeInfo(DataTypes.Guid);

            var dataTypeAtt = ReflectionHelper.GetPropertyAttribute<DataTypeAttribute>(propInfo);

            // for enums - use underlaying type
            if (propType.IsEnum)
                propType = propType.GetEnumUnderlyingType();

            if (propType == typeof(string)) 
            {
                return new DataTypeInfo(DataTypes.String, GetStringFormat(propInfo));
            }

            if (propType == typeof(DateTime))
            {
                return dataTypeAtt != null && dataTypeAtt.GetDataTypeName().Equals("Date", StringComparison.InvariantCultureIgnoreCase)
                    ? new DataTypeInfo(DataTypes.Date)
                    : new DataTypeInfo(DataTypes.DateTime);
            }

            if (propType == typeof(TimeSpan))
                return new DataTypeInfo(DataTypes.Time);

            if (propType == typeof(bool))
                return new DataTypeInfo(DataTypes.Boolean);

            if (propInfo.IsReferenceListProperty())
                return new DataTypeInfo(DataTypes.ReferenceListItem);

            if (propType.IsEntityType())
                return new DataTypeInfo(DataTypes.EntityReference);

            // note: numeric datatypes mapping is based on the OpenApi 3
            if (propType  == typeof(int) || propType == typeof(byte) || propType == typeof(short))
                return new DataTypeInfo(DataTypes.Number, NumberFormats.Int32);

            if (propType == typeof(Int64))
                return new DataTypeInfo(DataTypes.Number, NumberFormats.Int64);

            if (propType == typeof(Single) || propType == typeof(float))
                return new DataTypeInfo(DataTypes.Number, NumberFormats.Float);

            if (propType == typeof(double) || propType == typeof(decimal))
                return new DataTypeInfo(DataTypes.Number, NumberFormats.Double);

            if (propType.IsSubtypeOfGeneric(typeof(IList<>)) || propType.IsSubtypeOfGeneric(typeof(ICollection<>)))
                return new DataTypeInfo(DataTypes.Array);

            if (propType.IsClass)
                return new DataTypeInfo(DataTypes.Object);

            throw new NotSupportedException($"Data type not supported: {propType.FullName}");
        }

        /*
        /// <summary>
        /// Returns .Net type that is used to store data for the specified <paramref name="dataType"/> and <paramref name="dataFormat"/>
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="dataFormat"></param>
        /// <returns></returns>
        public static Type GetType(string dataType, string dataFormat)
        {
            switch (dataType) 
            {
                case DataTypes.Guid:
                    return typeof(Guid);
                case DataTypes.String:
                    return typeof(string);
                case DataTypes.Date:
                case DataTypes.DateTime:
                    return typeof(DateTime);
                case DataTypes.Time:
                    return typeof(TimeSpan);
                case DataTypes.Boolean:
                    return typeof(bool);
                case DataTypes.ReferenceListItem:
                    return typeof(Int64);

                case DataTypes.Number:
                {
                    switch (dataFormat) 
                    {
                        case NumberFormats.Int32:
                            return typeof(int);
                        case NumberFormats.Int64:
                            return typeof(Int64);
                        case NumberFormats.Float:
                            return typeof(float);
                        case NumberFormats.Double:
                            return typeof(decimal);
                        default: 
                            return typeof(decimal);
                    }
                }

                case DataTypes.EntityReference:
                case DataTypes.Array:
                default:
                    throw new NotSupportedException($"Data type not supported: {dataType}");
            }
        }
        */
    }
}