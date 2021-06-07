using Shesha.Domain.Attributes;

namespace Shesha.Web.FormsDesigner.Domain
{
    /// <summary>
    /// Form
    /// </summary>
    [Entity(TypeShortAlias = "Shesha.Framework.Form")]
    [DiscriminatorValue("Shesha.Framework.Form")]
    public class Form : ConfigurableComponent
    {
    }
}
