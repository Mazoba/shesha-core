using System;
using System.Collections.Generic;
using Shesha.AutoMapper.Dto;

namespace Shesha.ShaRoleAppointedPersons.Dto
{
    public interface IShaRoleAppointedPersonDto
    {
        Guid RoleId { get; }
        EntityWithDisplayNameDto<Guid?> Person { get; }
        List<EntityWithDisplayNameDto<Guid>> Regions { get; }
    }
}
