using System.Threading.Tasks;

namespace Shesha.Scheduler
{
    /// <summary>
    /// Scheduled jobs manages
    /// </summary>
    public interface IScheduledJobManager
    {
        /// <summary>
        /// Enqueue all jobs using Hangfire
        /// </summary>
        Task EnqueueAllAsync();
    }
}
