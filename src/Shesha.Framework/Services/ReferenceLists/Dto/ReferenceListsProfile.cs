using System;
using AutoMapper;
using Shesha.AutoMapper;
using Shesha.AutoMapper.Dto;
using Shesha.Domain;

namespace Shesha.Services.ReferenceLists.Dto
{
    public class ReferenceListsProfile: ShaProfile
    {
        public ReferenceListsProfile()
        {
            CreateMap<ReferenceListItem, ReferenceListItemDto>()
                .ForMember(u => u.ReferenceList,
                    options => options.MapFrom(e => e.ReferenceList != null ? new EntityWithDisplayNameDto<Guid?> { Id = e.ReferenceList.Id, DisplayText = e.ReferenceList.Name } : null))
                .MapReferenceListValuesToDto();

            CreateMap<ReferenceListItemDto, ReferenceListItem>()
                .ForMember(u => u.ReferenceList,
                    options => options.MapFrom(e =>
                        e.ReferenceList != null && e.ReferenceList.Id != null
                            ? GetEntity<ReferenceList, Guid>(e.ReferenceList.Id.Value)
                            : null))
                .MapReferenceListValuesFromDto();
            /*
            CreateMap<ReferenceListItemValueDto, int>().ConvertUsing(r => r != null ? r.ItemValue : -1);
            CreateMap<ReferenceListItemValueDto, int?>().ConvertUsing(r => r != null ? r.ItemValue : (int?)null);
            */

            /*
            AddMemberConfiguration().AddMember<ReferenceListItemValueDto>(m => m.);

            CreateMap<int, ReferenceListItemValueDto>().ConvertUsing(r => r != null ? r.ItemValue : -1);
            */

            // .ForMember(u => u.Status, options => options.MapFrom(e => GetRefListItemValueDto("Boxfusion.Shesha.DsdNpo.PropertyInspections", "PropertyInspectionStatus", (int?)e.Status)))
            //CreateMap<ShaRoleAppointedPersonDto, ShaRoleAppointedPerson>();
        }

    }
}
