using System.ComponentModel;
using Shesha.Domain.Attributes;

namespace Shesha.Domain.Enums
{
    [ReferenceList("Shesha.Core", "TypeOfAccount")]
    public enum RefListTypeOfAccount : long
    {
        [Description("External (Active Directory)")]
        AD = 0,

        [Description("Internal (SQL account)")]
        SQL = 1
    }
}
