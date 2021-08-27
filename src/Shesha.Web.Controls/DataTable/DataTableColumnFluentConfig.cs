using System;
using System.ComponentModel;
using Shesha.Web.DataTable;

namespace Shesha.Web.DataTable
{
    /// <summary>
    /// Datatable fluent configuration
    /// </summary>
    public class DataTableColumnFluentConfig
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="column"></param>
        public DataTableColumnFluentConfig(DataTableColumn column)
        {
            Column = column;
        }

        /// <summary>
        /// Column reference
        /// </summary>
        public DataTableColumn Column { get; protected set; }

        /// <summary>
        /// Set column visibility
        /// </summary>
        public DataTableColumnFluentConfig Visible(bool visible)
        {
            Column.IsVisible = visible;
            return this;
        }

        /// <summary>
        /// Set column editability
        /// </summary>
        public DataTableColumnFluentConfig Editable(bool editable)
        {
            Column.IsEditable = editable;
            return this;
        }

        /// <summary>
        /// Enable/disable filtration
        /// </summary>
        public DataTableColumnFluentConfig IsFilterable(bool isFilterable)
        {
            Column.IsFilterable = isFilterable;
            return this;
        }

        /// <summary>
        /// Enable/disable export to excel
        /// </summary>
        public DataTableColumnFluentConfig Exportable(bool allowExport = true)
        {
            Column.IsExportable = allowExport;
            return this;
        }

        /// <summary>
        /// Enable/disable html stripping
        /// </summary>
        public DataTableColumnFluentConfig StripHtml(bool stripHtml = true)
        {
            Column.StripHtml = stripHtml;
            return this;
        }

        /// <summary>
        /// Allow/disallow sorting
        /// </summary>
        public DataTableColumnFluentConfig Sortable(bool sortable)
        {
            Column.IsSortable = sortable;
            return this;
        }

        /// <summary>
        /// Set caption of the column
        /// </summary>
        public DataTableColumnFluentConfig Caption(string caption)
        {
            Column.Caption = caption;
            return this;
        }

        /// <summary>
        /// Set autocomplete url (applicable to entity reference columns)
        /// </summary>
        public DataTableColumnFluentConfig AutocompleteUrl(string url)
        {
            Column.AutocompleteUrl = url;
            return this;
        }

        public DataTableColumnFluentConfig AllowShowHide(bool allowShowHide)
        {
            Column.AllowShowHide = allowShowHide;
            return this;
        }

        public DataTableColumnFluentConfig WidthPixels(int width)
        {
            Column.Width = $"{width}px";
            return this;
        }
        public DataTableColumnFluentConfig Resizable(bool resizable)
        {
            Column.IsResizable = resizable;
            return this;
        }

        /*
        public DataTableColumnFluentConfig FilterControl(FilterColumnControl? control = null, string filterControlPartial = null, ColumnFilterModel filterControlModel = null, string captionOverride = null, string propertyNameOverride = null)
        {

            if (control.HasValue)
                Column.FilterControlToUse = control.Value;
            if (filterControlPartial != null)
                Column.FilterControlPartial = filterControlPartial;
            if (filterControlModel != null)
                Column.FilterControlModel = filterControlModel;
            if (captionOverride != null)
                Column.FilterCaption = captionOverride;
            if (propertyNameOverride != null)
                Column.FilterPropertyName = propertyNameOverride;
            return this;
        }
        */
        public DataTableColumnFluentConfig SortAscending()
        {
            Column.DefaultSorting = ListSortDirection.Ascending;
            return this;
        }

        public DataTableColumnFluentConfig SortDescending()
        {
            Column.DefaultSorting = ListSortDirection.Descending;
            return this;
        }

        public DataTableColumnFluentConfig HiddenByDefault(bool hidden = true)
        {
            Column.IsHiddenByDefault = hidden;
            return this;
        }

        public DataTableColumnFluentConfig Authorization(Func<bool> isAuthorized)
        {
            Column.IsAuthorized = isAuthorized;
            return this;
        }
    }
}
