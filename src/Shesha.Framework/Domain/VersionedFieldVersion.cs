using System.ComponentModel.DataAnnotations;
using Shesha.Domain.Attributes;

namespace Shesha.Domain
{
    /// <summary>
    /// Version definition of a versioned field
    /// </summary>
    [Entity(GenerateApplicationService = false)]
    public class VersionedFieldVersion : FullPowerEntity
    {
        /// <summary>
        /// Field link
        /// </summary>
        public virtual VersionedField Field { get; set; }

        /// <summary>
        /// Value content
        /// </summary>
        [StringLength(int.MaxValue)]
        public virtual string Content { get; set; }

        //[NotMap]
        //public virtual string DisplayText
        //{
        //    get
        //    {
        //        var service = this.Service<PersonBase>();
        //        var createdBy = this.CreatedUser;
        //        try
        //        {
        //            var createdByShortName = service.GetAll().FirstOrDefault(p => p.Username == this.CreatedUser)?.ShortName;
        //            if (!string.IsNullOrEmpty(createdByShortName))
        //                createdBy = createdByShortName;
        //        }
        //        catch (Exception ex)
        //        {
        //            // In some client databases, where different discriminators are used, an exception is thrown. We silently log it and show username until this is resolved.
        //            ErrorLog.GetDefault(HttpContext.Current).Log(new Error(ex));
        //        }
        //        var result = createdBy +
        //                     (CreatedTimestamp.HasValue
        //                         ? ", " + (CreatedTimestamp.Value.Year == DateTime.Now.Year ? CreatedTimestamp.Value.ToString("dd MMM") : CreatedTimestamp.Value.ToString("dd MMM yy")) /* Show year only if the version of prev years  */
        //                         : "");
        //        return result;
        //    }
        //}

        /*
        public virtual VersionStatus? Status { get; set; }
        public virtual string ReviewedBy { get; set; }
        public virtual DateTime? ReviewedOn { get; set; }
         */
    }
}
