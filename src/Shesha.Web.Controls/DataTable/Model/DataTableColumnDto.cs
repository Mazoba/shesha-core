using System;
using System.ComponentModel;
using Abp.AutoMapper;

namespace Shesha.Web.DataTable.Model
{
    /// <summary>
    /// Datatable column DTO
    /// </summary>
    [AutoMapFrom(typeof(DataTableColumn))]
    public class DataTableColumnDto
    {
        /// <summary>
        /// Name of the property in the model
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Filter caption
        /// </summary>
        public string FilterCaption { get; set; }

        /// <summary>
        /// Column name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Caption
        /// </summary>
        public string Caption { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool AllowShowHide { get; set; }
        
        /// <summary>
        /// Data type
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// Custom data type
        /// </summary>
        public string CustomDataType { get; set; }

        /// <summary>
        /// Reference list name
        /// </summary>
        public string ReferenceListName { get; set; }

        /// <summary>
        /// Reference list namespace
        /// </summary>
        public string ReferenceListNamespace { get; set; }

        /// <summary>
        /// Entity type short alias
        /// </summary>
        public string EntityReferenceTypeShortAlias { get; set; }

        /// <summary>
        /// Autocomplete url
        /// </summary>
        public string AutocompleteUrl { get; set; }

        /// <summary>
        /// Allow selection of inherited entities, is used in pair with <seealso cref="AutocompleteUrl"/> 
        /// </summary>
        public bool AllowInherited { get; set; }

        /// <summary>
        /// Indicates is column visible or not
        /// </summary>
        public bool IsVisible { get; set; }

        /// <summary>
        /// Indicates is column filterable or not
        /// </summary>
        public bool IsFilterable { get; set; }

        /// <summary>
        /// Indicates is column sortable or not
        /// </summary>
        public bool IsSortable { get; set; }

        /// <summary>
        /// Column width
        /// </summary>
        public string Width { get; set; }

        /// <summary>
        /// Default sorting (asc/desc)
        /// </summary>
        public ListSortDirection? DefaultSorting { get; set; }

        /// <summary>
        /// Indicates is column hidden by default or not
        /// </summary>
        public bool IsHiddenByDefault { get; set; }

        #region backward compatibility 

        /// <summary>
        /// Indicates is column hidden by default or not
        /// </summary>
        [Obsolete("replaced with `IsHiddenByDefault`, this property will be removed in next version")]
        public bool HiddenByDefault => IsHiddenByDefault;

        /// <summary>
        /// Indicates is column visible or not
        /// </summary>
        [Obsolete("replaced with `IsVisible`, this property will be removed in next version")]
        public bool Visible => IsVisible;

        #endregion
    }
}
