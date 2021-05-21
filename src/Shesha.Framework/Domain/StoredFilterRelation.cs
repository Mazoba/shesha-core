using System;
using JetBrains.Annotations;
using Shesha.Domain.Attributes;
using Shesha.Domain.Enums;

namespace Shesha.Domain
{
    /// <summary>
    /// Sub-filters. JsonLogic is preferable, we may only need to create sub-filters if we'd like to reuse some parts of more complex filters
    /// </summary>
    [Entity(TypeShortAlias = "Shesha.Framework.StoredFilterRelation", FriendlyName = "Filter to sub-filter relation")]
    public class StoredFilterRelation : FullPowerEntity
    {
        /// <summary>
        /// Filter link
        /// </summary>
        [NotNull]
        public virtual StoredFilter Filter { get; set; }

        /// <summary>
        /// Sub-Filter link
        /// </summary>
        [NotNull]
        public virtual StoredFilter SubFilter { get; set; }
        
        /// <summary>
        /// Operator (AND / OR)
        /// </summary>
        [ReferenceList("Shesha.Framework", "FilterJoinOperator")]
        public virtual RefListFilterJoinOperator JoinOperator { get; set; }

        /// <summary>
        /// Sub-filter Order Index for case when a filter has more than 1 sub-filters. The order is important, especially when both OR and AND are used
        /// </summary>
        public virtual int OrderIndex { get; set; }
    }
}
