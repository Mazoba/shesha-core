using Shesha.Web.DataTable.Model;
using System;

namespace Shesha.Web.DataTable
{
    /// <summary>
    /// Child Data Table configuration interface
    /// </summary>
    public interface IChildDataTableConfig : IDataTableConfig
    {
        /// <summary>
        /// Type of relationship
        /// </summary>
        RelationshipType RelationshipType { get; set; }

        /// <summary>
        /// The name of the property on a child entity that identifies the parent object.
        /// </summary>
        string Relationship_LinkToParent { get; set; }

        /// <summary>
        /// The name of the property on a parent entity that contains a list of child entities (for many-to-many relations)
        /// </summary>
        string Relationship_ChildsCollection { get; set; }

        /// <summary>
        /// Parent type
        /// </summary>
        Type ParentType { get; }
    }
}
