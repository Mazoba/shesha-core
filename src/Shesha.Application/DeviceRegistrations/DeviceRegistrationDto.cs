using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using Shesha.Domain;

namespace Shesha.DeviceRegistrations
{
    /// <summary>
    /// Registered mobile device DTO
    /// </summary>
    [AutoMap(typeof(DeviceRegistration))]
    public class DeviceRegistrationDto : EntityDto<Guid>
    {
        public string DeviceRegistrationId { get; set; }

        public Guid PersonId { get; set; }
    }
}
