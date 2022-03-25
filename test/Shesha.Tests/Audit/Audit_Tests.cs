using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.EntityHistory;
using Abp.Json;
using NHibernate.Linq;
using Shesha.Domain;
using Shesha.Utilities;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Shesha.Tests.Audit
{
    public class Audit_Tests: SheshaNhTestBase
    {
        private readonly IRepository<PublicHoliday, Guid> _publicHolidayRepository;
        private readonly IRepository<EntityChange, Int64> _entityChangeRepository;
        private readonly IRepository<EntityPropertyChange, Int64> _entityPropertyChangeRepository;
        private readonly IUnitOfWorkManager _ouwManager;

        public Audit_Tests()
        {
            _publicHolidayRepository = Resolve<IRepository<PublicHoliday, Guid>>();
            _entityChangeRepository = Resolve<IRepository<EntityChange, Int64>>();
            _entityPropertyChangeRepository= Resolve<IRepository<EntityPropertyChange, Int64>>();
            _ouwManager = Resolve<IUnitOfWorkManager>();
        }

        [Fact]
        public async Task UpdatePropertyAudit_Test() 
        {
            var id = "22EADFDF-D2E5-4744-B2DC-F4EF6B927A9F".ToGuid();
            var publicHolidayType = typeof(PublicHoliday).FullName;
            var initialPropValue = "Holiday name";
            var changedPropValue = "Holiday new name";

            // clean-up test data (entity and audit)
            await DeletePublicHolidayAsync(id);
            await DeleteAuditAsync(publicHolidayType, id.ToString());

            // create entity
            await WithUnitOfWorkAsync(async () =>
            {
                var holiday = new PublicHoliday
                {
                    Id = id,
                    Date = DateTime.Now.Date,
                    Name = initialPropValue
                };

                await _publicHolidayRepository.InsertAsync(holiday);
            });

            // check audit - there should be a create event
            await WithUnitOfWorkAsync(async () => {
                var events = await _entityChangeRepository.GetAll()
                    .Where(c => c.EntityTypeFullName == publicHolidayType && c.EntityId == id.ToString())
                    .ToListAsync();

                events.ShouldNotBeEmpty("Audit events are not logged for newly created entity");
                events.ShouldHaveSingleItem("Only one audit event should be logged for newly created entity");

                events.FirstOrDefault().ChangeType.ShouldBe(Abp.Events.Bus.Entities.EntityChangeType.Created);
            });

            await WithUnitOfWorkAsync(async () =>
            {
                var holiday = await _publicHolidayRepository.FirstOrDefaultAsync(id);
                Assert.NotNull(holiday);

                holiday.Name = changedPropValue;
                await _publicHolidayRepository.UpdateAsync(holiday);
            });

            await WithUnitOfWorkAsync(async () => {
                var entityUpdateEvents = await _entityChangeRepository.GetAll()
                    .Where(c => c.EntityTypeFullName == publicHolidayType && c.EntityId == id.ToString() && c.ChangeType == Abp.Events.Bus.Entities.EntityChangeType.Updated)
                    .ToListAsync();

                entityUpdateEvents.ShouldNotBeEmpty("Change events are not logged for entity");
                entityUpdateEvents.ShouldHaveSingleItem("Only one `update` audit event should be logged");

                var updateEvent = entityUpdateEvents.FirstOrDefault();
                updateEvent.PropertyChanges.ShouldNotBeEmpty("Update event must not be empty");
                updateEvent.PropertyChanges.ShouldHaveSingleItem("Update event must contain only one property change");
                
                var propUpdate = updateEvent.PropertyChanges.FirstOrDefault();
                var originalValue = propUpdate.OriginalValue.FromJsonString<string>();
                originalValue.ShouldBe(initialPropValue);

                var newValue = propUpdate.NewValue.FromJsonString<string>();
                newValue.ShouldBe(changedPropValue);
            });

            await DeletePublicHolidayAsync(id);
        }

        [UnitOfWork]
        private async Task<List<EntityChange>> GetEntityAuditAsync(string entityType, string entityId) 
        {
            List<EntityChange> result = null;
            await WithUnitOfWorkAsync(async () => {
                result = await _entityChangeRepository.GetAll().Where(c => c.EntityTypeFullName == entityType && c.EntityId == entityId).ToListAsync();
            });
            return result;
        }

        private async Task DeleteAuditAsync(string entityType, string entityId)
        {
            await WithUnitOfWorkAsync(async () => {
                
                var changeIds = await _entityChangeRepository.GetAll()
                    .Where(c => c.EntityTypeFullName == entityType && c.EntityId == entityId)
                    .Select(c => c.Id)
                    .ToListAsync();

                foreach (var changeId in changeIds) 
                {
                    await _entityPropertyChangeRepository.DeleteAsync(c => c.EntityChangeId == changeId);
                    await _ouwManager.Current.SaveChangesAsync();
                    await _entityChangeRepository.DeleteAsync(changeId);
                }
            });
        }

        private async Task DeletePublicHolidayAsync(Guid id) 
        {
            await WithUnitOfWorkAsync(async () => {
                await _publicHolidayRepository.HardDeleteAsync(h => h.Id == id);
            });
        }
    }
}
