using System.Threading.Tasks;
using Abp.Domain.Entities;
using Shesha.DynamicEntities.Dtos;

namespace Shesha.DynamicEntities
{
    /// <summary>
    /// Dynamic Property Manager
    /// Provides features to get and set values to dynamic properties of entities and Dtos
    /// </summary>
    public interface IDynamicPropertyManager
    {
        /// <summary>
        /// Get dynamic property value
        /// </summary>
        /// <typeparam name="TId">Type of primary key</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="property">Property to get value</param>
        /// <returns></returns>
        Task<string> GetValueAsync<TId>(IEntity<TId> entity, EntityPropertyDto property);

        // todo: get IsVersioned flag from the EntityPropertyDto
        /// <summary>
        /// Get dynamic property value
        /// </summary>
        /// <typeparam name="TId">Type of primary key</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="property">Property to set value</param>
        /// <param name="value">Value</param>
        /// <param name="createNewVersion">True if need to create a new version of value</param>
        /// <returns></returns>
        Task SetValueAsync<TId>(IEntity<TId> entity, EntityPropertyDto property, string value, bool createNewVersion);

        /// <summary>
        /// Map values of dynamic properties from Dto to Entity
        /// </summary>
        /// <typeparam name="TId">Type of primary key</typeparam>
        /// <typeparam name="TDynamicDto">Type of Dto</typeparam>
        /// <typeparam name="TEntity">Type of Entity</typeparam>
        /// <param name="dynamicDto">Dto</param>
        /// <param name="entity">Entity</param>
        /// <returns></returns>
        Task MapDtoToEntityAsync<TId, TDynamicDto, TEntity>(TDynamicDto dynamicDto, TEntity entity)
            where TEntity : class, IEntity<TId>
            where TDynamicDto : class, IDynamicDto<TEntity, TId>;

        /// <summary>
        /// Map values of dynamic properties from Entity to Dto
        /// </summary>
        /// <typeparam name="TId">Type of primary key</typeparam>
        /// <typeparam name="TDynamicDto">Type of Dto</typeparam>
        /// <typeparam name="TEntity">Type of Entity</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="dynamicDto">Dto</param>
        /// <returns></returns>
        Task MapEntityToDtoAsync<TId, TDynamicDto, TEntity>(TEntity entity, TDynamicDto dynamicDto)
            where TEntity : class, IEntity<TId>
            where TDynamicDto : class, IDynamicDto<TEntity, TId>;

    }
}