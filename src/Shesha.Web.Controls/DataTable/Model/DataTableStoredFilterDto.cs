using System;
using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;
using Shesha.Domain.Attributes;
using Shesha.Domain.Enums;
using Shesha.Web.DataTable;

namespace Shesha.Web.DataTable.Model
{
    /// <summary>
    /// Stored filter data contract
    /// </summary>
    [AutoMapFrom(typeof(DataTableStoredFilter))]
    public class DataTableStoredFilterDto
    {
        /// <summary>
        /// Filter ID
        /// </summary>
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        /// Display name of the stored filter
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Only one exclusive filter can be applied at a time
        /// </summary>
        public bool IsExclusive { get; set; }

        /// <summary>
        /// Private filters are managed within the datatable control
        /// </summary>
        public bool IsPrivate { get; set; }

        /// <summary>
        /// Filter expression type (HQL / JsonLogic / Column / Composite / Code filter)
        /// </summary>
        [ReferenceList("Shesha.Framework", "FilterExpressionType")]
        public string ExpressionType { get; set; }

        /// <summary>
        /// Expression that defines the filter
        /// </summary>
        [StringLength(int.MaxValue)]
        public string Expression { get; set; }
    }
}
