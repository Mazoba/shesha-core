using Shesha.Domain;

namespace Shesha.Web.DataTable
{
    /// <summary>
    /// Datatable helper
    /// </summary>
    public interface IDataTableHelper
    {
        /// <summary>
        /// Append quick search criteria
        /// </summary>
        /// <param name="tableConfig"></param>
        /// <param name="searchMode"></param>
        /// <param name="sSearch"></param>
        /// <param name="filterCriteria"></param>
        void AppendQuickSearchCriteria(DataTableConfig tableConfig, QuickSearchMode searchMode, string sSearch, FilterCriteria filterCriteria);
    }
}
