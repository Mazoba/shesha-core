using Hangfire;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.Server;
using Hangfire.States;
using Shesha.Reflection;
using Shesha.Scheduler.Exceptions;
using Shesha.Scheduler.Services.ScheduledJobs;
using Shesha.Services;
using System;
using System.Linq;
using System.Reflection;

namespace Shesha.Scheduler.Attributes
{
    /// <summary>
    /// Attribute to forward <see cref="DisableConcurrentExecutionAttribute"/> to <see cref="ScheduledJobAppService"/>
    /// </summary>
    public class ForwardDisableConcurrentExecutionAttribute : JobFilterAttribute, IServerFilter, IElectStateFilter
    {
        private const string LockAcquiredKey = "ShaLockAcquired";
        private const string DistributedLockKey = "DistributedLock";

        private readonly ILog _logger = LogProvider.For<ForwardDisableConcurrentExecutionAttribute>();

        public ForwardDisableConcurrentExecutionAttribute()
        {
        }

        public void OnPerforming(PerformingContext filterContext)
        {
            if (!IsApplicable(filterContext))
                return;

            var triggerId = (Guid)filterContext.BackgroundJob.Job.Args.First();

            var jobManager = StaticContext.IocManager.Resolve<IScheduledJobManager>();

            var jobType = jobManager.GetJobType(triggerId);
            
            if (jobType == null)
                return;

            var jobAttribute = jobType.GetAttribute<ScheduledJobAttribute>();
            if (jobAttribute == null)
                throw new NotSupportedException($"Job '{jobType.FullName}' must be decorated with '{nameof(ScheduledJobAttribute)}'");

            var disableConcurrentAttribute = jobType.GetAttribute<DisableConcurrentExecutionAttribute>();
            if (disableConcurrentAttribute == null)
                return;

            var resource = jobAttribute.Uid.ToString();

            var timeoutField = typeof(DisableConcurrentExecutionAttribute).GetField("_timeoutInSeconds", BindingFlags.NonPublic | BindingFlags.Instance);
            if (timeoutField == null)
                throw new NotSupportedException($"Failed to find timeout field on the '{nameof(DisableConcurrentExecutionAttribute)}'");

            var timeoutSeconds = (int)timeoutField.GetValue(disableConcurrentAttribute);

            var timeout = TimeSpan.FromSeconds(timeoutSeconds);

            var distributedLock = filterContext.Connection.AcquireDistributedLock(resource, timeout);
            filterContext.Items[DistributedLockKey] = distributedLock;
            filterContext.Items[LockAcquiredKey] = true;
        }

        public void OnPerformed(PerformedContext filterContext)
        {
            if (!IsApplicable(filterContext))
                return;

            if (!filterContext.Items.ContainsKey(LockAcquiredKey))
                return;

            if (!filterContext.Items.ContainsKey("DistributedLock"))
            {
                throw new InvalidOperationException("Can not release a distributed lock: it was not acquired.");
            }

            var distributedLock = (IDisposable)filterContext.Items[DistributedLockKey];
            distributedLock.Dispose();
        }

        private bool IsApplicable(PerformContext filterContext) 
        {
            return filterContext.BackgroundJob.Job.Method.DeclaringType == typeof(ScheduledJobAppService) && filterContext.BackgroundJob.Job.Method.Name == nameof(ScheduledJobAppService.RunTriggerAsync);
        }

        public void OnStateElection(ElectStateContext context)
        {
            var failedState = context.CandidateState as FailedState;
            if (failedState != null && (failedState.Exception is TriggerDeletedException || failedState.Exception is JobDeletedException))
            {
                context.CandidateState = new DeletedState
                {
                    Reason = failedState.Exception.Message
                };
            }
        }
    }
}
