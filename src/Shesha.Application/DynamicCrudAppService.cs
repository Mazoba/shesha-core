using Abp.Application.Services.Dto;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Shesha.DynamicEntities;
using Shesha.DynamicEntities.Dtos;
using System;
using System.Threading.Tasks;

namespace Shesha
{
    [DynamicControllerNameConvention]
    public class DynamicCrudAppService<TEntity, TDynamicDto, TPrimaryKey> : SheshaCrudServiceBase<TEntity,
        TDynamicDto, TPrimaryKey, PagedAndSortedResultRequestDto, TDynamicDto, TDynamicDto>, ITransientDependency
        where TEntity : class, IEntity<TPrimaryKey>
        where TDynamicDto : class, IDynamicDto<TEntity, TPrimaryKey>
    {
        public DynamicCrudAppService(IRepository<TEntity, TPrimaryKey> repository) : base(repository)
        {
        }

        public override async Task<TDynamicDto> GetAsync(EntityDto<TPrimaryKey> input)
        {
            CheckGetAllPermission();

            var entity = await Repository.GetAsync(input.Id);

            return await MapToCustomDynamicDtoAsync<TDynamicDto, TEntity, TPrimaryKey>(entity);
        }


        public override async Task<TDynamicDto> UpdateAsync(TDynamicDto input)
        {
            CheckUpdatePermission();

            var entity = await Repository.GetAsync(input.Id);

            await MapDynamicDtoToEntityAsync<TDynamicDto, TEntity, TPrimaryKey>(input, entity);

            await Repository.UpdateAsync(entity);

            return await MapToCustomDynamicDtoAsync<TDynamicDto, TEntity, TPrimaryKey>(entity);
        }

        public override async Task<TDynamicDto> CreateAsync(TDynamicDto input)
        {
            CheckCreatePermission();

            var entity = Activator.CreateInstance<TEntity>();

            await MapStaticPropertiesToEntityDtoAsync<TDynamicDto, TEntity, TPrimaryKey>(input, entity);

            await Repository.InsertAsync(entity);

            await MapDynamicPropertiesToEntityAsync<TDynamicDto, TEntity, TPrimaryKey>(input, entity);

            return await MapToCustomDynamicDtoAsync<TDynamicDto, TEntity, TPrimaryKey>(entity);
        }
    }
}
