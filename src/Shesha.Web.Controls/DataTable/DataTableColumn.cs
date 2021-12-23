using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Shesha.Configuration.Runtime;

namespace Shesha.Web.DataTable
{
    /// <summary>
    /// Data table column
    /// </summary>
    public abstract class DataTableColumn
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        protected internal DataTableColumn()
        {
            Fluent = new DataTableColumnFluentConfig(this);
            // default values
            IsVisible = true;
            IsSortable = true;
            Searchable = false;
            IsExportable = true;
            IsFilterable = true;
            Width = "";
            IsResizable = true;
            StripHtml = false;
            AllowShowHide = true;
            IsEditable = true;
        }

        /// <summary>
        /// Datatable configuration
        /// </summary>
        public DataTableConfig DataTableConfig { get; set; }

        /// <summary>
        /// Name of the property in the model
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Caption of the filter control (this field overrides default value Caption for the filter)
        /// </summary>
        public string FilterCaption { get; set; }

        /// <summary>
        /// Caption of the filter control (this field overrides default value Caption for the filter)
        /// </summary>
        public string FilterPropertyName { get; set; }

        /// <summary>
        /// Column name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Caption of the column
        /// </summary>
        public string Caption { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// If true, indicates that the column is visible
        /// </summary>
        public bool IsVisible { get; set; }

        /// <summary>
        /// If true, indicates that the column is sortable
        /// </summary>
        public bool IsSortable { get; set; }

        /// <summary>
        /// Default sorting direction (asc/desc)
        /// </summary>
        public ListSortDirection? DefaultSorting { get; set; }

        /// <summary>
        /// Width
        /// </summary>
        public string Width { get; set; }

        /// <summary>
        /// If true, indicates that the column is resizeable
        /// </summary>
        public bool IsResizable { get; set; }

        /// <summary>
        /// If true, indicates that the column is hidden by default, but the user is able to change visibility
        /// </summary>
        public bool IsHiddenByDefault { get; set; }

        /// <summary>
        /// If true, html tags will be stripped automatically
        /// </summary>
        public bool StripHtml { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        public bool AllowShowHide { get; set; }

        #region Reference list

        /// <summary>
        /// Reference list name
        /// </summary>
        public string ReferenceListName { get; set; }

        /// <summary>
        /// Reference list namespace
        /// </summary>
        public string ReferenceListNamespace { get; set; }

        #endregion

        #region Entity reference

        /// <summary>
        /// Type short alias of the referenced entity
        /// </summary>
        public string EntityReferenceTypeShortAlias { get; set; }

        /// <summary>
        /// Autocomplete url. Is used for column filter and inline editing
        /// </summary>
        public string AutocompleteUrl { get; set; }

        /// <summary>
        /// Allow selection of inherited entities, is used in pair with <seealso cref="AutocompleteUrl"/> 
        /// </summary>
        public bool AllowInherited { get; set; }

        #endregion

        /// <summary>
        /// Data type of the column
        /// </summary>
        [Obsolete]
        public string DataType =>
            GeneralDataType != null
                ? DataTableHelper.GeneralDataType2ColumnDataType(GeneralDataType.Value)
                : null;

        /// <summary>
        /// General data type
        /// </summary>
        [Obsolete]
        public GeneralDataType? GeneralDataType { get; set; }

        /// <summary>
        /// Custom data type
        /// </summary>
        [Obsolete("Will be replaced with StandardDataType")]
        public string CustomDataType { get; set; }

        public string StandardDataType { get; set; }
        public string DataFormat { get; set; }

        /// <summary>
        /// Fluent configuration
        /// </summary>
        [JsonIgnore]
        public DataTableColumnFluentConfig Fluent { get; set; }

        /// <summary>
        /// Returns cell content
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public abstract object CellContent(object entity);

        #region to be implemented

        /// <summary>
        /// If true, indicates that column is exportable to excel
        /// </summary>
        public bool IsExportable { get; set; }

        /// <summary>
        /// If true, indicates that column is editable
        /// </summary>
        public bool IsEditable { get; set; }

        /// <summary>
        /// If true, indicates that column is filterable
        /// </summary>
        public bool IsFilterable { get; set; }

        #endregion

        #region not implemented

        /// <summary>
        /// Not implemented, don't use it
        /// </summary>
        public bool Searchable { get; set; }

        #endregion

        /// <summary>
        /// Function, returns true if the user is authorized to view this column
        /// </summary>
        public Func<bool> IsAuthorized { get; set; }
    }
}