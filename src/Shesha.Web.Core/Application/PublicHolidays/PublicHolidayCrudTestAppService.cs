using Abp.Application.Services.Dto;
using Abp.Domain.Repositories;
using Shesha.Domain;
using Shesha.DynamicEntities.Dtos;
using System;
using System.Threading.Tasks;

namespace Shesha.Application.Persons
{
    public class PublicHolidayCrudTestAppService : SheshaCrudServiceBase<PublicHoliday, DynamicDto<PublicHoliday, Guid>, Guid>
    {
        public PublicHolidayCrudTestAppService(IRepository<PublicHoliday, Guid> repository) : base(repository)
        {
        }

        public override Task<DynamicDto<PublicHoliday, Guid>> GetAsync(EntityDto<Guid> input)
        {
            return base.GetAsync(input);
        }

        public async Task<DynamicDto<PublicHoliday, Guid>> TestGetAsync(Guid id)
        {
            var entity = await Repository.GetAsync(id);

            var dto = await MapToDynamicDtoAsync<PublicHoliday, Guid>(entity);

            return dto;
        }
    }
}
