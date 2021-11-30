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
using System.Reflection;
using System.Text;

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
                DataType = epc?.GeneralType ?? GetGeneralDataType(property.DeclaringType, property.Name),
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

        private GeneralDataType GetGeneralDataType(Type containerType, string propertyName)
        {
            return containerType.IsEntityType()
                    ? EntityExtensions.GetGeneralPropertyType(containerType, propertyName)
                    : EntityConfigurationLoaderByReflection.GetGeneralDataType(containerType.GetProperty(propertyName));
        }
    }
}
