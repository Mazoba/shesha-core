using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Shesha.Domain.Attributes;

namespace Shesha.Domain
{
    /// <summary>
    /// Version of the <see cref="StoredFile"/>
    /// </summary>
    [Entity(TypeShortAlias = "Shesha.Framework.StoredFileVersion", GenerateApplicationService = false)]
    public class StoredFileVersion : FullAuditedEntity<Guid>, IMayHaveTenant
    {

        /// <summary>
        /// Stored file
        /// </summary>
        [Required]
        public virtual StoredFile File { get; set; }

        /// <summary>
        /// Version number
        /// </summary>
        public virtual int VersionNo { get; set; }

        /// <summary>
        /// File size
        /// </summary>
        public virtual Int64 FileSize { get; set; }

        /// <summary>
        /// File name
        /// </summary>
        public virtual string FileName { get; set; }

        /// <summary>
        /// File type (extension)
        /// </summary>
        public virtual string FileType { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public virtual string Description { get; set; }

        /// <summary>
        /// Indicated is version signed or not
        /// </summary>
        public virtual bool IsSigned { get; set; }

        /// <summary>
        /// Tenant Id
        /// </summary>
        public virtual int? TenantId { get; set; }
    }

}
