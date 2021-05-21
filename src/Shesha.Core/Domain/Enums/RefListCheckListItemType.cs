using System.ComponentModel;
using Shesha.Domain.Attributes;

namespace Shesha.Domain.Enums
{
    /// <summary>
    /// Type of the check list item
    /// </summary>
    [ReferenceList("Shesha.Core", "CheckListItemType")]
    public enum RefListCheckListItemType
    {
        /// <summary>
        /// Group
        /// </summary>
        [Description("Group")]
        Group = 1,

        /// <summary>
        /// Two state item (yes/no)
        /// </summary>
        [Description("Two state (yes/no)")]
        TwoState = 2,

        /// <summary>
        /// Tri state (yes/no/na)
        /// </summary>
        [Description("Tri state (yes/no/na)")]
        ThreeStateTriState = 3,
    }
}
