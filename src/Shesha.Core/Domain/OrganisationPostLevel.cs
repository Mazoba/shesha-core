using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Shesha.Domain.Attributes;

namespace Shesha.Domain
{
    [Entity(TypeShortAlias = "Shesha.Core.OrganisationPostLevel", FriendlyName = "Post Level")]
    public class OrganisationPostLevel : FullAuditedEntity<Guid>, IMayHaveTenant
    {
        [StringLength(30)]
        public virtual string ShortName { get; set; }

        [StringLength(100)]
        [EntityDisplayName]
        public virtual string FullName { get; set; }

        [StringLength(300)]
        public virtual string Description { get; set; }
        public virtual Decimal? SignOffAmount { get; set; }
        public virtual int? DaysAllowedToRespond { get; set; }
        public virtual int? RankLevel { get; set; }

        #region Compare operators
        public virtual int CompareTo(object obj)
        {
            if (obj is OrganisationPostLevel compareToPostLevel)
            {
                return
                    !RankLevel.HasValue && !compareToPostLevel.RankLevel.HasValue
                        ? 0
                        : !RankLevel.HasValue
                            ? 1
                            : !compareToPostLevel.RankLevel.HasValue
                                ? -1
                                : RankLevel < compareToPostLevel.RankLevel
                                    ? 1
                                    : RankLevel > compareToPostLevel.RankLevel
                                        ? -1
                                        : 0;
            }
            throw new ArgumentException();
        }

        public static bool operator <(OrganisationPostLevel p1, OrganisationPostLevel p2)
        {
            return p1 != null && p1.CompareTo(p2) < 0;
        }

        public static bool operator >(OrganisationPostLevel p1, OrganisationPostLevel p2)
        {
            return p1 != null && p1.CompareTo(p2) > 0;
        }

        public static bool operator <=(OrganisationPostLevel p1, OrganisationPostLevel p2)
        {
            return p1 != null && p1.CompareTo(p2) <= 0;
        }

        public static bool operator >=(OrganisationPostLevel p1, OrganisationPostLevel p2)
        {
            return p1 != null && p1.CompareTo(p2) >= 0;
        }
        #endregion

        public virtual int? TenantId { get; set; }
    }
}
