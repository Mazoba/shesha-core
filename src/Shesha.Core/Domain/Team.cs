using Shesha.Domain.Attributes;

namespace Shesha.Domain
{
    [Discriminator]
    [Entity(TypeShortAlias = "Shesha.Core.Team")]
    public class Team : OrganisationBase<Team>
    {
        public virtual Area AreaLevel1 { get; set; }
        public virtual Area AreaLevel2 { get; set; }
        public virtual Area AreaLevel3 { get; set; }
        public virtual Area AreaLevel4 { get; set; }
    }
}