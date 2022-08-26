﻿using Abp.Application.Services.Dto;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Runtime.Validation;
using GraphQL;
using Microsoft.AspNetCore.Mvc;
using Shesha.Application.Services;
using Shesha.Application.Services.Dto;
using Shesha.DynamicEntities.Cache;
using Shesha.DynamicEntities.Dtos;
using Shesha.Exceptions;
using Shesha.Extensions;
using Shesha.GraphQL.Middleware;
using Shesha.GraphQL.Mvc;
using Shesha.GraphQL.Provider;
using Shesha.Metadata;
using Shesha.QuickSearch;
using Shesha.Utilities;
using Shesha.Web;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    public abstract class SheshaCrudServiceBase<TEntity, TEntityDto, TPrimaryKey, TGetAllInput, TCreateInput, TUpdateInput, TGetInput> : AbpAsyncCrudAppService<TEntity, TEntityDto, TPrimaryKey, TGetAllInput, TCreateInput, TUpdateInput, TGetInput>, IEntityAppService<TEntity, TPrimaryKey>
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

        //[HttpGet]
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

        #region GraphQL

        /// <summary>
        /// GraphQL document executer
        /// </summary>
        public IDocumentExecuter DocumentExecuter { get; set; }
        public ISchemaContainer SchemaContainer { get; set; }
        public IGraphQLSerializer Serializer { get; set; }
        public IEntityConfigCache EntityConfigCache { get; set; }

        /// <summary>
        /// Query entity data. 
        /// NOTE: don't use on prod, will be merged with the `Get`endpoint soon
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <response code="200">NOTE: shape of the `result` depends on the `properties` argument. When `properties` argument is not specified - it returns top level properties of the entity, all referenced entities are presented as their Id values</response>
        [HttpGet]
        public virtual async Task<GraphQLDataResult<TEntity>> QueryAsync(GetDynamicEntityInput<TPrimaryKey> input)
        {
            CheckGetAllPermission();

            var schemaName = StringHelper.ToCamelCase(typeof(TEntity).Name);

            var schema = await SchemaContainer.GetOrDefaultAsync(schemaName);
            var httpContext = AppContextHelper.Current;

            var properties = string.IsNullOrWhiteSpace(input.Properties)
                    ? await GetGqlTopLevelPropertiesAsync()
                    : CleanupProperties(input.Properties);

            var query = $@"query{{
  {schemaName}(id: ""{input.Id}"") {{
    {properties}
  }}
}}";

            var result = await DocumentExecuter.ExecuteAsync(async s =>
            {
                s.Schema = schema;

                s.Query = query;

                if (httpContext != null)
                {
                    s.RequestServices = httpContext.RequestServices;
                    s.UserContext = new GraphQLUserContext
                    {
                        User = httpContext.User,
                    };
                    s.CancellationToken = httpContext.RequestAborted;
                }
            });

            if (result.Errors != null)
            {
                var validationResults = result.Errors.Select(e => new ValidationResult(e.FullMessage())).ToList();
                throw new AbpValidationException(string.Join("\r\n", validationResults.Select(r => r.ErrorMessage)), validationResults);
            }

            return new GraphQLDataResult<TEntity>(result);
        }

        /// <summary>
        /// Query entities list
        /// NOTE: don't use on prod, will be merged with the GetAll endpoint soon
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <response code="200">NOTE: shape of the `items[]` depends on the `properties` argument. When `properties` argument is not specified - it returns top level properties of the entity, all referenced entities are presented as their Id values</response>
        [HttpGet]
        public virtual async Task<GraphQLDataResult<PagedResultDto<TEntity>>> QueryAllAsync(PropsFilteredPagedAndSortedResultRequestDto input)
        {
            CheckGetAllPermission();

            var schemaName = StringHelper.ToCamelCase(typeof(TEntity).Name);

            var schema = await SchemaContainer.GetOrDefaultAsync(schemaName);
            var httpContext = AppContextHelper.Current;

            var properties = string.IsNullOrWhiteSpace(input.Properties)
                ? await GetGqlTopLevelPropertiesAsync()
                : CleanupProperties(input.Properties);
            var query = $@"query getAll($filter: String, $quickSearch: String, $quickSearchProperties: [String], $sorting: String, $skipCount: Int, $maxResultCount: Int){{
  {schemaName}List(input: {{ filter: $filter, quickSearch: $quickSearch, quickSearchProperties: $quickSearchProperties, sorting: $sorting, skipCount: $skipCount, maxResultCount: $maxResultCount }}){{
    totalCount
    items {{
        {properties}
    }}
  }}
}}";

            var result = await DocumentExecuter.ExecuteAsync(async s =>
            {
                s.Schema = schema;

                s.Query = query;
                s.Variables = new Inputs(new Dictionary<string, object> {
                    { "filter", input.Filter },
                    { "quickSearch", input.QuickSearch },
                    { "quickSearchProperties", ExtractProperties(properties) },
                    { "sorting", input.Sorting },
                    { "skipCount", input.SkipCount },
                    { "maxResultCount", input.MaxResultCount },
                });

                if (httpContext != null)
                {
                    s.RequestServices = httpContext.RequestServices;
                    s.UserContext = new GraphQLUserContext
                    {
                        User = httpContext.User,
                    };
                    s.CancellationToken = httpContext.RequestAborted;
                }
            });

            if (result.Errors != null) {
                var validationResults = result.Errors.Select(e => new ValidationResult(e.FullMessage())).ToList();
                throw new AbpValidationException(string.Join("\r\n", validationResults.Select(r => r.ErrorMessage)), validationResults);
            }

            return new GraphQLDataResult<PagedResultDto<TEntity>>(result);
        }

        private List<string> ExtractProperties(string properties) 
        {
            var regex = new Regex(@"\s");
            return regex.Split(properties).ToList();
        }

        private string CleanupProperties(string properties) 
        {
            if (string.IsNullOrWhiteSpace(properties))
                return properties;

            var regex = new Regex(@"\s");
            return string.Join(' ', regex.Split(properties).Select(p => StringHelper.ToCamelCase(p)));
        }

        private void AppendProperty(StringBuilder sb, EntityPropertyDto property)
        {
            // todo: use FieldNameConverter to get correct case of the field names
            var propertyName = StringHelper.ToCamelCase(property.Name);

            switch (property.DataType)
            {
                case DataTypes.Array:
                    // todo: implement and uncomment
                    return;

                case DataTypes.EntityReference:
                    sb.AppendLine($"{propertyName}: {propertyName}{nameof(IEntity.Id)}");
                    break;

                case DataTypes.Object:
                    {
                        sb.Append(propertyName);
                        sb.AppendLine("{");
                        foreach (var subProp in property.Properties)
                        {
                            AppendProperty(sb, subProp);
                        }
                        sb.AppendLine("}");
                        break;
                    }
                default:
                    sb.AppendLine(propertyName);
                    break;
            }
        }

        private async Task<string> GetGqlTopLevelPropertiesAsync()
        {
            var sb = new StringBuilder();
            var properties = await EntityConfigCache.GetEntityPropertiesAsync(typeof(TEntity));
            foreach (var property in properties)
            {
                AppendProperty(sb, property);
            }

            return sb.ToString();
        }

        async Task<IDynamicDataResult> IEntityAppService.QueryAllAsync(PropsFilteredPagedAndSortedResultRequestDto input)
        {
            return await this.QueryAllAsync(input);
        }

        async Task<IDynamicDataResult> IEntityAppService<TEntity, TPrimaryKey>.QueryAsync(GetDynamicEntityInput<TPrimaryKey> input)
        {
            return await this.QueryAsync(input);
        }

        #endregion

    }
}
