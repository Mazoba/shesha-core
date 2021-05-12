using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Shesha.Domain.Attributes;
using Shesha.Domain.Enums;

namespace Shesha.Domain
{
    [Entity(TypeShortAlias = "Shesha.Core.OrganisationPostAppointment")]
    public class OrganisationPostAppointment : FullAuditedEntity<Guid>, IMayHaveTenant
    {
        [Required]
        public virtual OrganisationPost OrganisationPost { get; set; }

        [Required]
        public virtual Employee Employee { get; set; }
        public virtual RefListPostAppointmentType AppointmentStatus { get; set; }
        public virtual DateTime? AppointmentStartDate { get; set; }
        public virtual DateTime? AppointmentEndDate { get; set; }

        public virtual string Comment { get; set; }

        public virtual StoredFile StoredFile { get; set; }

        /// <summary>
        /// If true, indicates that user has opened homepage for this appointment
        /// </summary>
        public virtual bool UserHasOpened { get; set; }
        public virtual int? TenantId { get; set; }
    }
}
