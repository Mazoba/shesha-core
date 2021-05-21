using System;
using Shesha.Scheduler.Domain.Enums;
using Shesha.Utilities;

namespace Shesha.Scheduler.Attributes
{
    /// <summary>
    /// Contains basic info about scheduled job
    /// </summary>
    public class ScheduledJobAttribute : Attribute
    {
        /// <summary>
        /// Unique identifier of the job, all additional data in the DB is linked to the job using this Id
        /// </summary>
        public Guid Uid { get; set; }

        public string Description { get; set; }

        public string CronString { get; set; }
        public StartUpMode StartupMode { get; set; }

        public ScheduledJobAttribute(string uid, StartUpMode startupMode = StartUpMode.Automatic, string cronString = null, string description = null)
        {
            Uid = uid.ToGuid();
            StartupMode = startupMode;
            CronString = cronString;
            Description = description;
        }
    }
}
