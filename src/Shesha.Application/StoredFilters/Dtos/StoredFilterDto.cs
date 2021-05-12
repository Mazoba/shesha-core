using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using Shesha.AutoMapper.Dto;
using Shesha.Domain;
using Shesha.Domain.Attributes;
using Shesha.Domain.Enums;

namespace Shesha.StoredFilters.Dtos
{
    /// <summary>
    /// Stored Filter data contract
    /// </summary>
    [AutoMapFrom(typeof(StoredFilter))]
    public class StoredFilterDto : FullAuditedEntityDto<Guid>
    {
        /// <summary>
        /// List of container entity links such as Reporting Framework reports.
        /// Since we don't store data table configs in DB yet, they are listed separately for now: DataTableContainerIds
        /// </summary>
        public IList<EntityLinkDto> ContainerEntities { get; set; }

        /// <summary>
        /// User friendly name of the filter
        /// </summary>
        [StringLength(255), Required]
        public virtual string Name { get; set; }

        /// <summary>
        /// Namespace of the filter. Only necessary for the report sub-filters. Can be provided to limit output
        /// </summary>
        [StringLength(255), Required]
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
        public virtual RefListFilterComparerType ColumnFilterComparerType { get; set; }

        /// <summary>
        /// Value to compare with
        /// </summary>
        [StringLength(int.MaxValue)]
        public virtual string ColumnFilterValue { get; set; }

        /// <summary>
        /// When false, the filter is applied with ColumnFilterValue even when ColumnFilterValue is null or empty string
        /// </summary>
        public virtual bool ColumnDoNotApplyValue { get; set; }

        /// <summary>
        /// List of role entities that should be able to see the filter.
        /// It's normally list of ShaRoles or Person entities, can't reference ShaRole here because it's missing on the framework level.
        /// </summary>
        public IList<EntityLinkDto> VisibleBy { get; set; }
    }
}
