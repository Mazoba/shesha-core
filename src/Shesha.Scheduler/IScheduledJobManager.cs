using System;
using System.Threading.Tasks;

namespace Shesha.Scheduler
{
    /// <summary>
    /// Scheduled jobs manager
    /// </summary>
    public interface IScheduledJobManager
    {
        /// <summary>
        /// Enqueue all jobs using Hangfire
        /// </summary>
        Task EnqueueAllAsync();

        /// <summary>
        /// Get job type by trigger Id
        /// </summary>
        /// <param name="triggerId">Trigger Id</param>
        /// <returns></returns>
        Type GetJobType(Guid triggerId);
    }
}
