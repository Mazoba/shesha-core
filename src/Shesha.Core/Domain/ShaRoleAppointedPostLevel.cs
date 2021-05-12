using Shesha.Domain.Attributes;
using Shesha.Domain.Enums;

namespace Shesha.Domain
{
    [Entity(TypeShortAlias = "Shesha.Core.ShaRoleAppointedPostLevel", FriendlyName = "Appointed Post Level")]
    [DiscriminatorValue((int)ShaRoleAppointmentType.PostLevel)]
    public class ShaRoleAppointedPostLevel : ShaRoleAppointment
    {
        public virtual OrganisationPostLevel OrganisationPostLevel { get; set; }
    }
}