using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;
using Shesha.Domain.Attributes;

namespace Shesha.Domain
{
    //public class ApplicationSetting : FullAuditedEntity<Guid>
    //{
    //    /// <summary>
    //    /// The namespace for the setting. Helps differentiate against other settings of the same name but imported from another module.
    //    /// </summary>
    //    [StringLength(100)]
    //    public virtual string SettingNamespace { get; set; }

    //    /// <summary>
    //    /// The name of the setting.
    //    /// </summary>
    //    [Required(AllowEmptyStrings = false), StringLength(100)]
    //    public virtual string SettingName { get; set; }

    //    /// <summary>
    //    /// A description for the setting, it''s usage and options.
    //    /// </summary>
    //    [StringLength(500)]
    //    public virtual string Description { get; set; }

    //    /// <summary>
    //    /// Only applies if SettingType is List. The namespace of the List to use for the selection.
    //    /// </summary>
    //    [StringLength(100)]
    //    public virtual string ListNamespace { get; set; }

    //    /// <summary>
    //    /// Only applies if SettingType is List. The name of the List to use for the selection.
    //    /// </summary>
    //    [StringLength(100)]
    //    public virtual string ListName { get; set; }

    //    /// <summary>
    //    /// The value of the setting.
    //    /// </summary>
    //    public virtual string Value { get; set; }

    //    /// <summary>
    //    /// The type of value of the setting e.g. Text, Date, Number, List, File, etc...
    //    /// </summary>
    //    [ReferenceList("Shesha.Framework", "AppSettingType")]
    //    public virtual int SettingType { get; set; }

    //    [EntityDisplayName]
    //    [NotMapped]
    //    public virtual string FullName => $"{SettingNamespace}.{SettingName}".Trim();
    //}
}
