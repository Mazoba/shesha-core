using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;
using Shesha.Domain;
using Shesha.Domain.Attributes;
using Shesha.Web.FormsDesigner.Domain;

namespace Shesha.Web.FormsDesigner.Legacy
{
    [NotMapped]
    public class FormComponent : FullAuditedEntity<Guid>
    {
        [Required]
        public virtual Form Form { get; set; }
        public virtual FormComponent Parent { get; set; }
        [StringLength(250)]
        public virtual string Label { get; set; }

        public virtual int LabelPosition { get; set; }

        [StringLength(250)]
        public virtual string LabelWidth { get; set; }
        public virtual bool HideLabel { get; set; }

        [StringLength(int.MaxValue)]
        [DataType(DataType.Html)]
        public virtual string Description { get; set; }
        [StringLength(250)]
        public virtual string ApiKey { get; set; }
        [StringLength(250)]
        public virtual string Placeholder { get; set; }
        public virtual bool Hidden { get; set; }
        public virtual bool Disabled { get; set; }
        [StringLength(250)]
        public virtual string InputMask { get; set; }
        [StringLength(250)]
        [Required]
        public virtual string Type { get; set; }
        [StringLength(250)]
        public virtual string InputType { get; set; }
        [StringLength(250)]
        public virtual string Prefix { get; set; }
        [StringLength(250)]
        public virtual string Suffix { get; set; }

        [StringLength(int.MaxValue)]
        public virtual string DefaultValue { get; set; }
        public virtual int SortIndex { get; set; }

        [StringLength(250)]
        public virtual string CssClass { get; set; }

        public virtual int? TabIndex { get; set; }

        #region Layout

        [StringLength(20)]
        public virtual string MarginTop { get; set; }
        [StringLength(20)]
        public virtual string MarginRight { get; set; }
        [StringLength(20)]
        public virtual string MarginBottom { get; set; }
        [StringLength(20)]
        public virtual string MarginLeft { get; set; }

        #endregion

        #region Validation

        public virtual bool Required { get; set; }
        public virtual int? MinLength { get; set; }
        public virtual int? MaxLength { get; set; }

        [StringLength(int.MaxValue)]
        public virtual string CustomValidation { get; set; }

        public virtual bool ValidateImmediately { get; set; }

        #endregion

        [StringLength(int.MaxValue)]
        public virtual string CustomVisibility { get; set; }

        [StringLength(int.MaxValue)]
        public virtual string CustomInitialization { get; set; }


        /// <summary>
        /// Custom settings in JSON format
        /// </summary>
        [StringLength(int.MaxValue)]
        public virtual string CustomSettings { get; set; }

        public virtual bool Persist { get; set; }

        public virtual bool TrackVersions { get; set; }

        public FormComponent()
        {
            LabelPosition = 1;
            LabelWidth = "20%";
            InputType = "text";
        }
    }
}
