using System;
using System.Collections.Generic;
using System.Text;

namespace Shesha.Web.FormsDesigner.Legacy
{
    public class ComponentModelBase
    {
        public ComponentModelBase()
        {
            LabelPosition = 1;
            LabelWidth = "20%";
        }

        public Guid? Id { get; set; }
        public Guid? ParentId { get; set; }
        public string Label { get; set; }
        public int LabelPosition { get; set; }
        public string LabelWidth { get; set; }
        public bool HideLabel { get; set; }
        public string Description { get; set; }
        public string ApiKey { get; set; }
        public string Placeholder { get; set; }
        public bool Hidden { get; set; }
        public bool Disabled { get; set; }
        public string InputMask { get; set; }
        public string Type { get; set; }
        public string InputType { get; set; }
        public string Prefix { get; set; }
        public string Suffix { get; set; }
        public bool IsInput { get; set; }
        public string DefaultValue { get; set; }
        public int SortIndex { get; set; }
        public List<Guid> ComponentIds { get; set; }
        public virtual string CssClass { get; set; }
        public int? TabIndex { get; set; }
        public object ContextData { get; set; }

        #region Layout

        public virtual string MarginTop { get; set; }
        public virtual string MarginRight { get; set; }
        public virtual string MarginBottom { get; set; }
        public virtual string MarginLeft { get; set; }

        #endregion

        #region Validation

        public virtual bool Required { get; set; }
        public virtual int? MinLength { get; set; }
        public virtual int? MaxLength { get; set; }
        public string CustomValidation { get; set; }
        public virtual bool ValidateImmediately { get; set; }

        #endregion

        public string CustomVisibility { get; set; }
        public string CustomInitialization { get; set; }

        public bool Persist { get; set; }

        public bool TrackVersions { get; set; }
    }
}
