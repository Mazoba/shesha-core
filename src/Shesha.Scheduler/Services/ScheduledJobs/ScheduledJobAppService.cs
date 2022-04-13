using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Reflection;
using Castle.Core.Internal;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using NHibernate.Linq;
using Shesha.Authorization.Users;
using Shesha.Scheduler.Attributes;
using Shesha.Scheduler.Bootstrappers;
using Shesha.Scheduler.Domain;
using Shesha.Scheduler.Services.ScheduledJobs.Dto;
using Shesha.Web.DataTable;

namespace Shesha.Scheduler.Services.ScheduledJobs
{
    /// <summary>
    /// Scheduled Job application service
    /// </summary>
    public class ScheduledJobAppService: SheshaCrudServiceBase<ScheduledJob, ScheduledJobDto, Guid>, ITransientDependency
    {
        private readonly IScheduledJobManager _jobManager;
        private readonly IRepository<ScheduledJob, Guid> _jobRepo;

        public IRepository<User, Int64> UserRepository { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ScheduledJobAppService(IRepository<ScheduledJob, Guid> repository, IScheduledJobManager jobManager, IRepository<ScheduledJob, Guid> jobRepo) : base(repository)
        {
            _jobManager = jobManager;
            _jobRepo = jobRepo;
        }

        /// <summary>
        /// Run scheduled job
        /// </summary>
        /// <param name="id">Scheduled job Id</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<Guid> StartJobAsync(Guid id, CancellationToken cancellationToken)
        {
            // note: special code fore manual jobs
            var scheduledJob = await _jobRepo.GetAll().FirstOrDefaultAsync(j => j.Id == id, cancellationToken: cancellationToken);

            if (scheduledJob == null)
                throw new Exception("Job with the specified Id not found");

            var job = GetJobInstanceById(id);
            var executionId = Guid.NewGuid();
            await job.CreateExecutionRecordAsync(executionId,
                execution =>
                {
                    execution.Status = Domain.Enums.ExecutionStatus.Enqueued;
                    execution.StartedBy = AbpSession.UserId.HasValue
                        ? UserRepository.Get(AbpSession.UserId.Value)
                        : null;
                }
            );

            var jobId = BackgroundJob.Enqueue(() => job.ExecuteAsync(executionId, AbpSession.UserId, cancellationToken));
            return executionId;
        }

        /// <summary>
        /// Enqueue all jobs using Hangfire
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task EnqueueAll()
        {
            await _jobManager.EnqueueAllAsync();
        }

        /// <summary>
        /// Run scheduled job trigger
        /// </summary>
        /// <param name="triggerId">Trigger Id</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        public async Task RunTriggerAsync(Guid triggerId, CancellationToken cancellationToken)
        {
            var triggerService = IocManager.Resolve<IRepository<ScheduledJobTrigger, Guid>>();
            Guid jobId;

            using (var uow = UnitOfWorkManager.Begin())
            {
                // switch off the `SoftDelete` filter to skip job execution by a normal way and prevent unneeded retries
                using (UnitOfWorkManager.Current.DisableFilter(AbpDataFilters.SoftDelete))
                {
                    var trigger = await triggerService.GetAsync(triggerId);
                    if (trigger.IsDeleted)
                    {
                        Logger.Warn($"Trigger with Id = '{triggerId}' is deleted, execution skipped");
                        return;
                    }

                    if (trigger.Job.IsDeleted)
                    {
                        Logger.Warn($"Job with Id = '{triggerId}' is deleted, execution of trigger '{triggerId}' skipped");
                        return;
                    }

                    jobId = trigger.Job.Id;
                }

                await uow.CompleteAsync();
            }

            var job = GetJobInstanceById(jobId);
            job.TriggerId = triggerId;

            await job.ExecuteAsync(Guid.NewGuid(), AbpSession.UserId, cancellationToken);
        }

        private ScheduledJobBase GetJobInstanceById(Guid id)
        {
            var typeFinder = IocManager.Resolve<ITypeFinder>();
            var jobType = typeFinder.Find(t => t.GetAttribute<ScheduledJobAttribute>()?.Uid == id).FirstOrDefault();
            if (jobType == null)
                throw new Exception($"Job with Id = '{id}' not found");
            
            var jobInstance = IocManager.Resolve(jobType) as ScheduledJobBase;
            return jobInstance;
        }

        /// <summary>
        /// Bootstraps all scheduled jobs and default CRON triggers
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<string> BootstrapScheduledJobs()
        {
            var bootstrapper = IocManager.Resolve<ScheduledJobBootstrapper>();
            await bootstrapper.Process();
            return "Bootstrapped successfully";
        }

        /// <summary>
        /// Index table configuration
        /// </summary>
        /// <returns></returns>
        public static DataTableConfig IndexTable()
        {
            var table = new DataTableConfig<ScheduledJob, Guid>("ScheduledJob_Index");

            table.AddProperty(e => e.JobNamespace, m => m.WidthPixels(105));
            table.AddProperty(e => e.JobName, m => m.WidthPixels(235));
            table.AddProperty(e => e.JobDescription, m => m.WidthPixels(350));
            table.AddProperty(e => e.StartupMode, m => m.WidthPixels(80));
            table.AddProperty(e => e.JobStatus, m => m.WidthPixels(70));

            return table;
        }

        /// inheritedDoc
        public override async Task<ScheduledJobDto> CreateAsync(ScheduledJobDto input)
        {
            var result = await base.CreateAsync(input);

            await UnitOfWorkManager.Current.SaveChangesAsync();

            // sync with Hangfire
            await _jobManager.EnqueueAllAsync();

            return result;
        }

        /// inheritedDoc
        public override async Task<ScheduledJobDto> UpdateAsync(ScheduledJobDto input)
        {
            var result = await base.UpdateAsync(input);

            await UnitOfWorkManager.Current.SaveChangesAsync();

            // sync with Hangfire
            await _jobManager.EnqueueAllAsync();

            return result;
        }

        /// inheritedDoc
        public override async Task DeleteAsync(EntityDto<Guid> input)
        {
            await base.DeleteAsync(input);

            await UnitOfWorkManager.Current.SaveChangesAsync();

            // sync with Hangfire
            await _jobManager.EnqueueAllAsync();
        }
    }
}
