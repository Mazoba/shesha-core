using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using AutoMapper;
using Shesha.AutoMapper.Dto;
using Shesha.Extensions;
using Shesha.Services;
using System;

namespace Shesha.DynamicEntities.Mapper
{
    /// <summary>
    /// Entity to EntityWithDisplayNameDto converter
    /// </summary>
    public class EntityToEntityWithDisplayNameDtoConverter<TEntity, TId> : ITypeConverter<TEntity, EntityWithDisplayNameDto<TId>> where TEntity : class, IEntity<TId>
    {
        public EntityWithDisplayNameDto<TId> Convert(TEntity source, EntityWithDisplayNameDto<TId> destination, ResolutionContext context)
        {
            return source == null
                ? null
                : new EntityWithDisplayNameDto<TId> 
                    { 
                        Id = source.Id, 
                        DisplayText = source.GetDisplayName() 
                    };
        }
    }
}
