using Abp.Application.Services.Dto;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using Shesha.Application.Persons.Dtos;
using Shesha.Domain;
using Shesha.DynamicEntities.Dtos;
using System;
using System.Threading.Tasks;

namespace Shesha.Application.Persons
{
    public class PersonTestAppService: SheshaAppServiceBase, ITransientDependency
    {
        private readonly IRepository<Person, Guid> _repository;

        public PersonTestAppService(IRepository<Person, Guid> repository)
        {
            _repository = repository;
        }

        public async Task<PersonDto> GetAsync(EntityDto<Guid> input) 
        {
            var entity = await _repository.GetAsync(input.Id);
            
            return MapToEntityDto(entity);
        }

        [HttpPost]
        public async Task<DynamicDto<Person, Guid>> UpdateOpenDynamicDtoAsync(DynamicDto<Person, Guid> dto)
        {
            return dto;
        }

        [HttpPost]
        public async Task<DynamicDto<Person, Guid>> UpdateClosedDynamicDtoAsync(PersonDynamicDto dto)
        {
            return dto;
        }
    }
}
