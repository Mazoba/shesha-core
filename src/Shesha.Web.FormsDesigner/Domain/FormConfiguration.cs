using Shesha.ConfigurationItems;
using Shesha.Domain;
using Shesha.Domain.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Shesha.Web.FormsDesigner.Domain
{
    /// <summary>
    /// Form configuration
    /// </summary>
    [Entity(TypeShortAlias = "Shesha.Core.FormConfiguration")]
    public class FormConfiguration: ConfigurationItemBase
    {
        /// <summary>
        /// Form markup
        /// </summary>
        [StringLength(int.MaxValue)]
        public virtual string Markup { get; set; }

        /// <summary>
        /// ModelType
        /// </summary>
        [StringLength(int.MaxValue)]
        public virtual string ModelType { get; set; }

        /// <summary>
        /// Type
        /// </summary>
        [StringLength(100)]
        public virtual string Type { get; set; }

        public override string ItemType => "form";

        public override Task<IConfigurationItem> GetDependencies()
        {
            throw new System.NotImplementedException();
        }
    }
}
