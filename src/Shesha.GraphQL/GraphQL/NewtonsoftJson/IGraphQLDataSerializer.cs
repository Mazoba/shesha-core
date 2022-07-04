using GraphQL;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Shesha.GraphQL.NewtonsoftJson
{
    public interface IGraphQLDataSerializer
    {
        /// <summary>
        /// Asynchronously serializes the specified object to the specified stream.
        /// Typically used to write <see cref="ExecutionResult"/> instances to a JSON result.
        /// </summary>
        Task WriteAsync(Stream stream, ExecutionResult executionResult, CancellationToken cancellationToken = default);
    }
}
