using System;
using System.ComponentModel.DataAnnotations;
using Shesha.Domain.Attributes;

namespace Shesha.Domain
{
    [Entity(TypeShortAlias = "Shesha.Core.OrganisationBankAccount")]
    public class OrganisationBankAccount : RelationshipEntityBase<Guid>
    {
        [Required]
        public virtual Organisation Organisation { get; set; }

        [Required]
        public virtual BankAccount BankAccount { get; set; }

        [ReferenceList("Shesha.Core", "OrganisationBankAccountRole")]
        public override int? Role { get; set; }
    }
}
