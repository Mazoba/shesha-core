using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities.Auditing;
using Shesha.Domain.Attributes;
using Shesha.Scheduler.Domain.Enums;

namespace Shesha.Scheduler.Domain
{
    [Entity(TypeShortAlias = "Shesha.Scheduler.ScheduledJob")]
    public class ScheduledJob: FullAuditedEntity<Guid>
    {
        /// <summary>
        /// Name of the scheduled job
        /// </summary>
        [EntityDisplayName]
        [StringLength(300, MinimumLength = 3)]
        public virtual string JobName { get; set; }

        /// <summary>
        /// Namespace
        /// </summary>
        [StringLength(300, MinimumLength = 3)]
        public virtual string JobNamespace { get; set; }

        /// <summary>
        /// Description of the job
        /// </summary>
        [DataType(DataType.MultilineText)]
        [StringLength(int.MaxValue)]
        public virtual string JobDescription { get; set; }

        /// <summary>
        /// Job status (Active/Inactive). Is used to switch job on/off.
        /// </summary>
        public virtual JobStatus JobStatus { get; set; }

        /// <summary>
        /// Startup mode (Automatic/Manual)
        /// </summary>
        public virtual StartUpMode StartupMode { get; set; }
    }
}
