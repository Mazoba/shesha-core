using Shesha.DynamicEntities.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.DynamicEntities.Cache
{
    /// <summary>
    /// Entity config cache
    /// </summary>
    public interface IEntityConfigCache
    {
        /// <summary>
        /// Get entity properties
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        Task<List<EntityPropertyDto>> GetEntityPropertiesAsync(Type entityType);

        Task<List<EntityPropertyDto>> Test1(string testvals);
    }
}
