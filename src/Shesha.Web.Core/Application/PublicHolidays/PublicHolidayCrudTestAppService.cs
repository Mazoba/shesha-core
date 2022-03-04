using Abp.Domain.Repositories;
using Shesha.Domain;
using Shesha.DynamicEntities.Dtos;
using System;
using System.Threading.Tasks;

namespace Shesha.Application.Persons
{
    /*
    public class PublicHolidayCrudTestAppService : SheshaCrudAppServiceInternal
    {
        private readonly IRepository<PublicHoliday, Guid> _repository;

        public PublicHolidayCrudTestAppService(IRepository<PublicHoliday, Guid> repository)
        {
            _repository = repository;
        }

        public async Task<DynamicDto<PublicHoliday, Guid>> GetAsync(Guid id)
        {
            var entity = await _repository.GetAsync(id);

            var dto = await MapToDynamicDtoAsync<PublicHoliday, Guid>(entity);

            return dto;
        }
    }
    */
}
