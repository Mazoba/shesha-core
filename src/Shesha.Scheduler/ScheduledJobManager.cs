using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Castle.Core.Logging;
using Hangfire;
using Hangfire.Storage;
using NHibernate.Linq;
using Shesha.Scheduler.Domain;
using Shesha.Scheduler.Domain.Enums;
using Shesha.Scheduler.Services.ScheduledJobs;
using Shesha.Scheduler.Utilities;

namespace Shesha.Scheduler
{
    /// inheritedDoc
    public class ScheduledJobManager: IScheduledJobManager, ITransientDependency
    {
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IRepository<ScheduledJobTrigger, Guid> _triggerRepository;
        public ILogger Logger { get; set; }

        public ScheduledJobManager(IRepository<ScheduledJobTrigger, Guid> triggerRepository, IUnitOfWorkManager unitOfWorkManager)
        {
            _triggerRepository = triggerRepository;
            _unitOfWorkManager = unitOfWorkManager;
            Logger = NullLogger.Instance;
        }

        /// inheritedDoc
        public async Task EnqueueAllAsync()
        {
            try
            {
                var activeTriggers = await _triggerRepository.GetAll()
                    .Where(t => t.Job.JobStatus == JobStatus.Active && t.Job.StartupMode == StartUpMode.Automatic && t.Status == TriggerStatus.Enabled)
                    .ToListAsync();

                // remove all unused triggers
                var allRecurringJobs = JobStorage.Current.GetConnection().GetRecurringJobs();
                var jobsToRemove = allRecurringJobs.Where(j => activeTriggers.All(t => t.Id.ToString() != j.Id)).ToList();

                foreach (var jobDto in jobsToRemove)
                {
                    RecurringJob.RemoveIfExists(jobDto.Id);
                }

                // update existing triggers
                foreach (var trigger in activeTriggers)
                {
                    if (!CronStringHelper.IsValidCronExpression(trigger.CronString))
                    {
                        Logger.Warn($"Trigger {trigger.Id} has has invalid CRON expression: {trigger.CronString} - skipped");
                        continue;
                    }

                    RecurringJob.AddOrUpdate<ScheduledJobAppService>(trigger.Id.ToString(), s => s.RunTriggerAsync(trigger.Id, CancellationToken.None), trigger.CronString);
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}
