using Abp.Dependency;
using Abp.Reflection;
using Shesha.AutoMapper;
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

                Converters.Add(new ConverterInfo { 
                    EntityType = entityType, 
                    IdType = idType, 
                    IdToEntityConverter = typeof(IdToEntityConverter<,>).MakeGenericType(entityType, idType),
                    EntityToIdConverter = typeof(EntityToIdConverter<,>).MakeGenericType(entityType, idType),
                });
            }
        }

        public EntityMapProfile()
        {
            foreach (var converterInfo in Converters) 
            {
                CreateMap(converterInfo.IdType, converterInfo.EntityType).ConvertUsing(converterInfo.IdToEntityConverter);
                CreateMap(converterInfo.EntityType, converterInfo.IdType).ConvertUsing(converterInfo.EntityToIdConverter);
            }
        }

        public class ConverterInfo 
        { 
            public Type EntityType { get; set; }
            public Type IdType { get; set; }
            public Type IdToEntityConverter { get; set; }
            public Type EntityToIdConverter { get; set; }
        }
    }
}
