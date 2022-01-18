using Abp.Application.Services.Dto;
using Abp.Dependency;
using Abp.Domain.Repositories;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Shesha.Application.Persons.Dtos;
using Shesha.Domain;
using Shesha.Domain.Enums;
using Shesha.DynamicEntities;
using Shesha.DynamicEntities.Dtos;
using Shesha.NHibernate;
using Shesha.NHibernate.Session;
using Shesha.Services.VersionedFields;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Shesha.Application.Persons
{
    public class PersonTestAppService: SheshaAppServiceBase, ITransientDependency
    {
        private readonly IRepository<Person, Guid> _repository;
        private readonly ISessionProvider _sessionProvider;
        private readonly IDynamicDtoTypeBuilder _dtoBuilder;
        private readonly IVersionedFieldManager _versionedFieldManager;

        public PersonTestAppService(IRepository<Person, Guid> repository, ISessionProvider sessionProvider, IDynamicDtoTypeBuilder dtoBuilder, IVersionedFieldManager versionedFieldManager)
        {
            _repository = repository;
            _sessionProvider = sessionProvider;
            _dtoBuilder = dtoBuilder;
            _versionedFieldManager = versionedFieldManager;
        }

        public async Task<DynamicDto<Person, Guid>> GetAsync(EntityDto<Guid> input) 
        {
            var entity = await _repository.GetAsync(input.Id);
            
            return await MapToEntityDtoAsync(entity);
        }

        private async Task<DynamicDto<Person, Guid>> MapToEntityDtoAsync(Person person) 
        {
            // build dto type
            var dtoType = await _dtoBuilder.BuildDtoFullProxyTypeAsync(typeof(DynamicDto<Person, Guid>));
            var dto = Activator.CreateInstance(dtoType) as DynamicDto<Person, Guid>;

            // create mapper
            var mapper = GetMapper(typeof(Person), dtoType);

            // map entity to DTO
            mapper.Map(person, dto);

            // todo: map dynamic fields
            var dynamicProperties = (await _dtoBuilder.GetEntityPropertiesAsync(typeof(Person)))
                .Where(p => p.Source == MetadataSourceType.UserDefined)
                .ToList();
            var dtoProps = dto.GetType().GetProperties();
            foreach (var property in dynamicProperties)
            {
                var dtoProp = dtoProps.FirstOrDefault(p => p.Name == property.Name);

                if (dtoProp != null)
                {
                    var serializedValue = await _versionedFieldManager.GetVersionedFieldValueAsync<Person, Guid>(person, property.Name);
                    var rawValue = serializedValue != null
                        ? DeserializeProperty(dtoProp.PropertyType, serializedValue)
                        : null;
                    dtoProp.SetValue(dto, rawValue);
                }
            }

            return dto;
        }

        private async Task MapToEntityDtoAsync(DynamicDto<Person, Guid> dto, Person person) 
        {
            // add cache 
            var mapper = GetMapper(dto.GetType(), person.GetType());
            mapper.Map(dto, person);

            // manual mapping???
            var dynamicProperties = (await _dtoBuilder.GetEntityPropertiesAsync(typeof(Person)))
                .Where(p => p.Source == MetadataSourceType.UserDefined)
                .ToList();

            var dtoProps = dto.GetType().GetProperties();
            foreach (var property in dynamicProperties)
            {
                var dtoProp = dtoProps.FirstOrDefault(p => p.Name == property.Name);

                if (dtoProp != null) 
                {
                    var rawValue = dtoProp.GetValue(dto);
                    var convertedValue = SerializeProperty(property, rawValue);
                    await _versionedFieldManager.SetVersionedFieldValueAsync<Person, Guid>(person, property.Name, convertedValue, false);
                }
            }
        }

        private string SerializeProperty(EntityPropertyDto propertyDto, object value)
        {
            // todo: extract interface from EntityPropertyDto that describes data type only
            return JsonConvert.SerializeObject(value);
        }

        private object DeserializeProperty(Type propertyType, string value)
        {
            return JsonConvert.DeserializeObject(value, propertyType);
        }

        private IMapper GetMapper(Type sourceType, Type destinationType)
        {
            var modelConfigMapperConfig = new MapperConfiguration(cfg =>
            {
                var mapExpression = cfg.CreateMap(sourceType, destinationType);

                cfg.CreateMap<RefListPersonTitle, Int64>().ConvertUsing<EnumToInt64TypeConverter<RefListPersonTitle>>();
                cfg.CreateMap<Int64, RefListPersonTitle>().ConvertUsing<Int64ToEnumTypeConverter<RefListPersonTitle>>();
            });

            return modelConfigMapperConfig.CreateMapper();
        }

        [HttpPost]
        public async Task UpdateOpenDynamicDtoAsync(DynamicDto<Person, Guid> dto)
        {
            var person = await _repository.GetAsync(dto.Id);

            await MapToEntityDtoAsync(dto, person);
            
            var dirtyProperties = _sessionProvider.Session.GetDirtyProperties(person);

            await _repository.UpdateAsync(person);
        }

        [HttpPost]
        public async Task UpdateClosedDynamicDtoAsync(PersonDynamicDto dto)
        {
            var person = await _repository.GetAsync(dto.Id);

            await MapToEntityDtoAsync(dto, person);

            await _repository.UpdateAsync(person);
        }
    }

    public class EnumToInt64TypeConverter<T> : ITypeConverter<T, long> where T : struct
    {
        public long Convert(T source, long destination, ResolutionContext context)
        {
            return System.Convert.ToInt64(source);
        }
    }

    public class Int64ToEnumTypeConverter<T> : ITypeConverter<long, T> where T : struct
    {
        public T Convert(long source, T destination, ResolutionContext context)
        {
            return (T)Enum.ToObject(typeof(T), source);
        }
    }    
}
