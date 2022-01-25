using System;
using System.Collections.Generic;
using System.Text;

namespace Shesha.Metadata
{
    /// <summary>
    /// Data type and format info
    /// </summary>
    public class DataTypeInfo
    {
        /// <summary>
        /// Data Type
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// Data format
        /// </summary>
        public string DataFormat { get; set; }

        public DataTypeInfo(string dataType, string dataFormat = null)
        {
            DataType = dataType;
            DataFormat = dataFormat;
        }
    }
}
