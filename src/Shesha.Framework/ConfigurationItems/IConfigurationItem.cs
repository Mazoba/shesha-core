using System.Threading.Tasks;

namespace Shesha.ConfigurationItems
{
    /// <summary>
    /// Interface of the configuration item
    /// </summary>
    public interface IConfigurationItem
    {
        /// <summary>
        /// Get dependencies of current configuration item
        /// </summary>
        /// <returns></returns>
        Task<IConfigurationItem> GetDependencies();
    }
}
