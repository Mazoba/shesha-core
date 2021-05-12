using System;
using System.Collections.Generic;
using System.Text;

namespace Shesha.Web.FormsDesigner.Legacy
{
    [AttributeUsage(validOn: AttributeTargets.Class)]
    public class FormComponentPropsAttribute : Attribute
    {
        public bool IsInput { get; set; }
        public bool IsOutput { get; set; }
        public string ComponentType { get; set; }
        public bool BindChilds { get; set; }
        public string JavascriptLibrary { get; set; }

        public FormComponentPropsAttribute(string componentType)
        {
            ComponentType = componentType;
        }
    }
}
