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
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class PersonTest2Controller: ControllerBase, ITransientDependency
    {
        private readonly IRepository<Person, Guid> _repository;

        public PersonTest2Controller(IRepository<Person, Guid> repository)
        {
            _repository = repository;
        }

        [HttpPost]
        public async Task<PersonDto> UpdateAtRuntimeAsync(EntityDto<Guid> input)
        {
            var entity = await _repository.GetAsync(input.Id);

            // bind manually
            if (await TryUpdateModelAsync(
                entity,
                "Instructor",
                x => x.FirstName, 
                x => x.LastName!))
            {
            }

            return MapToEntityDto(entity);
        }

        [HttpPost]
        public async Task<PersonDto> UpdateDtoAtRuntimeAsync(
            //DynamicDto<Person, Guid> input
            PersonDynamicDto input
            /*EntityDto<Guid> input*/)
        {
            var dto = new PersonDynamicDto();

            // bind manually
            if (await TryUpdateModelAsync(
                dto,
                string.Empty,//"Instructor",
                x => x.FirstName))
            {
                
            }

            return null;
        }

        private PersonDto MapToEntityDto(Person person)
        {
            var dto = new PersonDto();

            // map hardcoded fields
            // add dynamic fields

            return dto;
        }
    }
}
