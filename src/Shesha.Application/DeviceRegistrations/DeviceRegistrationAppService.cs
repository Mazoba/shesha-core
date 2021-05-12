using System;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Shesha.Domain;

namespace Shesha.DeviceRegistrations
{
    [AbpAuthorize()]
    public class DeviceRegistrationAppService : SheshaCrudServiceBase<DeviceRegistration, DeviceRegistrationDto, Guid>
    {
        public DeviceRegistrationAppService(IRepository<DeviceRegistration, Guid> repository) : base(repository)
        {
        }

        public override async Task<DeviceRegistrationDto> CreateAsync(DeviceRegistrationDto input)
        {
            CheckCreatePermission();

            var entity = MapToEntity(input);

            entity.Person = await GetCurrentPersonAsync();

            await Repository.InsertAsync(entity);
            await CurrentUnitOfWork.SaveChangesAsync();

            return MapToEntityDto(entity);
        }
    }
}
