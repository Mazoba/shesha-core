using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Shesha.Web.FormsDesigner.Legacy
{
    public class SelectModel : ComponentModelBase
    {
        public SelectModel()
        {
            DataSourceType = SelectDataSourceType.Values;
        }

        public SelectDataSourceType? DataSourceType { get; set; }
        public string ReferenceListNamespace { get; set; }
        public string ReferenceListName { get; set; }
        public string DataSourceValues { get; set; }
        //public List<DataSourceValue> DataSourceValues { get; set; }
        public string EntityTypeShortAlias { get; set; }
        public string DataSourceUrl { get; set; }
    }

    public class DataSourceValue
    {
        [JsonProperty("label")]
        public string Label { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public enum SelectDataSourceType : int
    {
        Values = 1,
        ReferenceList = 2,
        EntitiesList = 3,
        Url = 4
    }
}
