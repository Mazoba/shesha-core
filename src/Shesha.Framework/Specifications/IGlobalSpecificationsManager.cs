using System.Collections.Generic;

namespace Shesha.Specifications
{
    /// <summary>
    /// Global specifications manager
    /// </summary>
    public interface IGlobalSpecificationsManager
    {
        /// <summary>
        /// List of global specifications
        /// </summary>
        List<ISpecificationInfo> Specifications { get; }
    }
}
