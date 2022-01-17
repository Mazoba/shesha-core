using Shesha.DynamicEntities.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.DynamicEntities
{
    /// <summary>
    /// Dynamic DTO builder
    /// </summary>
    public interface IDynamicDtoTypeBuilder
    {
        /// <summary>
        /// Create instance of the <see cref="IDynamicDto{TEntity, TId}"/>
        /// </summary>
        /// <param name="dtoType">Type of the DTO</param>
        /// <returns></returns>
        Task<object> CreateDtoInstanceAsync(Type dtoType);

        /// <summary>
        /// Build proxy type for the DTO
        /// </summary>
        /// <param name="baseType">DTO type</param>
        /// <returns></returns>
        Task<Type> BuildDtoProxyTypeAsync(Type baseType);
    }
}
