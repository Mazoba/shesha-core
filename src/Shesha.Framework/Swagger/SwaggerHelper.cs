using Abp.Application.Services;
using Abp.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Shesha.Services;
using Shesha.Utilities;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Shesha.Swagger
{
    public static class SwaggerHelper
    {
        /// <summary>
        /// Add separate Swagger documents for each Application Service and Controller
        /// Url format: `/swagger/service:{ApplicationService or controller name}/swagger.json`
        /// </summary>
        /// <param name="options"></param>
        public static void AddDocumentsPerService(this SwaggerGenOptions options)
        {
            var names = new List<string>();

            var types = GetRegisteredControllerTypes();

            // 1. add controllers
            var controllers = types.Where(t => typeof(ControllerBase).IsAssignableFrom(t)).ToList();
            foreach (var controller in controllers)
            {
                var serviceName = MvcHelper.GetControllerName(controller);
                names.Add(serviceName);
                options.SwaggerDoc($"service:{serviceName}", new OpenApiInfo() { Title = $"{serviceName} (ControllerBase)", Version = "v1" });
            }

            // 2. add application services
            var appServices = types.Where(t => typeof(IApplicationService).IsAssignableFrom(t)).ToList();
            foreach (var service in appServices)
            {
                var serviceName = MvcHelper.GetControllerName(service);
                names.Add(serviceName);
                options.SwaggerDoc($"service:{serviceName}", new OpenApiInfo() { Title = $"API {serviceName} (IApplicationService)", Version = "v1" });
            }
            options.DocInclusionPredicate((docName, description) => ApiExplorerGroupPerControllerConvention.GroupInclusionPredicate(docName, description));
        }

        private static IList<TypeInfo> GetRegisteredControllerTypes() 
        {
            var controllerFeature = new ControllerFeature();
            var applicationPartManager = StaticContext.IocManager.Resolve<ApplicationPartManager>();
            applicationPartManager.PopulateFeature(controllerFeature);
            return controllerFeature.Controllers;
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
