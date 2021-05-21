using System;
using System.Collections.Generic;
using System.Text;

namespace Shesha.Web.FormsDesigner.Legacy
{
    public class PanelModel : ComponentModelBase
    {
        public PanelModel()
        {
            Theme = "primary";
            Collapsible = true;
        }

        public string Theme { get; set; }

        public bool Collapsible { get; set; }
        public bool CollapsedByDefault { get; set; }

        public bool HideWhenEmpty { get; set; } = true;
    }
}
