using Abp;
using Abp.Domain.Entities;
using Shesha.ConfigurationItems;
using Shesha.Domain.ConfigurationItems;
using Shesha.Services;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace Shesha.Domain
{
    /// <summary>
    /// Configuration item base
    /// </summary>
    public abstract class ConfigurationItemBase: Entity<Guid>, IConfigurationItem
    {
        /// <summary>
        /// Configuration item base info
        /// </summary>
        [ForeignKey("Id")]
        public virtual ConfigurationItem Configuration { get; set; }

        public ConfigurationItemBase(string itemType)
        {
            /*
            var guidGenerator = StaticContext.IocManager.Resolve<IGuidGenerator>();

            Id = guidGenerator.Create();
            Configuration = new ConfigurationItem { 
                Id = this.Id,
                ItemType = itemType
            };
            */
        }

        public abstract Task<IConfigurationItem> GetDependencies();        
    }
}
