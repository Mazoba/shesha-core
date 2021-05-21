using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Shesha.Reflection;

namespace Shesha.Web.FormsDesigner.Legacy
{
    public class ComponentModelContractResolver : DefaultContractResolver
    {
        public ComponentModelContractResolver(SerializationMode mode = SerializationMode.AllProperties)
        {
            Mode = mode;

            NamingStrategy = new CamelCaseNamingStrategy
            {
                ProcessDictionaryKeys = true,
                OverrideSpecifiedNames = true
            };
        }

        public SerializationMode Mode { get; set; }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (Mode == SerializationMode.CustomProperties)
            {
                property.ShouldSerialize = instance => property.DeclaringType != typeof(ComponentModelBase) && !property.PropertyName.Equals("ChildComponents", StringComparison.InvariantCultureIgnoreCase);
                property.ShouldDeserialize = instance => property.DeclaringType != typeof(ComponentModelBase);
            }

            //property.PropertyName = GetPropertyName(property);

            return property;
        }

        /*
        private string GetPropertyName(JsonProperty property)
        {
            var jsonPropertyAttribute = property.AttributeProvider.GetAttributes(false).OfType<JsonPropertyAttribute>().FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(jsonPropertyAttribute?.PropertyName))
                return property.PropertyName; // already applied

            var componentType = property.DeclaringType.GetAttribute<FormComponentPropsAttribute>()?.ComponentType;

            return !string.IsNullOrWhiteSpace(componentType)
                ? componentType + "-" + property.PropertyName
                : property.PropertyName;
        }
        */

        protected override JsonContract CreateContract(Type objectType)
        {
            return base.CreateContract(objectType.StripCastleProxyType());
        }

        public enum SerializationMode
        {
            AllProperties,
            CustomProperties
        }
    }

}
