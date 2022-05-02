using Abp.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.Metadata
{
    public class DtoHelper: IDtoHelper
    {
        private readonly IAssemblyFinder _assembleFinder;

        public DtoHelper(IAssemblyFinder assembleFinder)
        {
            _assembleFinder = assembleFinder;
        }

        private List<string> GetDtos()
        {
            var assemblies = _assembleFinder.GetAllAssemblies().Distinct(new AssemblyFullNameComparer()).Where(a => !a.IsDynamic).ToList();
            var allApiPermissions = new List<PermissionedObjectDto>();

            var shaServiceType = typeof(SheshaAppServiceBase);
            var crudServiceType = typeof(IAsyncCrudAppService<,,,,,,>);

            foreach (var assembly in assemblies)
            {
                var module = assembly.GetTypes().FirstOrDefault(t =>
                    t.IsPublic && !t.IsAbstract && typeof(AbpModule).IsAssignableFrom(t));

                var services = assembly.GetTypes()
                    .Where(t => t.IsPublic && !t.IsAbstract && shaServiceType.IsAssignableFrom(t)
                                && !t.GetInterfaces().Any(x =>
                                   x.IsGenericType &&
                                   x.GetGenericTypeDefinition() == crudServiceType))
                    .ToList();
                foreach (var service in services)
                {
                    var parent = new PermissionedObjectDto()
                    {
                        Object = service.FullName,
                        Module = module?.FullName ?? "",
                        Name = GetName(service),
                        Type = PermissionedObjectsSheshaTypes.WebApi,
                        Description = GetDescription(service)
                    };
                    allApiPermissions.Add(parent);

                    var methods = service.GetMethods(BindingFlags.Public | BindingFlags.Instance).ToList();
                    methods = methods.Where(x =>
                        x.IsPublic
                        && !x.IsAbstract
                        && !x.IsConstructor
                        && !x.IsSpecialName
                        && !x.IsGenericMethod
                        && x.DeclaringType != typeof(object)
                        && x.DeclaringType != typeof(ApplicationService)
                    ).ToList();

                    foreach (var methodInfo in methods)
                    {
                        var child = new PermissionedObjectDto()
                        {
                            Object = service.FullName + "@" + methodInfo.Name,
                            //Action = methodInfo.Name, 
                            Module = module?.FullName ?? "",
                            Name = GetName(methodInfo),
                            Type = PermissionedObjectsSheshaTypes.WebApiAction,
                            Parent = service.FullName,
                            Description = GetDescription(methodInfo)
                        };
                        //parent.Child.Add(child);
                        allApiPermissions.Add(child);
                    }
                }
            }

            return allApiPermissions;

        }

    }
}
