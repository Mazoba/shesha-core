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
        /// Returns <see cref="EntityConfiguration"/> by entity type
        /// </summary>
        EntityConfiguration Get(Type entityType);

        /// <summary>
        /// Returns <see cref="EntityConfiguration"/> by class name or type short alias
        /// </summary>
        EntityConfiguration Get(string nameOrAlias);
    }
}
