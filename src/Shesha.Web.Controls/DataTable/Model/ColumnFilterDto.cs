using Newtonsoft.Json;
using System;

namespace Shesha.Web.DataTable.Model
{
    /// <summary>
    /// Columns filter DTO
    /// </summary>
    public class ColumnFilterDto
    {
        /// <summary>
        /// Column identifier
        /// </summary>
        [Obsolete("Use `PropertyName` instead, this property will be removed later")]
        public string ColumnId { get; set; }

        /// <summary>
        /// Property name. Supports dot notation
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Filter options
        /// </summary>
        public string FilterOption { get; set; }
        
        /// <summary>
        /// Filter value
        /// </summary>
        public object Filter { get; set; } // string, number, date, date[], number[]

        [JsonIgnore]
        public string RealPropertyName => !string.IsNullOrWhiteSpace(PropertyName) ? PropertyName : ColumnId;
    }
}