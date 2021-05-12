using System;
using System.Collections.Generic;
using System.Text;

namespace Shesha.Web.FormsDesigner.Legacy
{
    public class DisplayTextModel : ComponentModelBase
    {
        public bool AutoUpdate { get; set; }
        public bool IsCalculated { get; set; }
        public string Calculation { get; set; }
        public bool DisplayEntityAsLink { get; set; }
        public bool HideWhenEmpty { get; set; }
    }
}
