using System;

namespace Shesha.Specifications
{
    /// <summary>
    /// Stores basic information about specifications
    /// </summary>
    public interface ISpecificationInfo
    {
        /// <summary>
        /// Type of specifications
        /// </summary>
        Type SpecificationsType { get; }

        /// <summary>
        /// Type of Entity
        /// </summary>
        Type EntityType { get; }
    }
}
