using System;
using System.Collections.Generic;
using System.Text;

namespace Shesha.Web.FormsDesigner.Legacy
{
    public class FormModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ModelType { get; set; }
        public List<ComponentModelBase> Components { get; set; }

        public FormModel()
        {
            Components = new List<ComponentModelBase>();
        }
    }
}
