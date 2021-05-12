using Shesha.Domain.Attributes;
using Shesha.Domain.Enums;

namespace Shesha.Domain
{
    [Entity(TypeShortAlias = "Shesha.Core.ShaRoleAppointedPost", FriendlyName = "Appointed Post")]
    [DiscriminatorValue((int)ShaRoleAppointmentType.Post)]
    public class ShaRoleAppointedPost : ShaRoleAppointment
    {
        public virtual OrganisationPost OrganisationPost { get; set; }
    }
}