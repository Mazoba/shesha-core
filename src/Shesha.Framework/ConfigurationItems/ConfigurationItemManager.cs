using System.IO;
using System.Threading.Tasks;

namespace Shesha.ConfigurationItems
{
    /// <summary>
    /// Base class of the Configuration Item Manager
    /// </summary>
    public abstract class ConfigurationItemManager<TItem> : IConfigurationItemManager<TItem> where TItem: IConfigurationItem
    {
        public virtual Task<string> ExportFileAsync()
        {
            throw new System.NotImplementedException();
        }

        public abstract Task<string> ExportItemAsync(TItem item);

        public virtual Task ImportFileAsync(Stream contentStream)
        {
            throw new System.NotImplementedException();
        }

        public abstract Task ImportItemAsync(string content);        
    }
}
