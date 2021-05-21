using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Abp.Domain.Entities;
using Shesha.Utilities;
using Shesha.Web.DataTable.Model;

namespace Shesha.Web.DataTable
{
    /// <summary>
    /// Child datatable configuration
    /// </summary>
    /// <typeparam name="TParent"></typeparam>
    /// <typeparam name="TChild"></typeparam>
    /// <typeparam name="TChildId"></typeparam>
    public class ChildDataTableConfig<TParent, TChild, TChildId> : DataTableConfig<TChild, TChildId>, IChildDataTableConfig
        where TChild : class, IEntity<TChildId>
        where TParent : class
    {

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="id"></param>
        public ChildDataTableConfig(string id) : base(id)
        {
        }

        /// <summary>
        /// Create child table with multiple owners (OwnerType + OwnerId)
        /// </summary>
        /// <param name="id">Table Id</param>
        /// <returns></returns>
        public static ChildDataTableConfig<TParent, TChild, TChildId> MultipleOwners(string id)
        {
            var config = new ChildDataTableConfig<TParent, TChild, TChildId>(id)
            {
                RelationshipType = RelationshipType.MultipleOwners
            };

            return config;
        }

        /// <summary>
        /// Create child table with one-to-many relation
        /// </summary>
        /// <param name="id">Table Id</param>
        /// <param name="parentFunc">Parent accessor</param>
        /// <returns></returns>
        public static ChildDataTableConfig<TParent, TChild, TChildId> OneToMany(string id, Expression<Func<TChild, TParent>> parentFunc)
        {
            var config = new ChildDataTableConfig<TParent, TChild, TChildId>(id)
            {
                RelationshipType = RelationshipType.OneToMany,
                Relationship_LinkToParent = ExpressionHelper.GetExpressionText(parentFunc)
            };

            return config;
        }

        /// <summary>
        /// Create child table with many-to-many relation
        /// </summary>
        /// <param name="id">Table Id</param>
        /// <param name="childsFunc">Child entities accessor</param>
        /// <returns></returns>
        public static ChildDataTableConfig<TParent, TChild, TChildId> ManyToMany(string id, Expression<Func<TParent, IList<TChild>>> childsFunc)
        {
            var config = new ChildDataTableConfig<TParent, TChild, TChildId>(id)
            {
                RelationshipType = RelationshipType.ManyToMany,
                Relationship_ChildsCollection = ExpressionHelper.GetExpressionText(childsFunc)
            };

            return config;
        }

        /// <summary>
        /// The PickerDialogConfiguration to use when selecting an entity to add as a child.
        /// </summary>
        //public PickerDialogConfig AttachPickerDialogConfig { get; set; }

        #region Parent

        /// InheritDoc
        public RelationshipType RelationshipType { get; set; }

        /// InheritDoc
        public string Relationship_LinkToParent { get; set; }

        /// <summary>
        /// The name of the property on a parent entity that contains a list of child entities.
        /// </summary>
        public string Relationship_ChildsCollection { get; set; }

        public Type ParentType => typeof(TParent);

        #endregion
    }
}
