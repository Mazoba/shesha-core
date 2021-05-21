using System;
using System.Collections.Generic;
using System.Text;
using Shesha.Configuration.Runtime;

namespace Shesha.Web.FormsDesigner.Legacy
{
    public class ModelPropertyConfig
    {
        public bool IsVisible { get; set; }
        public bool Required { get; set; }
        public bool Readonly { get; set; }
        public bool ConfigurableByUser { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }

        public double? Min { get; set; }
        public double? Max { get; set; }

        public string Path { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }

        public bool IsEmail { get; set; }

        public GeneralDataType DataType { get; set; }
        public string EntityTypeShortAlias { get; set; }
        public Type EnumType { get; set; }
        public string ReferenceListName { get; set; }
        public string ReferenceListNamespace { get; set; }

        public int OrderIndex { get; set; }
        public string GroupName { get; set; }
    }
}
