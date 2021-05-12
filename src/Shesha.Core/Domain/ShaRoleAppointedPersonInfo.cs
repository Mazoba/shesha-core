using System;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Shesha.Domain.Attributes;

namespace Shesha.Domain
{
    [Table("vw_core_ShaRoleAppointedPersons")]
    [ImMutable]
    public class ShaRoleAppointedPersonInfo: Entity<Guid>
    {
        public virtual ShaRole Role { get; set; }
        public virtual int? TenantId { get; set; }
        public virtual Person Person { get; set; }
        public virtual string Teams { get; set; }
        public virtual string Regions { get; set; }

        public virtual DateTime? CreationTime { get; set; }
        public virtual DateTime? LastModificationTime { get; set; }
    }
}
