using System.ComponentModel.DataAnnotations;

namespace Shesha.Domain.ConfigurationItems
{
    /// <summary>
    /// Module
    /// </summary>
    public class Module: FullPowerEntity
    {
        /// <summary>
        /// Module name
        /// </summary>
        [StringLength(200)]
        public virtual string Name { get; set; }

        /// <summary>
        /// Module description
        /// </summary>
        [StringLength(int.MaxValue)]
        public virtual string Description { get; set; }

        /// <summary>
        /// If true, indicates that the module is enabled
        /// </summary>
        public virtual bool IsEnabled { get; set; }
    }
}
