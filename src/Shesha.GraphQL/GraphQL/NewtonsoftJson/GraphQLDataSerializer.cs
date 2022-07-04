using Abp.Dependency;
using GraphQL;
using GraphQL.Execution;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shesha.GraphQL.NewtonsoftJson
{
    public class GraphQLDataSerializer : IGraphQLDataSerializer, ITransientDependency
    {
        private readonly JsonArrayPool _jsonArrayPool = new JsonArrayPool(ArrayPool<char>.Shared);
        private static readonly Encoding _utf8Encoding = new UTF8Encoding(false);

        public async Task WriteAsync(Stream stream, ExecutionResult executionResult, CancellationToken cancellationToken = default)
        {
            using var writer = new HttpResponseStreamWriter(stream, _utf8Encoding);
            using var jsonWriter = new JsonTextWriter(writer)
            {
                ArrayPool = _jsonArrayPool,
                CloseOutput = false,
                AutoCompleteOnClose = false
            };

            var errorInfoProvider = new ErrorInfoProvider();
            var serializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ContractResolver = new ShaGraphQLContractResolver(errorInfoProvider),
            };

            var serializer = JsonSerializer.CreateDefault(serializerSettings);

            serializer.Serialize(jsonWriter, executionResult);

            await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
