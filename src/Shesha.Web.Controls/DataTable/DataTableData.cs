using System;
using System.Collections.Generic;

namespace Shesha.Web.DataTable
{
    /// <summary>
    /// Represents the data of the table used by DataTables  on the client-side
    /// </summary>
    public class DataTableData
    {
        /// <summary>
        /// Total number of rows after filters
        /// </summary>
        public long TotalRows { get; set; }

        /// <summary>
        /// Total number of rows before filters
        /// </summary>
        public long TotalRowsBeforeFilter { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Echo { get; set; }

        /// <summary>
        /// Data cells
        /// </summary>
        public List<Dictionary<string, object>> Rows { get; set; }
    }
}
