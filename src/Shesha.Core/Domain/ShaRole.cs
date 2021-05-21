using System;
using System.ComponentModel.DataAnnotations;
using Abp.Auditing;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Shesha.Domain.Attributes;
using Shesha.EntityHistory;

namespace Shesha.Domain
{
    [Entity(TypeShortAlias = "Shesha.Core.ShaRole")]
    [DisplayManyToManyAuditTrail(typeof(ShaRoleAppointedPerson), "Person", DisplayName = "Member")]
    public class ShaRole: FullPowerEntity
    {
        [StringLength(200)]
        public virtual string NameSpace { get; set; }

        [StringLength(500)]
        [Audited]
        public virtual string Name { get; set; }

        [StringLength(2000)]
        [Audited]
        public virtual string Description { get; set; }

        public virtual int SortIndex { get; set; }

        // note: to be removed! todo: convert tu custom params
        public virtual bool IsRegionSpecific { get; set; }

        public virtual bool IsProcessConfigurationSpecific { get; set; }

        [Display(Name = "Hard linked to application", Description = "If true, indicates that the application logic references the value or name of this role and should therefore not be changed.")]
        public virtual bool HardLinkToApplication { get; protected set; }

        public virtual bool CanAssignToMultiple { get; set; }
        public virtual bool CanAssignToPerson { get; set; }
        public virtual bool CanAssignToRole { get; set; }
        public virtual bool CanAssignToOrganisationRoleLevel { get; set; }
        public virtual bool CanAssignToUnit { get; set; }
        
        public override string ToString()
        {
            return Name;
        }
    }
}