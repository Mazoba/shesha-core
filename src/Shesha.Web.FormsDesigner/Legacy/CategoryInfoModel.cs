using System;
using System.Collections.Generic;
using System.Text;

namespace Shesha.Web.FormsDesigner.Legacy
{
    public class CategoryInfoModel
    {
        public string Category { get; set; }
        public int OrderIndex { get; set; }
        public List<ModelPropertyConfig> Properties { get; set; }
        public List<string> CustomViews { get; set; }

        public CategoryInfoModel()
        {
            Properties = new List<ModelPropertyConfig>();
            CustomViews = new List<string>();
        }
    }
}
