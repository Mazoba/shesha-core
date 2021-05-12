using System;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using Shesha.Scheduler.Domain;
using Shesha.Scheduler.Services.ScheduledJobs.Dto;
using Shesha.Web.DataTable;

namespace Shesha.Scheduler.Services.ScheduledJobs
{
    /// <summary>
    /// Scheduled Job Trigger application service
    /// </summary>
    public class ScheduledJobTriggerAppService : AsyncCrudAppService<ScheduledJobTrigger, ScheduledJobTriggerDto, Guid>, ITransientDependency
    {
        private readonly IScheduledJobManager _jobManager;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="jobManager"></param>
        public ScheduledJobTriggerAppService(IRepository<ScheduledJobTrigger, Guid> repository, IScheduledJobManager jobManager) : base(repository)
        {
            _jobManager = jobManager;
        }

        /*
        public static void RunTrigger(Guid triggerId)
        {
            var triggerService = StaticContext.IocManager.Resolve<IRepository<ScheduledJobTrigger, Guid>>();
            var trigger = triggerService.Get(triggerId);

            var job = GetJobInstanceById(trigger.Job.Id);

            job.TriggerId = trigger.Id;

            job.Execute();
        }

        private static ScheduledJobBase GetJobInstanceById(Guid id)
        {
            var typeFinder = StaticContext.IocManager.Resolve<ITypeFinder>();
            var jobType = typeFinder.Find(t => t.GetAttribute<ScheduledJobAttribute>()?.Uid == id).FirstOrDefault();
            if (jobType == null)
                throw new Exception($"Job with Id = '{id}' not found");
            
            var jobInstance = StaticContext.IocManager.Resolve(jobType) as ScheduledJobBase;
            return jobInstance;
        }
        */

        /// <summary>
        /// Scheduled job triggers index table
        /// </summary>
        /// <returns></returns>
        public static ChildDataTableConfig<ScheduledJob, ScheduledJobTrigger, Guid> ScheduledJobTriggers()
        {
            var table = ChildDataTableConfig<ScheduledJob, ScheduledJobTrigger, Guid>.OneToMany("ScheduledJob_Triggers", e => e.Job);

            table.AddProperty(e => e.Status);
            table.AddProperty(e => e.CronString);
            table.AddProperty(e => e.Description);

            return table;
        }

        /// inheritedDoc
        public override async Task<ScheduledJobTriggerDto> CreateAsync(ScheduledJobTriggerDto input)
        {
            var result = await base.CreateAsync(input);

            await UnitOfWorkManager.Current.SaveChangesAsync();

            // sync with Hangfire
            await _jobManager.EnqueueAllAsync();

            return result;
        }

        /// inheritedDoc
        public override async Task<ScheduledJobTriggerDto> UpdateAsync(ScheduledJobTriggerDto input)
        {
            var result = await base.UpdateAsync(input);

            await UnitOfWorkManager.Current.SaveChangesAsync();

            // sync with Hangfire
            await _jobManager.EnqueueAllAsync();

            return result;
        }

        /// inheritedDoc
        [HttpPost]
        public override async Task DeleteAsync(EntityDto<Guid> input)
        {
            await base.DeleteAsync(input);

            await UnitOfWorkManager.Current.SaveChangesAsync();

            // sync with Hangfire
            await _jobManager.EnqueueAllAsync();
        }
    }
}
