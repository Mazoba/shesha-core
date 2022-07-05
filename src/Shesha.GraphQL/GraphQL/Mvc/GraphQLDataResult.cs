﻿using GraphQL;
using GraphQL.Execution;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Shesha.GraphQL.NewtonsoftJson;

namespace Shesha.GraphQL.Mvc
{
    /// <summary>
    /// GraphQL data result
    /// </summary>
    public class GraphQLDataResult : JsonResult
    {
        public GraphQLDataResult(ExecutionResult executionResult): base(executionResult)
        {
            var errorInfoProvider = new ErrorInfoProvider();
            SerializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ContractResolver = new ShaGraphQLContractResolver(errorInfoProvider) {
                    NamingStrategy = new CamelCaseNamingStrategy
                    {
                        ProcessDictionaryKeys = true,
                        OverrideSpecifiedNames = true
                    }
                },
            };
        }
    }
}
