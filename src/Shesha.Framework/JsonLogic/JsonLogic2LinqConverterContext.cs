using Shesha.Metadata;
using System.Collections.Generic;

namespace Shesha.JsonLogic
{
    public class JsonLogic2LinqConverterContext
    {
        /// <summary>
        /// Query parameters prefix
        /// </summary>
        public string ParametersPrefix { get; set; } = "par";

        /// <summary>
        /// List of query parameters
        /// </summary>
        public Dictionary<string, object> FilterParameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Variable resolvers
        /// </summary>
        public Dictionary<string, string> VariablesResolvers { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Fields metadata dictionary
        /// </summary>
        public Dictionary<string, IPropertyMetadata> FieldsMetadata { get; set; } = new Dictionary<string, IPropertyMetadata>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parametersPrefix"></param>
        public JsonLogic2LinqConverterContext(string parametersPrefix)
        {
            ParametersPrefix = parametersPrefix;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public JsonLogic2LinqConverterContext()
        {

        }

        /// <summary>
        /// Add new parameter and get it's name
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string AddParameter(object value)
        {
            var name = $"{ParametersPrefix}{FilterParameters.Count + 1}";
            FilterParameters.Add(name, value);

            return name;
        }
    }
}
