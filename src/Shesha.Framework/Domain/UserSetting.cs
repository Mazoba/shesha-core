using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities.Auditing;
using Shesha.Domain.Attributes;

namespace Shesha.Domain
{
    ///// <summary>
    ///// A user specific application setting.
    ///// </summary>
    //public class UserSetting : FullAuditedEntity<Guid>
    //{
    //    /// <summary>
    //    /// The username of the user the setting pertains to.
    //    /// </summary>
    //    [StringLength(100)]
    //    public virtual Guid UserId { get; set; }

    //    /// <summary>
    //    /// The namespace for the setting. Helps differentiate against other settings of the same name but imported from another module.
    //    /// </summary>
    //    [StringLength(100)]
    //    public virtual string SettingNamespace { get; set; }

    //    /// <summary>
    //    /// The name of the setting.
    //    /// </summary>
    //    [Required(AllowEmptyStrings = false), StringLength(255)]
    //    public virtual string SettingName { get; set; }

    //    /// <summary>
    //    /// The value of the setting.
    //    /// </summary>
    //    [Required]
    //    [StringLength(int.MaxValue)]
    //    public virtual string Value { get; set; }
    //}
}
