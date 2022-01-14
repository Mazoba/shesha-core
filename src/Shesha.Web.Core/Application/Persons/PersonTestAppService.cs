using Abp.Application.Services.Dto;
using Abp.Dependency;
using Abp.Domain.Repositories;
using AutoMapper;
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

        private PersonDto MapToEntityDto(Person person) 
        {
            var dto = new PersonDto();

            var mapper = GetMapper<Person, PersonDto>();
            mapper.Map(person, dto);

            // map hardcoded fields
            // add dynamic fields

            return dto;
        }

        private IMapper GetMapper<TSource, TDestination>()
        {
            var modelConfigMapperConfig = new MapperConfiguration(cfg => {
                var mapExpression = cfg.CreateMap<TSource, TDestination>();
            });

            return modelConfigMapperConfig.CreateMapper();
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
