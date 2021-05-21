using System;
using Abp.Application.Services;
using Shesha.Roles.Dto;
using Shesha.ShaRoles.Dto;

namespace Shesha.ShaRoles
{
    public interface IShaRoleAppService : IAsyncCrudAppService<ShaRoleDto, Guid, PagedRoleResultRequestDto, CreateShaRoleDto, ShaRoleDto>
    {
    }
}
