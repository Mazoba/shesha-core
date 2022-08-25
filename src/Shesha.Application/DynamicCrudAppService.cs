using Abp.Application.Services.Dto;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Runtime.Validation;
using Shesha.Application.Services.Dto;
using Shesha.DynamicEntities;
using Shesha.DynamicEntities.Dtos;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Shesha
{
    [DynamicControllerNameConvention]
    public class DynamicCrudAppService<TEntity, TDynamicDto, TPrimaryKey> : SheshaCrudServiceBase<TEntity,
        TDynamicDto, TPrimaryKey, FilteredPagedAndSortedResultRequestDto, TDynamicDto, TDynamicDto, GetDynamicEntityInput<TPrimaryKey>>, IDynamicCrudAppService<TEntity, TDynamicDto, TPrimaryKey>, ITransientDependency
        where TEntity : class, IEntity<TPrimaryKey>
        where TDynamicDto : class, IDynamicDto<TEntity, TPrimaryKey>
    {
        public DynamicCrudAppService(IRepository<TEntity, TPrimaryKey> repository) : base(repository)
        {
        }

        public override async Task<TDynamicDto> GetAsync(GetDynamicEntityInput<TPrimaryKey> input)
        {
            CheckGetPermission();

            var entity = await Repository.GetAsync(input.Id);

            return await MapToCustomDynamicDtoAsync<TDynamicDto, TEntity, TPrimaryKey>(entity);
        }

        public override async Task<TDynamicDto> UpdateAsync(TDynamicDto input)
        {
            CheckUpdatePermission();

            var entity = await Repository.GetAsync(input.Id);

            var jObject = (input as IHasJObjectField).JObject;

            if (jObject != null)
            {
                var validationResults = new List<ValidationResult>();
                var result = MapJObjectToStaticPropertiesEntityAsync<TEntity, TPrimaryKey>(jObject, entity, validationResults);
                if (!result)
                    throw new AbpValidationException("Please correct the errors and try again", validationResults);

                if (!Validator.TryValidateObject(entity, new ValidationContext(entity), validationResults))
                    throw new AbpValidationException("Please correct the errors and try again", validationResults);

                result = await MapJObjectToDynamicPropertiesEntityAsync<TEntity, TPrimaryKey>(jObject, entity, validationResults);
                if (!result)
                    throw new AbpValidationException("Please correct the errors and try again", validationResults);
            }
            else
            {
                await MapDynamicDtoToEntityAsync<TDynamicDto, TEntity, TPrimaryKey>(input, entity);

                var validationResults = new List<ValidationResult>();
                if (!Validator.TryValidateObject(entity, new ValidationContext(entity), validationResults))
                    throw new AbpValidationException("Please correct the errors and try again", validationResults);
            }

            await Repository.UpdateAsync(entity);

            return await MapToCustomDynamicDtoAsync<TDynamicDto, TEntity, TPrimaryKey>(entity);
        }

        public override async Task<TDynamicDto> CreateAsync(TDynamicDto input)
        {
            CheckCreatePermission();

            var entity = Activator.CreateInstance<TEntity>();

            var jObject = (input as IHasJObjectField).JObject;

            if (jObject != null)
            {
                var validationResults = new List<ValidationResult>();
                var result = MapJObjectToStaticPropertiesEntityAsync<TEntity, TPrimaryKey>(jObject, entity, validationResults);

                if (!result)
                    throw new AbpValidationException("Please correct the errors and try again", validationResults);

                if (!Validator.TryValidateObject(entity, new ValidationContext(entity), validationResults))
                    throw new AbpValidationException("Please correct the errors and try again", validationResults);
            }
            else
            {
                await MapStaticPropertiesToEntityDtoAsync<TDynamicDto, TEntity, TPrimaryKey>(input, entity);

                var validationResults = new List<ValidationResult>();
                if (!Validator.TryValidateObject(entity, new ValidationContext(entity), validationResults))
                    throw new AbpValidationException("Please correct the errors and try again", validationResults);
            }

            await Repository.InsertAsync(entity);

            await UnitOfWorkManager.Current.SaveChangesAsync();

            if (jObject != null)
            {
                var validationResults = new List<ValidationResult>();
                var result = await MapJObjectToDynamicPropertiesEntityAsync<TEntity, TPrimaryKey>(jObject, entity, validationResults);
                if (!result)
                    throw new AbpValidationException("Please correct the errors and try again", validationResults);
            }
            else
            {
                await MapDynamicPropertiesToEntityAsync<TDynamicDto, TEntity, TPrimaryKey>(input, entity);
            }

            await UnitOfWorkManager.Current.SaveChangesAsync();

            return await MapToCustomDynamicDtoAsync<TDynamicDto, TEntity, TPrimaryKey>(entity);
        }

        public override async Task<PagedResultDto<TDynamicDto>> GetAllAsync(FilteredPagedAndSortedResultRequestDto input)
        {
            CheckGetAllPermission();

            var query = CreateFilteredQuery(input);

            var totalCount = await AsyncQueryableExecuter.CountAsync(query);

            query = ApplySorting(query, input);
            query = ApplyPaging(query, input);

            var entities = await AsyncQueryableExecuter.ToListAsync(query);

            var list = new List<TDynamicDto>();
            foreach (var entity in entities)
            {
                list.Add(await MapToCustomDynamicDtoAsync<TDynamicDto, TEntity, TPrimaryKey>(entity));
            }

            return new PagedResultDto<TDynamicDto>(
                totalCount,
                list
            );
        }
    }
}
