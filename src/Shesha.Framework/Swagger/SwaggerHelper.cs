using System.Collections.Generic;
using System.IO;
using Abp.Application.Services;
using Abp.Dependency;
using Abp.Extensions;
using Abp.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Shesha.Services;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Shesha.Swagger
{
    public static class SwaggerHelper
    {
        public static void AddDocumentsPerService(this SwaggerGenOptions options)
        {
            var typeFinder = StaticContext.IocManager.Resolve<ITypeFinder>();

            var names = new List<string>();

            // 1. add controllers
            var controllers = typeFinder.Find(c => !c.IsAbstract && typeof(ControllerBase).IsAssignableFrom(c));
            foreach (var controller in controllers)
            {
                var serviceName = controller.Name.RemovePostFix("Controller");
                names.Add(serviceName);
                options.SwaggerDoc($"service:{serviceName}", new OpenApiInfo() { Title = $"{serviceName} (ControllerBase)", Version = "v1" });
            }

            // 2. add application services
            var appServices = typeFinder.Find(c => !c.IsAbstract && typeof(IApplicationService).IsAssignableFrom(c));
            foreach (var service in appServices)
            {
                var serviceName = service.Name.RemovePostFix("AppService");
                names.Add(serviceName);
                options.SwaggerDoc($"service:{serviceName}", new OpenApiInfo() { Title = $"API {serviceName} (IApplicationService)", Version = "v1" });
            }
            options.DocInclusionPredicate((docName, description) => ApiExplorerGroupPerControllerConvention.GroupInclusionPredicate(docName, description));
        }

        public static void AddXmlDocuments(this SwaggerGenOptions options)
        {
            var assemblyFinder = StaticContext.IocManager.Resolve<IAssemblyFinder>();

            var assemblies = assemblyFinder.GetAllAssemblies();

            foreach (var assembly in assemblies)
            {
                var xmlPath = Path.ChangeExtension(assembly.Location, ".xml");
                if (File.Exists(xmlPath))
                    options.IncludeXmlComments(xmlPath, true);
            }
        }
    }

}
