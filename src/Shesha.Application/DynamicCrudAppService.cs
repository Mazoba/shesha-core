using Abp.Application.Services.Dto;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using AutoMapper;
using Shesha.Domain.Enums;
using Shesha.DynamicEntities;
using Shesha.DynamicEntities.Dtos;
using Shesha.ObjectMapper;
using Shesha.Services.VersionedFields;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shesha
{
    [DynamicControllerNameConvention]
    public abstract class DynamicCrudAppService<TEntity, TDynamicDto, TPrimaryKey> : SheshaCrudServiceBase<TEntity,
        TDynamicDto, TPrimaryKey, PagedAndSortedResultRequestDto, TDynamicDto, TDynamicDto>, ITransientDependency
        where TEntity : class, IEntity<TPrimaryKey>
        where TDynamicDto : class, IDynamicDto<TEntity, TPrimaryKey>
    {
        public IDynamicDtoTypeBuilder DtoBuilder { get; set; }
        public IVersionedFieldManager VersionedFieldManager { get; set; }
        public ISerializationManager SerializationManager { get; set; }

        public DynamicCrudAppService(IRepository<TEntity, TPrimaryKey> repository) : base(repository)
        {
        }

        public override async Task<TDynamicDto> GetAsync(EntityDto<TPrimaryKey> input)
        {
            CheckGetAllPermission();

            var entity = await Repository.GetAsync(input.Id);

            return await MapToEntityDtoAsync(entity);
        }


        public override async Task<TDynamicDto> UpdateAsync(TDynamicDto input)
        {
            CheckUpdatePermission();

            var entity = await Repository.GetAsync(input.Id);

            await MapStaticPropertiesToEntityDtoAsync(input, entity);
            await MapDynamicPropertiesToEntityDtoAsync(input, entity);

            await Repository.UpdateAsync(entity);

            return await MapToEntityDtoAsync(entity);
        }

        public override async Task<TDynamicDto> CreateAsync(TDynamicDto input)
        {
            CheckCreatePermission();

            var entity = Activator.CreateInstance<TEntity>();

            await MapStaticPropertiesToEntityDtoAsync(input, entity);

            await Repository.InsertAsync(entity);

            await MapDynamicPropertiesToEntityDtoAsync(input, entity);

            return await MapToEntityDtoAsync(entity);
        }

        #region private declarations 

        private async Task<TDynamicDto> MapToEntityDtoAsync(TEntity entity)
        {
            // build dto type
            var context = new DynamicDtoTypeBuildingContext() { ModelType = typeof(TDynamicDto) };
            var dtoType = await DtoBuilder.BuildDtoFullProxyTypeAsync(typeof(TDynamicDto), context);
            var dto = Activator.CreateInstance(dtoType) as TDynamicDto;

            // create mapper
            var mapper = GetMapper(typeof(TEntity), dtoType);

            // map entity to DTO
            mapper.Map(entity, dto);

            // todo: map dynamic fields
            var dynamicProperties = (await DtoBuilder.GetEntityPropertiesAsync(typeof(TEntity)))
                .Where(p => p.Source == MetadataSourceType.UserDefined)
                .ToList();
            var dtoProps = dto.GetType().GetProperties();
            foreach (var property in dynamicProperties)
            {
                var dtoProp = dtoProps.FirstOrDefault(p => p.Name == property.Name);

                if (dtoProp != null)
                {
                    var serializedValue = await VersionedFieldManager.GetVersionedFieldValueAsync<TEntity, TPrimaryKey>(entity, property.Name);
                    var rawValue = serializedValue != null
                        ? SerializationManager.DeserializeProperty(dtoProp.PropertyType, serializedValue)
                        : null;
                    dtoProp.SetValue(dto, rawValue);
                }
            }

            return dto;
        }

        private async Task MapStaticPropertiesToEntityDtoAsync(TDynamicDto dto, TEntity entity) 
        {
            var mapper = GetMapper(dto.GetType(), entity.GetType());
            mapper.Map(dto, entity);
        }

        private async Task MapDynamicPropertiesToEntityDtoAsync(TDynamicDto dto, TEntity entity) 
        {
            var dynamicProperties = (await DtoBuilder.GetEntityPropertiesAsync(typeof(TEntity)))
                .Where(p => p.Source == MetadataSourceType.UserDefined)
                .ToList();

            var dtoProps = dto.GetType().GetProperties();
            foreach (var property in dynamicProperties)
            {
                var dtoProp = dtoProps.FirstOrDefault(p => p.Name == property.Name);

                if (dtoProp != null)
                {
                    var rawValue = dtoProp.GetValue(dto);
                    var convertedValue = SerializationManager.SerializeProperty(property, rawValue);
                    await VersionedFieldManager.SetVersionedFieldValueAsync<TEntity, TPrimaryKey>(entity, property.Name, convertedValue, false);
                }
            }
        }

        private IMapper GetMapper(Type sourceType, Type destinationType)
        {
            var modelConfigMapperConfig = new MapperConfiguration(cfg =>
            {
                var mapExpression = cfg.CreateMap(sourceType, destinationType);

                // todo: move to conventions
                cfg.CreateMap<RefListPersonTitle, Int64>().ConvertUsing<EnumToInt64TypeConverter<RefListPersonTitle>>();
                cfg.CreateMap<Int64, RefListPersonTitle>().ConvertUsing<Int64ToEnumTypeConverter<RefListPersonTitle>>();
            });

            return modelConfigMapperConfig.CreateMapper();
        }


        #endregion
    }
}
