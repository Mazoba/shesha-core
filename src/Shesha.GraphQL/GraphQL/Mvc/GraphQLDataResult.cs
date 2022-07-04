using GraphQL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Shesha.GraphQL.NewtonsoftJson;
using System.Net;
using System.Threading.Tasks;

namespace Shesha.GraphQL.Mvc
{
    /// <summary>
    /// GraphQL data result
    /// </summary>
    public class GraphQLDataResult : IActionResult
    {
        private readonly ExecutionResult _executionResult;

        public GraphQLDataResult(ExecutionResult executionResult)
        {
            _executionResult = executionResult;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            var serializer = context.HttpContext.RequestServices.GetRequiredService<IGraphQLDataSerializer>();
            var response = context.HttpContext.Response;
            response.ContentType = "application/json";
            response.StatusCode = _executionResult.Executed ? (int)HttpStatusCode.OK : (int)HttpStatusCode.BadRequest;
            await serializer.WriteAsync(response.Body, _executionResult, context.HttpContext.RequestAborted);
        }
    }
}
