using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;
using JetBrains.Annotations;
using Shesha.Authorization.Users;
using Shesha.Domain.Attributes;
using Shesha.Domain.Enums;

namespace Shesha.Domain
{
    /// <summary>
    /// A stored filter definition. This is used for both data table filters (index view selectors, saved filters and pre-defined filters) and the reporting framework report filters
    /// </summary>
    [Entity(TypeShortAlias = "Shesha.Framework.StoredFilter", FriendlyName = "Stored Filter", GenerateApplicationService = false)]
    public class StoredFilter : FullPowerEntity
    {
        /// <summary>
        /// List of objects that this filter can be added to. One of: report, data table by ID, entire entity type.
        /// </summary>
        [ManyToMany(table: "Frwk_StoredFilterContainers", childColumn: "FilterId", keyColumn: "Id", where: "IsDeleted=0")]
        public virtual IList<StoredFilterContainer> ContainerEntities { get; set; } = new List<StoredFilterContainer>();

        /// <summary>
        /// List of roles that can see the filter
        /// </summary>
        [ManyToMany(table: "Frwk_EntityVisibility", childColumn: "EntityId", "Id"/*, "Frwk_OwnerType='Shesha.Core.ShaRole' and IsDeleted=0"*/)]
        public virtual  IList<EntityVisibility> VisibleBy { get; set; } = new List<EntityVisibility>();

        /// <summary>
        /// User friendly name of the filter
        /// </summary>
        [EntityDisplayName, StringLength(255), Required, NotNull]
        public virtual string Name { get; set; }

        /// <summary>
        /// Namespace of the filter. Only necessary for the report sub-filters. Can be provided to limit output
        /// </summary>
        [StringLength(255)]
        public virtual string Namespace { get; set; }

        /// <summary>
        /// Filter description can be provided here
        /// </summary>
        [StringLength(int.MaxValue)]
        public virtual string Description { get; set; }

        /// <summary>
        /// Filter expression type (HQL / JsonLogic / Column / Composite / Code filter)
        /// </summary>
        [ReferenceList("Shesha.Framework", "FilterExpressionType")]
        public virtual RefListFilterExpressionType ExpressionType { get; set; }

        /// <summary>
        /// Expression that defines the filter
        /// </summary>
        [StringLength(int.MaxValue)]
        public virtual string Expression { get; set; }

        /// <summary>
        /// The name of the column that the filter is for (only for Column or Composite filter type)
        /// </summary>
        [StringLength(255)]
        public virtual string ColumnName { get; set; }

        /// <summary>
        /// Filter comparer: [ColumnName] (equals / start with / contains / ...) [ColumnFilterValue]
        /// </summary>
        [ReferenceList("Shesha.Framework", "FilterComparerType")]
        public virtual RefListFilterComparerType ColumnFilterComparerType { get; set; }

        /// <summary>
        /// When false, the filter is applied with ColumnFilterValue even when ColumnFilterValue is null or empty string
        /// </summary>
        public virtual bool ColumnDoNotApplyValue { get; set; }

        /// <summary>
        /// value to filter by (for pre-defined filters only). Placeholders can be used
        /// </summary>
        [StringLength(int.MaxValue)]
        public virtual string ColumnFilterValue { get; set; }



        /// <summary>
        /// A user who has created the filter. Null for system filters
        /// </summary>
        [ForeignKey("CreatorUserId")]
        public virtual User CreatorUser { get; set; }

        /// <summary>
        /// True for filters that should only be visible to creator:
        /// 1) filter drafts created by admins before publishing or
        /// 2) filters created by end users.
        /// System filters are never private.
        /// </summary>
        public virtual bool IsPrivate { get; set; }

        /// <summary>
        /// when true, this filter cannot be applied on top of other filter(s).
        /// This effects the following:
        /// 1) If a multi-check-list control is used for filter selection, selecting this filter is only if other filter is already checked (on the Data Table view or Report view),
        /// 2) Exclusive filters cannot be used as sub-filters of another filter
        /// </summary>
        public virtual bool IsExclusive { get; set; }

        /// <summary>
        /// Items with `null` order index are shown in the filter list sorted alphabetically after showing filters with non-empty Order Index
        /// </summary>
        public virtual int OrderIndex { get; set; }

        /// <summary>
        /// This feature is actively used in the Reporting Framework. specifies a path to a view containing the filter UI
        /// </summary>
        [StringLength(512)]
        public virtual string CustomFilterViewPath { get; set; }

        /// <summary>
        /// This is for cases when form designer is used for creating filters UI. Can replace CustomFilterViewPath in many cases
        /// </summary>
        public virtual Guid? CustomFilterFormId { get; set; }


    }
}
