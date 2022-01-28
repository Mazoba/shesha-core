using System;

namespace Shesha.Domain.Attributes
{
    /// <summary>
    /// Provides metadata to a domain entity
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class EntityAttribute : Attribute
    {
        /// <summary>
        /// Specifies friendly name of the entity that should be shown to user when required
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        /// This is a short version of the Type name of the entity class that is unique within 
        /// all the entities in the current solution. 
        /// The Alias must be 50 characters long or less. This is also typically match
        /// the Discriminator value defined for the entity on NHibernate mapping if the entity
        /// is a subclass of another entity.
        /// </summary>
        public string TypeShortAlias { get; set; }

        /// <summary>
        /// If true, indicates that an Application Service should be generated automatically for this entity. True by default
        /// </summary>
        public bool GenerateApplicationService { get; set; }

        /// <summary>
        /// Name of the auto-generated applicaiton service, applicable only when <see cref="GenerateApplicationService"/> is true. Leave empty to use class name as a name of the applicaiton service
        /// </summary>
        public string ApplicationServiceName { get; set; }

        public EntityAttribute()
        {
            GenerateApplicationService = true;
        }
    }
}
