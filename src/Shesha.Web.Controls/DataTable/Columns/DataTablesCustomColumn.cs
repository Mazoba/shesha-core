﻿using System;
using System.Threading.Tasks;

namespace Shesha.Web.DataTable.Columns
{
    /// <summary>
    /// Custom column
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataTablesCustomColumn<T> : DataTableColumn where T: class
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="contentFunc"></param>
        public DataTablesCustomColumn(Func<T, string> contentFunc) : base()
        {
            ContentFunc = contentFunc;
            IsSortable = false;
            Fluent.WidthPixels(20);
            IsExportable = false;
        }

        /// <summary>
        /// Content func
        /// </summary>
        protected Func<T, string> ContentFunc;

        /// inheritedDoc
        public override async Task<object> CellContentAsync<TRow, TId>(TRow entity, bool isExport)
        {
            return ContentFunc?.Invoke(entity as T);
        }
    }
}
