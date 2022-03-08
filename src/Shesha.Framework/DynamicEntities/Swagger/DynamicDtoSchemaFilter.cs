using Microsoft.OpenApi.Models;
using Shesha.DynamicEntities.Dtos;
using Shesha.Reflection;
using Shesha.Services;
using Shesha.Utilities;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace Shesha.DynamicEntities.Swagger
{
    public class DynamicDtoSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            var isProxy = typeof(IDynamicDtoProxy).IsAssignableFrom(context.Type);

            if (!context.Type.IsDynamicDto() || isProxy)
                return;

            var dtoBuilder = StaticContext.IocManager.Resolve<IDynamicDtoTypeBuilder>();

            var builderContext = new DynamicDtoTypeBuildingContext
            {
                ModelType = context.Type
            };
            var dtoType = AsyncHelper.RunSync(async () => await dtoBuilder.BuildDtoFullProxyTypeAsync(builderContext.ModelType, builderContext));

            // build list of properties for case-insensitive search
            var propNames = schema.Properties.Select(p => p.Key.ToLower()).ToList();

            var dtoSchema = context.SchemaGenerator.GenerateSchema(dtoType, context.SchemaRepository);

            var allProperties = dtoType.GetProperties();
            foreach (var property in allProperties) 
            {
                if (propNames.Contains(property.Name.ToLower()))
                    continue;

                var propertySchema = context.SchemaGenerator.GenerateSchema(property.PropertyType, context.SchemaRepository, memberInfo: property);

                // note: Nullable is not processed by GenerateSchema
                propertySchema.Nullable = property.PropertyType.IsNullableType();

                schema.Properties.Add(property.Name.ToCamelCase(), propertySchema);
            }
        }
    }
}
