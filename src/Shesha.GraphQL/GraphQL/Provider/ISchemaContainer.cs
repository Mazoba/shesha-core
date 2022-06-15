using GraphQL.Types;
using System.Threading.Tasks;

namespace Shesha.GraphQL.Provider
{
    public interface ISchemaContainer
    {
        Task<ISchema> GetOrDefaultAsync(string schemaName, string defaultSchemaName = null);
    }
}
