using System.IO;
using System.Threading.Tasks;

namespace Shesha.ConfigurationItems
{
    /// <summary>
    /// Interface of the Configuration Item Manager
    /// </summary>
    public interface IConfigurationItemManager<TItem> where TItem : IConfigurationItem
    {
        /// <summary>
        /// Import all items from JSON file
        /// </summary>
        /// <param name="contentStream"></param>
        /// <returns></returns>
        Task ImportFileAsync(Stream contentStream);

        /// <summary>
        /// Import configuration item in JSON format
        /// </summary>
        /// <returns></returns>
        Task ImportItemAsync(string content);

        /// <summary>
        /// Export configuration item in JSON format
        /// </summary>
        /// <returns></returns>
        Task<string> ExportItemAsync(TItem item);

        /// <summary>
        /// Export all items to JSON
        /// </summary>
        /// <returns></returns>
        Task<string> ExportFileAsync();
    }
}
