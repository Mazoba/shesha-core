using Shesha.DynamicEntities.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.DynamicEntities.Cache
{
    /// <summary>
    /// EntityConfig cache item
    /// </summary>
    public class EntityConfigCacheItem
    {
        public List<EntityPropertyDto> Properties { get; set; }
    }
}
