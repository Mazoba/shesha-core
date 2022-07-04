using Abp.Application.Services.Dto;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Shesha.Application.Services.Dto;
using Shesha.Domain;
using Shesha.Extensions;
using Shesha.QuickSearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shesha
{
    public abstract class SheshaCrudServiceBase<TEntity, TEntityDto, TPrimaryKey> : SheshaCrudServiceBase<TEntity,
        TEntityDto, TPrimaryKey, FilteredPagedAndSortedResultRequestDto, TEntityDto, TEntityDto>
        where TEntity : class, IEntity<TPrimaryKey>
        where TEntityDto : IEntityDto<TPrimaryKey>
    {
        protected SheshaCrudServiceBase(IRepository<TEntity, TPrimaryKey> repository) : base(repository)
        {
        }

        /*
        public virtual async Task<IActionResult> Query([FromBody] GraphQLRequest request) 
        {
            var startTime = DateTime.UtcNow;

            var result = await _documentExecuter.ExecuteAsync(s =>
            {
                s.Schema = _schema;
                s.Query = request.Query;
                s.Variables = request.Variables;
                s.OperationName = request.OperationName;
                s.RequestServices = HttpContext.RequestServices;
                s.UserContext = new GraphQLUserContext
                {
                    User = HttpContext.User,
                };
                s.CancellationToken = HttpContext.RequestAborted;
            });

            if (_graphQLOptions.Value.EnableMetrics)
            {
                result.EnrichWithApolloTracing(startTime);
            }

            return new ExecutionResultActionResult(result);
        }
        */
    }


    /// <summary>
    /// CRUD service base
    /// </summary>
    public abstract class SheshaCrudServiceBase<TEntity, TEntityDto, TPrimaryKey, TGetAllInput, TCreateInput, TUpdateInput> : SheshaCrudServiceBase<TEntity, TEntityDto, TPrimaryKey, TGetAllInput, TCreateInput, TUpdateInput, EntityDto<TPrimaryKey>>
        where TEntity : class, IEntity<TPrimaryKey>
        where TEntityDto : IEntityDto<TPrimaryKey>
        where TUpdateInput : IEntityDto<TPrimaryKey>
        where TGetAllInput : FilteredPagedAndSortedResultRequestDto
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="repository"></param>
        protected SheshaCrudServiceBase(IRepository<TEntity, TPrimaryKey> repository)
            : base(repository)
        {
        }
    }

    /// <summary>
    /// CRUD service base
    /// </summary>
    public abstract class SheshaCrudServiceBase<TEntity, TEntityDto, TPrimaryKey, TGetAllInput, TCreateInput, TUpdateInput, TGetInput> : AbpAsyncCrudAppService<TEntity, TEntityDto, TPrimaryKey, TGetAllInput, TCreateInput, TUpdateInput, TGetInput>
        where TEntity : class, IEntity<TPrimaryKey>
        where TEntityDto : IEntityDto<TPrimaryKey>
        where TUpdateInput : IEntityDto<TPrimaryKey>
        where TGetAllInput: FilteredPagedAndSortedResultRequestDto
        where TGetInput : IEntityDto<TPrimaryKey>
    {
        public IQuickSearcher QuickSearcher { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="repository"></param>
        protected SheshaCrudServiceBase(IRepository<TEntity, TPrimaryKey> repository)
            : base(repository)
        {
        }

        protected override IQueryable<TEntity> CreateFilteredQuery(TGetAllInput input)
        {
            var query = Repository.GetAll().ApplyFilter<TEntity, TPrimaryKey>(input.Filter);

            if (this.QuickSearcher != null && !string.IsNullOrWhiteSpace(input.QuickSearch))
                query = this.QuickSearcher.ApplyQuickSearch(query, input.QuickSearch);

            return query;
        }

        public override async Task<PagedResultDto<TEntityDto>> GetAllAsync(TGetAllInput input)
        {
            CheckGetAllPermission();

            var query = CreateFilteredQuery(input);

            var totalCount = await AsyncQueryableExecuter.CountAsync(query);

            query = ApplySorting(query, input);
            query = ApplyPaging(query, input);

            var entities = await AsyncQueryableExecuter.ToListAsync(query);

            return new PagedResultDto<TEntityDto>(
                totalCount,
                entities.Select(MapToEntityDto).ToList()
            );
        }

    }
}
