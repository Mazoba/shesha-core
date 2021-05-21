using Shesha.Domain.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shesha.Domain.Enums
{
    [ReferenceList("Shesha.Core", "SupplierStatus")]
    public enum RefListSupplierStatus : int
    {
        Activate = 1,
        DeActivated = 2
    }
}
