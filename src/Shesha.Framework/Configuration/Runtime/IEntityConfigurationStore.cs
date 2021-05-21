using System;
using System.Collections.Generic;

namespace Shesha.Configuration.Runtime
{
    /// <summary>
    /// Stores information about entities
    /// </summary>
    public interface IEntityConfigurationStore
    {
        /// <summary>
        /// Entity types dictionary (key - TypeShortAlias, value - type of entity)
        /// </summary>
        IDictionary<string, Type> EntityTypes { get; }

        /// <summary>
        /// Returns <see cref="EntityConfiguration"/> by entity type
        /// </summary>
        EntityConfiguration Get(Type entityType);

        /// <summary>
        /// Returns <see cref="EntityConfiguration"/> by type short alias
        /// </summary>
        EntityConfiguration Get(string typeShortAlias);
    }
}
