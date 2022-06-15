using Abp.Domain.Entities;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Shesha.GraphQL.Provider.GraphTypes;
using Shesha.GraphQL.Provider.Queries;
using System;
using GraphQL;
using Abp.Application.Services.Dto;

namespace Shesha.GraphQL.Provider.Schemas
{
    /// <summary>
    /// Generic entity schema
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TId"></typeparam>
    public class EntitySchema<TEntity, TId>: Schema where TEntity: class, IEntity<TId>
    {
        public EntitySchema(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Query = serviceProvider.GetRequiredService<EntityQuery<TEntity, TId>>();

            /*
            this.RegisterTypeMapping<TGetOutputDto, GraphQLGenericType<TGetOutputDto>>();
            this.RegisterTypeMapping<TGetListOutputDto, GraphQLGenericType<TGetListOutputDto>>();
            */
            this.RegisterTypeMapping<TEntity, GraphQLGenericType<TEntity>>();
            this.RegisterTypeMapping<PagedResultDtoType<TEntity>, GraphQLGenericType<PagedResultDtoType<TEntity>>>();
        }
    }
}
