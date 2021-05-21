using System;
using Shesha.Domain.Enums;

namespace Shesha.Web.DataTable
{
    /// <summary>
    /// Data table stored filter registration attribute. Only use this for filters that are never reused. Otherwise please use class registration
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class StoredFilterAttribute : Attribute
    {
        /// <summary>
        /// Register a new system stored filter
        /// </summary>
        public StoredFilterAttribute(string id, string name, string expression)
        {
            Id = new Guid(id);
            Name = name;
            Expression = expression;
        }

        /// <summary>
        /// Register a new system stored filter
        /// </summary>
        public StoredFilterAttribute(string id, string name, string expression, string shaRoleNamespace, string shaRoleName) : this(id, name, expression)
        {
            VisibilityRoleNamespace = shaRoleNamespace;
            VisibilityRoleName = shaRoleName;
        }

        /// <summary>
        /// Register a new system stored filter
        /// </summary>
        public StoredFilterAttribute(string id, string name, string hql, string shaRoleNamespace, string shaRoleName, int orderIndex, string expression) : this(id, name, hql, shaRoleNamespace, shaRoleName)
        {
            OrderIndex = orderIndex;
            Expression = expression;
        }

        /// <summary>
        /// Filter ID
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Filter display name
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Filter expression type (HQL / JsonLogic / Column / Composite / Code filter)
        /// </summary>
        public RefListFilterExpressionType ExpressionType { get; set; }

        /// <summary>
        /// Expression that defines the filter
        /// </summary>
        public string Expression { get; }

        /// <summary>
        /// Items with `null` order index are shown in the filter list sorted alphabetically after showing filters with non-empty Order Index
        /// </summary>
        public int? OrderIndex { get; }

        /// <summary>
        /// When provided, the filter is only visible to selected roles
        /// </summary>
        public string VisibilityRoleNamespace { get; }

        /// <summary>
        /// When provided, the filter is only visible to selected roles
        /// </summary>
        public string VisibilityRoleName { get; }
    }
}
