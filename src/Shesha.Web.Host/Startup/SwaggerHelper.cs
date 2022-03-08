using Shesha.DynamicEntities;
using System;
using System.Linq;

namespace Shesha.Web.Host.Startup
{
    public static class SwaggerHelper
    {
        public static string GetSchemaId(Type modelType)
        {
            if (modelType.IsDynamicDto())
            {
                var test = modelType.IsConstructedGenericType
                    ? "DynamicDto" + modelType.GetGenericArguments().Select(genericArg => GetSchemaId(genericArg)).Aggregate((previous, current) => previous + current)
                    : "Proxy" + GetSchemaId(modelType.BaseType);
                return test;
            }

            if (!modelType.IsConstructedGenericType) return modelType.Name.Replace("[]", "Array");

            var prefix = modelType.GetGenericArguments()
                .Select(genericArg => GetSchemaId(genericArg))
                .Aggregate((previous, current) => previous + current);

            var result = prefix + modelType.Name.Split('`').First();

            return result;
        }
    }
}
