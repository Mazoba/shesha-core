using System;
using Shesha.Web.DataTable.Model;
using System.Collections.Generic;

namespace Shesha.Web.DataTable
{
    /// <summary>
    /// Data table --> GetData: input parameters are passed from client to server.
    /// </summary>
    public class DataTableGetDataInput
    {
        /// <summary>
        /// Data table ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Page size
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Quick search textbox contents
        /// </summary>
        public string QuickSearch { get; set; }

        /// <summary>
        /// Current page number
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// Parent entity ID (only for ChildDataTable)
        /// </summary>
        public string ParentEntityId { get; set; }

        /// <summary>
        /// Sorting settings per column
        /// </summary>
        public List<ColumnSortingDto> Sorting { get; set; } = new List<ColumnSortingDto>();

        /// <summary>
        /// Advanced Filtering settings per column
        /// </summary>
        public List<ColumnFilterDto> Filter { get; set; } = new List<ColumnFilterDto>();

        /// <summary>
        /// Stored Filters IDs that user has selected and that must be applied
        /// </summary>
        [Obsolete("Use SelectedFilters instead")]
        public List<string> SelectedStoredFilterIds { get; set; } = new List<string>();

        /// <summary>
        /// Selected filters
        /// </summary>
        public List<SelectedStoredFilterDto> SelectedFilters { get; set; } = new List<SelectedStoredFilterDto>();

        /*
         1) sort order: list of columns with sort order asc/desc
         2) list of displayed columns
         3) applied filters (saved or unsaved ones)
         4) applied quick filters
         */
    }
}
