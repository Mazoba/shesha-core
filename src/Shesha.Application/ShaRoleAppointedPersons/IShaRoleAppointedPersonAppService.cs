using System;
using Abp.Application.Services;
using Shesha.Roles.Dto;
using Shesha.ShaRoleAppointedPersons.Dto;

namespace Shesha.ShaRoleAppointedPersons
{
    public interface IShaRoleAppointedPersonAppService : IAsyncCrudAppService<ShaRoleAppointedPersonDto, Guid, PagedRoleResultRequestDto, CreateShaRoleAppointedPersonDto, ShaRoleAppointedPersonDto>
    {
    }
}
