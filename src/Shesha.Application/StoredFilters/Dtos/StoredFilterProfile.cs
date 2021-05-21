using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Shesha.AutoMapper;
using Shesha.AutoMapper.Dto;
using Shesha.Domain;
using Shesha.StoredFilters.Dtos;

namespace Shesha.StoredFilters.Dto
{
    /// <summary>
    /// Mappings profile
    /// </summary>
    [UsedImplicitly]
    public class StoredFilterProfile : ShaProfile
    {
        /// <summary>
        /// 
        /// </summary>
        public StoredFilterProfile()
        {
            CreateMap<StoredFilterContainer, EntityLinkDto>()
                .ForMember(dto => dto.EntityType, o => o.MapFrom(e => e.OwnerType))
                .ForMember(dto => dto.EntityId, o => o.MapFrom(e => e.OwnerId))
                
                .ReverseMap();

            CreateMap<EntityVisibility, EntityLinkDto>()
                .ForMember(dto => dto.EntityType, o => o.MapFrom(e => e.OwnerType))
                .ForMember(dto => dto.EntityId, o => o.MapFrom(e => e.OwnerId))
                
                .ReverseMap();

            CreateMap<StoredFilter, StoredFilterDto>();

            // Reverse mappings for the list dto --> entity (insert / update / inactivate list items)
            CreateMap<List<EntityLinkDto>, List<StoredFilterContainer>>().ConvertUsing<EntityLinkCollectionConverter<EntityLinkDto, StoredFilterContainer, Guid>>();
            CreateMap<List<EntityLinkDto>, List<EntityVisibility>>().ConvertUsing<EntityLinkCollectionConverter<EntityLinkDto, EntityVisibility, Guid>>();

            CreateMap<CreateStoredFilterDto, StoredFilter>();
            CreateMap<StoredFilterDto, StoredFilter>();
            CreateMap<UpdateStoredFilterDto, StoredFilter>();
        }
    }
}
