using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Shesha.Web.FormsDesigner.Legacy
{
    public class ComponentModelJsonConverter : JsonConverter
    {
        private readonly Dictionary<string, Type> _componentTypes;

        public ComponentModelJsonConverter(Dictionary<string, Type> componentTypes)
        {
            _componentTypes = componentTypes;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObject = JToken.ReadFrom(reader);
            var type = jObject["type"].ToObject<string>();

            var componentType = _componentTypes[type] ?? typeof(ComponentModelBase);
            var instance = Activator.CreateInstance(componentType);

            serializer.Populate(jObject.CreateReader(), instance);

            return instance;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(ComponentModelBase).IsAssignableFrom(objectType);
        }

        public override bool CanRead => true;
        public override bool CanWrite => false;
    }

}
