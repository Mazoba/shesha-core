using Abp.Domain.Repositories;
using Shesha.Domain;
using Shesha.DynamicEntities.Dtos;
using System;
using System.Threading.Tasks;

namespace Shesha.Application.Persons
{
    public class PublicHolidayTestAppService: SheshaAppServiceBase
    {
        private readonly IRepository<PublicHoliday, Guid> _repository;

        public PublicHolidayTestAppService(IRepository<PublicHoliday, Guid> repository)
        {
            _repository = repository;
        }

        public async Task<DynamicDto<PublicHoliday, Guid>> GetAsync(Guid id)
        {
            var entity = await _repository.GetAsync(id);

            var dto = await MapToDynamicDtoAsync<PublicHoliday, Guid>(entity);

            return dto;
        }

        public async Task<DynamicDto<PublicHoliday, Guid>> UpdateAsync(DynamicDto<PublicHoliday, Guid> input)
        {
            var entity = await _repository.GetAsync(input.Id);

            await MapDynamicDtoToEntityAsync<DynamicDto<PublicHoliday, Guid>, PublicHoliday, Guid>(input, entity);

            await _repository.UpdateAsync(entity);

            return await MapToDynamicDtoAsync<PublicHoliday, Guid>(entity);
        }
    }
}
