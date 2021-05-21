using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Shesha.Domain.Attributes;
using Shesha.Domain.Enums;

namespace Shesha.Domain
{
    [Discriminator]
    public class ImportResult : FullAuditedEntity<Guid>, IMayHaveTenant
    {
        public virtual DateTime? StartedOn { get; set; }
        public virtual DateTime? FinishedOn { get; set; }

        public virtual bool IsSuccess { get; set; }
        public virtual string ErrorMessage { get; set; }

        public virtual int RowsAffected { get; set; }

        public virtual int RowsSkipped { get; set; }
        public virtual int RowsInserted { get; set; }
        public virtual int RowsUpdated { get; set; }
        public virtual int RowsInactivated { get; set; }

        [Display(Name = "Rows Skipped (not changed)")]
        public virtual int RowsSkippedNotChanged { get; set; }

        [Display(Name = "Avg speed (rps)")]
        public virtual decimal AvgSpeed { get; set; }

        [StringLength(300)]
        public virtual string Comment { get; set; }

        public virtual StoredFile LogFile { get; set; }
        public virtual StoredFile ImportedFile { get; set; }
        [StringLength(50)]
        public virtual string ImportedFileMD5 { get; set; }

        /// <summary>
        /// Type of the data source
        /// </summary>
        public virtual RefListImportSourceType? SourceType { get; set; }

        public virtual int? TenantId { get; set; }
    }
}
