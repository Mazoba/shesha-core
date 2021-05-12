using System.Threading;
using System.Threading.Tasks;
using Abp.Dependency;
using Shesha.Scheduler.Attributes;
using Shesha.Scheduler.Domain.Enums;

namespace Shesha.Scheduler
{
    [ScheduledJob("305CDDB9-E2CA-4E6A-BE25-0FCDD8303F37", StartUpMode.Manual)]
    public class TestJob: ScheduledJobBase, ITransientDependency
    {
        public override async Task DoExecuteAsync(CancellationToken cancellationToken)
        {
            Log.Info("Started...");

            for (int i = 0; i < 100; i++)
            {
                Thread.Sleep(100);
                Log.Info($"processing {i}");
            }

            Log.Info("Finished...");
        }
    }
}
