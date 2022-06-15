using Abp.Dependency;
using Abp.Extensions;
using Abp.Reflection;
using GraphQL.Types;
using Shesha.Extensions;
using Shesha.GraphQL.Provider.Schemas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shesha.GraphQL.Provider
{
    public class SchemaContainer : ISchemaContainer, ISingletonDependency
    {
        protected ISchema DefaultSchema { get; set; }

        protected Dictionary<string, ISchema> Schemas { get; set; }

        private readonly ITypeFinder _typeFinder;

        public SchemaContainer(
            //IOptions<AbpGraphQLOptions> options,
            IServiceProvider serviceProvider,
            IEnumerable<ISchema> customSchemas,
            ITypeFinder typeFinder)
        {
            DefaultSchema = new Schema();
            DefaultSchema.Query = new EmptyQuery();
            _typeFinder = typeFinder;

            Schemas = customSchemas.ToDictionary(
                keySelector: schema => schema.GetType().Name.RemovePostFix("Schema"),
                elementSelector: schema => schema);

            // find all entities and register schemas and queries
            var entityTypes = _typeFinder.Find(t => t.IsEntityType()).ToList();
            foreach (var entityType in entityTypes) 
            {
                var idType = entityType.GetEntityIdType();
                var schemaType = typeof(EntitySchema<,>).MakeGenericType(entityType, idType);

                var schema = Activator.CreateInstance(schemaType, serviceProvider);

                var schemaName = entityType.Name.ToCamelCase();
                Schemas.Add(schemaName, (ISchema)schema);
            }

            /*
            foreach (var configuration in options.Value.AppServiceSchemes.GetConfigurations())
            {
                var schemaType = typeof(AppServiceSchema<,,,,>).MakeGenericType(configuration.AppServiceInterfaceType,
                    configuration.GetOutputDtoType, configuration.GetListOutputDtoType, configuration.KeyType,
                    configuration.GetListInputType);

                var schema = Activator.CreateInstance(schemaType, serviceProvider);

                Schemas.Add(configuration.SchemaName, (ISchema)schema);
            }
            */
        }

        public virtual Task<ISchema> GetOrDefaultAsync(string schemaName, string specifiedDefaultSchemaName = null)
        {
            return Task.FromResult(!schemaName.IsNullOrEmpty() && Schemas.ContainsKey(schemaName)
                ? Schemas[schemaName]
                : specifiedDefaultSchemaName != null ? Schemas[specifiedDefaultSchemaName] : DefaultSchema);
        }
    }
}
