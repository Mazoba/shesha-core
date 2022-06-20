using Abp.Application.Services.Dto;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Linq;
using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Shesha.Extensions;
using Shesha.GraphQL.Dtos;
using Shesha.GraphQL.Provider.GraphTypes;
using Shesha.JsonLogic;
using Shesha.Utilities;
using System;
using System.ComponentModel;
using System.Linq;

namespace Shesha.GraphQL.Provider.Queries
{
    /// <summary>
    /// Entity query
    /// </summary>
    public class EntityQuery<TEntity, TId> : ObjectGraphType, ITransientDependency where TEntity : class, IEntity<TId>
    {
        private IJsonLogic2LinqConverter _jsonLogicConverter;

        protected EntityQuery(IJsonLogic2LinqConverter jsonLogicConverter)
        {
            _jsonLogicConverter = jsonLogicConverter;
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

                    // filter entities
                    query = AddFilter(query, input.Filter);

                    // add quick search
                    query = AddQuickSearch(query, input.QuickSearch);

                    // calculate total count
                    var totalCount = query.Count();

                    // apply sorting
                    query = ApplySorting(query, input.Sorting);

                    // apply paging
                    var pageQuery = query.Skip(input.SkipCount).Take(input.MaxResultCount);

                    var entities = await asyncExecuter.ToListAsync(pageQuery);

                    var result = new PagedResultDto<TEntity> {
                        Items = entities,
                        TotalCount = totalCount
                    };

                    return result;
                }
            );
        }

        /// <summary>
        /// Add filter to <paramref name="query"/>
        /// </summary>
        /// <param name="query">Queryable to be filtered</param>
        /// <param name="filter">String representation of JsonLogic filter</param>
        /// <returns></returns>
        private IQueryable<TEntity> AddFilter(IQueryable<TEntity> query, string filter) 
        {
            if (string.IsNullOrWhiteSpace(filter))
                return query;

            var jsonLogic = JObject.Parse(filter);

            var expression = _jsonLogicConverter.ParseExpressionOf<TEntity>(jsonLogic);

            return query.Where(expression);
        }

        
        private IQueryable<TEntity> AddQuickSearch(IQueryable<TEntity> query, string quickSearch)
        {
            if (string.IsNullOrWhiteSpace(quickSearch))
                return query;

            // todo: implement filter

            return query;
        }

        /// <summary>
        /// Apply sorting to <paramref name="query"/>
        /// </summary>
        /// <param name="query">Queryable to be sorted</param>
        /// <param name="sorting">Sorting string (in the standard SQL format e.g. "Property1 asc, Property2 desc")</param>
        /// <returns></returns>
        private IQueryable<TEntity> ApplySorting(IQueryable<TEntity> query, string sorting)
        {
            if (string.IsNullOrWhiteSpace(sorting))
                return query;

            var sortColumns = sorting.Split(',').Select(c => c.Trim()).Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
            var sorted = false;
            foreach (var sortColumn in sortColumns)
            {
                var column = sortColumn.LeftPart(' ', ProcessDirection.LeftToRight);
                if (string.IsNullOrWhiteSpace(column))
                    continue;

                var direction = sortColumn.RightPart(' ', ProcessDirection.LeftToRight)?.Trim().Equals("desc", StringComparison.InvariantCultureIgnoreCase) == true
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;

                if (sorted)
                {
                    if (!(query is IOrderedQueryable<TEntity> orderedQuery))
                        throw new Exception($"Query must implement {nameof(IOrderedQueryable)} to allow sort by multiple columns");

                    // already sorted (it's not a first sorting column)
                    switch (direction)
                    {
                        case ListSortDirection.Ascending:
                            query = orderedQuery.ThenBy(column);
                            break;
                        case ListSortDirection.Descending:
                            query = orderedQuery.ThenByDescending(column);
                            break;
                    }
                }
                else
                {
                    switch (direction)
                    {
                        case ListSortDirection.Ascending:
                            query = query.OrderBy(column);
                            break;
                        case ListSortDirection.Descending:
                            query = query.OrderByDescending(column);
                            break;
                    }
                }
                sorted = true;
            }
            
            return query;
        }
        
        private static Type MakeGetInputType()
        {
            return GraphTypeMapper.GetGraphType(typeof(TId), true, true);
        }
    }
}
