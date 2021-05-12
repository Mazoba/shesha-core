using System;

namespace Shesha.Domain.Attributes
{
    /// <summary>
    /// Provides metadata to a domain entity
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    //[MeansImplicitUse]
    public class EntityAttribute : Attribute
    {
        /// <summary>
        /// Specifies friendly name of the entity that should be shown to user when required
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        /// This is a short version of the Type name of the entity class that is unique within 
        /// all the entities in the current solution. 
        /// The Alias must be 30 characters long or less. This is also typically match
        /// the Discriminator value defined for the entity on NHibernate mapping if the entity
        /// is a subclass of another entity.
        /// </summary>
        public string TypeShortAlias { get; set; }


        /*
         potentially unneeded properties, to be uncommented on demand

        /// <summary>
        /// Specifies the default view to use when drilling through to an entity of the marked type.
        /// String format will typically follow the following format: '~/ControllerName/ViewName/{0}'
        /// where '{0}' will be substituted for the entity id.
        /// </summary>
        public string DrillToView { get; set; }

        /// <summary>
        /// The name of the controller that allows the user to view and manipulate this entity.
        /// By default the framework will assume the controller name is the same as the class name.
        /// </summary>
        public string ControllerName { get; set; }

        /// <summary>
        /// The name of the action to view the details of the entity. 
        /// By default the framework will assume it is 'Details'.
        /// </summary>
        public string DetailsActionName { get; set; }

        /// <summary>
        /// The name of the action to Edit the details of the entity. 
        /// By default the framework will assume it is 'Edit'.
        /// </summary>
        public string EditActionName { get; set; }

        /// <summary>
        /// The name of the action to create an entity. 
        /// By default the framework will assume it is 'Create'.
        /// </summary>
        public string CreateActionName { get; set; }

        /// <summary>
        /// The name of the action to Delete an entity. 
        /// By default the framework will assume it is 'Delete'.
        /// </summary>
        public string DeleteActionName { get; set; }

        /// <summary>
        /// The name of the action to create an entity. 
        /// By default the framework will assume it is 'Inactivate'.
        /// </summary>
        public string InactivateActionName { get; set; }
         */
    }

}
