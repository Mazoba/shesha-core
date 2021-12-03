using Abp.Dependency;
using Shesha.Configuration.Runtime;
using Shesha.Extensions;
using Shesha.Metadata.Dtos;
using Shesha.Reflection;
using Shesha.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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

            var allProps = containerType.GetProperties(flags);

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

            var result = new PropertyMetadataDto
            {
                Path = path,
                Label = ReflectionHelper.GetDisplayName(property),
                Description = ReflectionHelper.GetDescription(property),
                IsVisible = property.GetAttribute<BrowsableAttribute>()?.Browsable ?? true,
                Required = property.HasAttribute<RequiredAttribute>(),
                Readonly = property.GetAttribute<ReadOnlyAttribute>()?.IsReadOnly ?? false,
                DataType = GetDataType(property),
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

        /// inheritedDoc
        public string GetDataType(PropertyInfo propInfo)
        {
            var propType = ReflectionHelper.GetUnderlyingTypeIfNullable(propInfo.PropertyType);
            //var isNullable = ReflectionHelper.IsNullableType(propInfo.PropertyType);

            if (propType == typeof(Guid)) 
                return DataTypes.Uuid;

            if (propType == typeof(string))
                return DataTypes.String;

            if (propType == typeof(DateTime))
            {
                var dataTypeAtt = ReflectionHelper.GetPropertyAttribute<DataTypeAttribute>(propInfo);
                return dataTypeAtt != null && dataTypeAtt.GetDataTypeName().Equals("Date", StringComparison.InvariantCultureIgnoreCase)
                    ? DataTypes.Date
                    : DataTypes.DateTime;
            }

            if (propType == typeof(TimeSpan))
                return DataTypes.Time;

            if (propType == typeof(bool))
                return DataTypes.Boolean;

            if (propInfo.IsReferenceListProperty())
                return DataTypes.RefListValue;

            if (propType.IsEntityType())
                return DataTypes.EntityReference;

            // note: numeric datatypes mapping is based on the OpenApi 3
            if (propType  == typeof(int) || propType == typeof(byte) || propType == typeof(short))
                return DataTypes.Int32;

            if (propType == typeof(Int64))
                return DataTypes.Int64;

            if (propType == typeof(Single) || propType == typeof(float))
                return DataTypes.Float;

            if (propType == typeof(double) || propType == typeof(decimal))
                return DataTypes.Double;

            if (propType.IsSubtypeOfGeneric(typeof(IList<>)) || propType.IsSubtypeOfGeneric(typeof(ICollection<>)))
                return DataTypes.Array;

            return "unknown";
        }
    }
}