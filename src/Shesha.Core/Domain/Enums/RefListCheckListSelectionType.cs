using System;
using System.ComponentModel;
using Shesha.Domain.Attributes;

namespace Shesha.Domain.Enums
{
    /// <summary>
    /// Checklist selection type (yes/no/na)
    /// </summary>
    [ReferenceList("Shesha.Core", "CheckListSelectionType")]
    [Obsolete("Should use equivalent entity under Shesha.Enterprise")]
    public enum RefListCheckListSelectionType
    {
        /// <summary>
        /// Yes
        /// </summary>
        Yes = 1,

        /// <summary>
        /// No
        /// </summary>
        No = 2,
        
        /// <summary>
        /// N/A
        /// </summary>
        [Description("N/A")]
        NotAvailable = 3
    }
}
