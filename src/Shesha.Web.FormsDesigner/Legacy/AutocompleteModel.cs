using System;
using System.Collections.Generic;
using System.Text;

namespace Shesha.Web.FormsDesigner.Legacy
{
    public class AutocompleteModel : ComponentModelBase
    {
        public AutocompleteDataSourceType? DataSourceType { get; set; }
        public string EntityTypeShortAlias { get; set; }
        public string DataSourceUrl { get; set; }
    }

    public enum AutocompleteDataSourceType : int
    {
        //public const int EntitiesList = 1;
        //public const int Url = 2;
        EntitiesList = 1,
        Url = 2
    }
}
