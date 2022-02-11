using Shesha.AutoMapper;
using Shesha.Domain;
using Shesha.Metadata.Dtos;
using System.Linq;

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

            CreateMap<ModelPropertyDto, PropertyMetadataDto>()
                .ForMember(e => e.Path, c => c.MapFrom(e => e.Name))
                .ForMember(e => e.IsVisible, c => c.MapFrom(e => true));
            //CreateMap<PropertyMetadataDto, ModelPropertyDto>();            
        }
    }
}
