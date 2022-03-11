using Abp.Dependency;
using Abp.Reflection;
using Shesha.AutoMapper;
using Shesha.AutoMapper.Dto;
using Shesha.Extensions;
using Shesha.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shesha.ObjectMapper
{
    /// <summary>
    /// Entity mapping profile
    /// </summary>
    public class EntityMapProfile: ShaProfile, ITransientDependency
    {
        private static List<ConverterInfo> Converters;

        static EntityMapProfile() 
        {
            Converters = new List<ConverterInfo>();

            var typeFinder = StaticContext.IocManager.Resolve<ITypeFinder>();

            var entityTypes = typeFinder.Find(t => t.IsEntityType()).ToList();
            foreach (var entityType in entityTypes)
            {
                var idType = entityType.GetEntityIdType();

                Converters.Add(new ConverterInfo(idType, entityType, typeof(IdToEntityConverter<,>).MakeGenericType(entityType, idType)));
                Converters.Add(new ConverterInfo(entityType, idType, typeof(EntityToIdConverter<,>).MakeGenericType(entityType, idType)));

                var dtoType = typeof(EntityWithDisplayNameDto<>).MakeGenericType(idType);
                Converters.Add(new ConverterInfo(dtoType, entityType, typeof(EntityWithDisplayNameDtoToEntityConverter<,>).MakeGenericType(entityType, idType)));
                Converters.Add(new ConverterInfo(entityType, dtoType, typeof(EntityToEntityWithDisplayNameDtoConverter<,>).MakeGenericType(entityType, idType)));
            }
        }

        public EntityMapProfile()
        {
            foreach (var converterInfo in Converters) 
            {
                CreateMap(converterInfo.SrcType, converterInfo.DstType).ConvertUsing(converterInfo.ConverterType);
            }
        }

        public class ConverterInfo 
        {
            public Type SrcType { get; set; }
            public Type DstType { get; set; }
            public Type ConverterType { get; set; }

            public ConverterInfo(Type srcType, Type dstType, Type converterType)
            {
                SrcType = srcType;
                DstType = dstType;
                ConverterType = converterType;
            }
        }
    }
}
