using System;
using System.Collections.Generic;
using Shesha.AutoMapper.Dto;

namespace Shesha.ShaRoleAppointedPersons.Dto
{
    public class CreateShaRoleAppointedPersonDto: IShaRoleAppointedPersonDto
    {
        public Guid RoleId { get; set; }
        public EntityWithDisplayNameDto<Guid?> Person { get; set; }
        public List<EntityWithDisplayNameDto<Guid>> Regions { get; set; } = new List<EntityWithDisplayNameDto<Guid>>();
    }
}
