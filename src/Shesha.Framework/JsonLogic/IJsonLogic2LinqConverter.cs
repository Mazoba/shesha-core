using Newtonsoft.Json.Linq;
using System;
using System.Linq.Expressions;
using System.Text.Json;

namespace Shesha.JsonLogic
{
    /// <summary>
    /// Json Logic to Linq converter
    /// </summary>
    public interface IJsonLogic2LinqConverter
    {
        /// <summary>
        /// Convert Json Logic to HQL
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        void Convert(JToken rule, JsonLogic2LinqConverterContext context);

        Expression<Func<T, bool>> ParseExpressionOf<T>(JObject rule);
        
        Func<T, bool> ParsePredicateOf<T>(JObject rule);
    }
}
