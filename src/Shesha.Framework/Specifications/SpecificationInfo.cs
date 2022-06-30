using System;

namespace Shesha.Specifications
{
    /// <summary>
    /// Stores basic information about specifications
    /// </summary>
    public class SpecificationInfo: ISpecificationInfo
    {
        /// <summary>
        /// Type of specifications
        /// </summary>
        public Type SpecificationsType { get; set; }

        /// <summary>
        /// Type of Entity
        /// </summary>
        public Type EntityType { get; set; }
    }
}
