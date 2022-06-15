using Abp.Domain.Entities;
using Shesha.Domain;
using Shesha.Web.DataTable.Columns;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        /// <summary>
        /// Append quick search criteria
        /// </summary>
        void AppendQuickSearchCriteria(Type rowType, List<DataTableColumn> columns, QuickSearchMode searchMode, string sSearch, FilterCriteria filterCriteria, Action<FilterCriteria, string> onRequestToQuickSearch, string cacheKey);

        /// <summary>
        /// Get column properties by type of model and property name
        /// </summary>
        /// <param name="rowType">Type of model (table row)</param>
        /// <param name="propName">Name of property. Supports nested properties with dot notation</param>
        /// <param name="name">Name of the column, leave empty to fill with default name</param>
        /// <returns></returns>
        [Obsolete]
        DataTablesDisplayPropertyColumn GetDisplayPropertyColumn(Type rowType, string propName, string name = null);

        /// <summary>
        /// Get column properties by type of model and property name
        /// </summary>
        /// <param name="rowType">Type of model (table row)</param>
        /// <param name="propName">Name of property. Supports nested properties with dot notation</param>
        /// <param name="name">Name of the column, leave empty to fill with default name</param>
        /// <returns></returns>
        Task<DataTablesDisplayPropertyColumn> GetDisplayPropertyColumnAsync(Type rowType, string propName, string name = null);        
    }
}
