using Shesha.ConfigurationItems;
using Shesha.Web.FormsDesigner.Domain;
using System.IO;
using System.Threading.Tasks;

namespace Shesha.Web.FormsDesigner.Services
{
    /// <summary>
    /// Form configuration manager
    /// </summary>
    public class FormConfigurationManager : ConfigurationItemManager<FormConfiguration>
    {
        public override Task<string> ExportItemAsync(FormConfiguration item)
        {
            throw new System.NotImplementedException();
        }

        public override Task ImportItemAsync(string content)
        {
            throw new System.NotImplementedException();
        }
    }
}
