using Abp.Dependency;
using Newtonsoft.Json;
using Shesha.DynamicEntities.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.DynamicEntities
{
    /// inheritedDoc
    public class SerializationManager : ISerializationManager, ITransientDependency
    {
        /// inheritedDoc
        public object DeserializeProperty(Type propertyType, string value)
        {
            return JsonConvert.DeserializeObject(value, propertyType);
        }

        /// inheritedDoc
        public string SerializeProperty(EntityPropertyDto propertyDto, object value)
        {
            // todo: extract interface from EntityPropertyDto that describes data type only
            return JsonConvert.SerializeObject(value);
        }
    }
}
