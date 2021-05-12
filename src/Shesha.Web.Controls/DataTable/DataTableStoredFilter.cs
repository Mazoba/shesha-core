using System;
using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;
using Shesha.Domain;
using Shesha.Domain.Attributes;
using Shesha.Domain.Enums;

namespace Shesha.Web.DataTable
{
    /// <summary>
    /// Filter model
    /// </summary>
    [AutoMapFrom(typeof(StoredFilter))]
    public class DataTableStoredFilter
    {
        /// <summary>
        /// 
        /// </summary>
        protected internal DataTableStoredFilter()
        {
        }

        /// <summary>
        /// Filter ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Display name of the stored filter
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Only one exclusive filter can be applied at a time
        /// </summary>
        public bool IsExclusive { get; set; }

        /// <summary>
        /// Private filters are managed within the data table control
        /// </summary>
        public bool IsPrivate { get; set; }

        /// <summary>
        /// Filter expression type (HQL / JsonLogic / Column / Composite / Code filter)
        /// </summary>
        [ReferenceList("Shesha.Framework", "FilterExpressionType")]
        public RefListFilterExpressionType ExpressionType { get; set; }

        /// <summary>
        /// Expression that defines the filter
        /// </summary>
        [StringLength(int.MaxValue)]
        public string Expression { get; set; }
    }
}