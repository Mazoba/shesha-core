using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Shesha.Web.FormsDesigner.Legacy
{
    /// <summary>
    /// Configuration of the PropertyGrid
    /// </summary>
    public class PropertyGridConfig
    {
        public PropertyGridConfig(IPropertyGridDataProvider dataProvider)
        {
            ColCount = 1;
            HiddenCategories = new List<string>();
            HiddenProperties = new List<string>();
            CustomEditors = new List<CustomEditorModel>();
            HideInherited = false;
            DataProvider = dataProvider;
        }

        /// <summary>
        /// Type of model, is usefull when the propertygrid is rendered for interface
        /// </summary>
        public Type ModelType { get; set; }

        public IPropertyGridDataProvider DataProvider { get; set; }

        /// <summary>
        /// If true indicates that grid is in readonly mode
        /// </summary>
        public bool Readonly { get; set; }

        /// <summary>
        /// If true indicates that only properties marked by the "Category" attribute should be visible on the grid
        /// </summary>
        public bool ShowOnlyCategorized { get; set; }

        /// <summary>
        /// Default name of the category, is used only when <cref name="ShowOnlyCategorized"/> = true
        /// </summary>
        public string DefaultCategoryName { get; set; }

        /// <summary>
        /// List of categories which should be hidden on the PropertyGrid
        /// </summary>
        public List<string> HiddenCategories { get; set; }

        /// <summary>
        /// If true, all properties will be rendered without panels
        /// </summary>
        public bool HidePanels { get; set; }

        /// <summary>
        /// If true indicates that Knockout bindings should be generated
        /// </summary>
        public bool InsertBindings { get; set; }

        /// <summary>
        /// List of properties which should be hidden on the PropertyGrid
        /// </summary>
        public List<string> HiddenProperties { get; set; }

        /// <summary>
        /// Function that is used for sorting of the categories
        /// </summary>
        public Func<string, int> CategoriesOrderFunc { get; set; }

        /// <summary>
        /// List of custom editors which should be embedded to the PropertyGrid
        /// </summary>
        public List<CustomEditorModel> CustomEditors { get; set; }

        /// <summary>
        /// Number of columns
        /// </summary>
        public int ColCount { get; set; }

        /// <summary>
        /// If true, inherited properties will not be shown on the grid
        /// </summary>
        public bool HideInherited { get; set; }

        /// <summary>
        /// Manages the visibility of categoiries
        /// </summary>
        public Func<string, bool> CategoriesVisibility { get; set; }

        /// <summary>
        /// Manages the visibility of properties
        /// </summary>
        public Func<PropertyInfo, bool> PropertiesVisibility { get; set; }
    }

}
