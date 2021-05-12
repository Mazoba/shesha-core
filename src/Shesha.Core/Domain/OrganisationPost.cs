using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using JetBrains.Annotations;
using Shesha.Domain.Attributes;

namespace Shesha.Domain
{
    
    [Entity(TypeShortAlias = "Shesha.Core.OrganisationPost")]
    public class OrganisationPost : FullAuditedEntity<Guid>, IMayHaveTenant
    {
        [StringLength(100)]
        [EntityDisplayName]
        public virtual string Name { get; set; }

        [StringLength(100)]
        public virtual string ShortName { get; set; }

        [StringLength(100)]
        public virtual string PostDiscriminator { get; set; }

        [CanBeNull]
        public virtual OrganisationPostLevel OrganisationPostLevel { get; set; }

        public virtual OrganisationUnit OrganisationUnit { get; set; }

        [Display(Name = "Supervisor Post")]
        public virtual OrganisationPost SupervisorPost { get; set; }
        
        [DisplayFormat(DataFormatString = "Yes|No")]
        public virtual bool IsUnitSupervisor { get; set; }

        /// <summary>
        /// Name with PostDiscriminator
        /// </summary>
        public virtual string DiscriminatorName => $"{Name} {PostDiscriminator ?? ""}".Trim();
        public virtual int? TenantId { get; set; }
        public virtual object Clone()
        {
            return MemberwiseClone();
        }
    }
}
