using System;
using System.Collections.Generic;
using Shesha.AutoMapper;
using Shesha.Domain;
using Shesha.Metadata.Dtos;
using System.Linq;
using Abp.Localization;
using Shesha.DynamicEntities.Dtos;
using Shesha.Roles.Dto;

namespace Shesha.Permissions.Dtos
{
    public class PermissionMapProfile : ShaProfile
    {
        public PermissionMapProfile()
        {
            /*CreateMap<Abp.Authorization.Permission, PermissionDto>()
                .ForMember(e => e.DisplayName, c => c.MapFrom(e => 
                    e.DisplayName.Localize()));*/
        }
    }
}
