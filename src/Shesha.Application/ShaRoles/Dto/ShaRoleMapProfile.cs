using AutoMapper;
using Shesha.Domain;

namespace Shesha.ShaRoles.Dto
{
    public class ShaRoleAppointedPersonMapProfile : Profile
    {
        public ShaRoleAppointedPersonMapProfile()
        {
            CreateMap<CreateShaRoleDto, ShaRole>();

            CreateMap<ShaRoleDto, ShaRole>();
            CreateMap<ShaRole, ShaRoleDto>();
        }
    }
}
