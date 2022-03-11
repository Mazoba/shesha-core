using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Shesha.Domain;
using Shesha.DynamicEntities;
using Shesha.DynamicEntities.Dtos;
using System;
using System.Threading.Tasks;

namespace Shesha.Application.Persons
{
    [AbpAuthorize]
    public class PersonTestAppService : DynamicCrudAppService<Person, DynamicDto<Person, Guid>, Guid>, ITransientDependency
    {
        public PersonTestAppService(IRepository<Person, Guid> repository) : base(repository)
        {
        }

        public override async Task<DynamicDto<Person, Guid>> UpdateAsync([DynamicBinder(UseDtoForEntityReferences = true)] DynamicDto<Person, Guid> input) 
        { 
            return await base.UpdateAsync(input);
        }

        public override async Task<DynamicDto<Person, Guid>> GetAsync(EntityDto<Guid> input)
        {
            CheckGetAllPermission();

            var entity = await Repository.GetAsync(input.Id);

            return await MapToCustomDynamicDtoAsync<DynamicDto<Person, Guid>, Person, Guid>(entity, new DynamicMappingSettings { UseDtoForEntityReferences = true });
        }

    }
}
