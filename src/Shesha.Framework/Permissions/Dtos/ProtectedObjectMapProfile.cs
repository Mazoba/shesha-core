using System;
using System.Collections.Generic;
using Shesha.AutoMapper;
using Shesha.Domain;
using Shesha.Metadata.Dtos;
using System.Linq;
using Shesha.DynamicEntities.Dtos;

namespace Shesha.Permissions.Dtos
{
    public class ProtectedObjectMapProfile : ShaProfile
    {
        public ProtectedObjectMapProfile()
        {
            "".Split(",").ToList();
            CreateMap<ProtectedObjectDto, ProtectedObject>()
                .ForMember(e => e.Permissions, c => c.MapFrom(e => string.Join(",", e.Permissions)));
            CreateMap<ProtectedObject, ProtectedObjectDto>()
                .ForMember(e => e.Permissions, c => c.MapFrom(e => 
                    e.Permissions == null 
                        ? new List<string>() 
                        : e.Permissions.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList()));
        }
    }
}
