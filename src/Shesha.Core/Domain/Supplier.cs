using Shesha.Domain.Attributes;
using Shesha.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Shesha.Domain
{
    [Entity(TypeShortAlias = "Shesha.Core.Supplier")]
    public class Supplier : Organisation
    {
        [StringLength(50)]
        public virtual string SupplierNo { get; set; }
        public virtual string Email { get; set; }
        public virtual string TellNo { get; set; }
        public virtual Address Address { get; set; }
        public virtual RefListSupplierStatus? Status { get; set; }

    }
}
