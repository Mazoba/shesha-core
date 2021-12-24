using Shesha.AutoMapper;
using Shesha.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shesha.DynamicEntities.Dtos
{
    public class DynamicEntitiesMapProfile: ShaProfile
    {
        public DynamicEntitiesMapProfile()
        {
            CreateMap<EntityConfigDto, EntityConfig>();
            CreateMap<EntityConfig, EntityConfigDto>();

            CreateMap<EntityPropertyDto, EntityProperty>();
            CreateMap<EntityProperty, EntityPropertyDto>();

            CreateMap<EntityProperty, ModelPropertyDto>()
                .ForMember(e => e.Properties, c => c.MapFrom(e => e.Properties.OrderBy(p => p.SortOrder).ToList()));

            CreateMap<EntityConfig, ModelConfigurationDto>()
                .ForMember(e => e.Properties, c => c.Ignore());
        }
    }
}
