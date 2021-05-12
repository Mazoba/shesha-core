using System.Collections.Generic;

namespace Shesha.Web.DataTable
{
    /// <summary>
    /// Data table configuration store interface
    /// </summary>
    public interface IDataTableConfigurationStore
    {
        /// <summary>
        /// Get a data table configuration from the store. Please keep in mind that some parts of the configuration are user specific
        /// so we have to either cache per person or update it for current user after getting another user's configuration from cache.
        /// </summary>
        DataTableConfig GetTableConfiguration(string id, bool throwNotFoundException = true);

        /// <summary>
        /// Get identifiers of all registered table configurations
        /// </summary>
        /// <returns></returns>
        List<string> GetTableIds();
    }
}
