using Abp.Application.Services.Dto;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Linq;
using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Shesha.GraphQL.Dtos;
using Shesha.GraphQL.Provider.GraphTypes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shesha.GraphQL.Provider.Queries
{
    /// <summary>
    /// Entity query
    /// </summary>
    public class EntityQuery<TEntity, TId> : ObjectGraphType, ITransientDependency where TEntity : class, IEntity<TId>
    {
        protected EntityQuery()
        {

        }

        public EntityQuery(IServiceProvider serviceProvider)
        {
            var entityName = typeof(TEntity).Name;

            Name = entityName + "Query";

            var repository = serviceProvider.GetRequiredService<IRepository<TEntity, TId>>();
            var asyncExecuter = serviceProvider.GetRequiredService<IAsyncQueryableExecuter>();

            FieldAsync<GraphQLGenericType<TEntity>>(entityName,
                arguments: new QueryArguments(new QueryArgument(MakeGetInputType()) { Name = nameof(IEntity.Id) }),
                resolve: async context => {
                    var id = context.GetArgument<TId>(nameof(IEntity.Id));
                    return await repository.GetAsync(id);
                }                    
            );

            FieldAsync<PagedResultDtoType<TEntity>>($"{entityName}List",
                arguments: new QueryArguments(
                    new QueryArgument<GraphQLInputGenericType<FilteredPagedAndSortedResultRequestDto>>
                    { Name = "input", DefaultValue = new FilteredPagedAndSortedResultRequestDto() } ),
                resolve: async context => {
                    var input = context.GetArgument<FilteredPagedAndSortedResultRequestDto>("input");

                    var query = repository.GetAll();

                    var totalCount = query.Count();

                    var pageQuery = query.Skip(input.SkipCount).Take(input.MaxResultCount);

                    // todo: add sorting
                    // todo: add filters support

                    var entities = await asyncExecuter.ToListAsync(pageQuery);

                    var result = new PagedResultDto<TEntity> {
                        Items = entities,
                        TotalCount = totalCount
                    };

                    return result;
                }
            );

            /*
            FieldAsync<PagedResultDtoType<TGetListOutputDto>>($"{entityName}List",
                arguments: new QueryArguments(
                    new QueryArgument<GraphQLInputGenericType<TGetListInput>>
                    { Name = "input", DefaultValue = Activator.CreateInstance<TGetListInput>() }),
                resolve: async context =>
                    await readOnlyAppService.GetListAsync(context.GetArgument<TGetListInput>("input"))
            );
            */
        }

        private static Type MakeGetInputType()
        {
            return GraphTypeMapper.GetGraphType(typeof(TId), true, true);
        }
    }
}
