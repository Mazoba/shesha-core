using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Abp.Application.Services;
using Abp.Dependency;
using Abp.Reflection;
using Shesha.Permissions;
using Shesha.Reflection;

namespace Shesha.Permissions
{
    public class ApiPermissionedObjectProvider : PermissionedObjectProviderBase, IPermissionedObjectProvider
    {

        public ApiPermissionedObjectProvider(IAssemblyFinder assembleFinder) : base(assembleFinder)
        {
        }

        //private List<string> IgnoredMethods = new List<string>() { "ToString", "GetType", "Equals", "GetHashCode" };

        public const string ObjectCategory = "apiServices";

        public string GetCategory()
        {
            return ObjectCategory;
        }

        public string GetCategoryByType(Type type)
        {
            var shaServiceType = typeof(SheshaAppServiceBase);
            var crudServiceType = typeof(IAsyncCrudAppService<,,,,,,>);

            return type.IsPublic && !type.IsAbstract && shaServiceType.IsAssignableFrom(type)
                   && !type.GetInterfaces().Any(x =>
                       x.IsGenericType &&
                       x.GetGenericTypeDefinition() == crudServiceType)
                ? ObjectCategory
                : null;
        }

        public List<PermissionedObjectDto> GetAll()
        {
            var assemblies = _assembleFinder.GetAllAssemblies().Distinct(new AssemblyFullNameComparer()).Where(a => !a.IsDynamic).ToList();
            var allApiPermissions = new List<PermissionedObjectDto>();

            var shaServiceType = typeof(SheshaAppServiceBase);
            var crudServiceType = typeof(IAsyncCrudAppService<,,,,,,>);

            foreach (var assembly in assemblies)
            {
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
                        Category = ObjectCategory, 
                        Description = GetDescription(service)
                    };
                    allApiPermissions.Add(parent);

                    var methods = service.GetMethods(BindingFlags.Public | BindingFlags.Instance).ToList();
                    methods = methods.Where(x =>
                        x.IsPublic
                        && !x.IsAbstract
                        && !x.IsConstructor
                        && !x.IsSpecialName
                        && x.DeclaringType != typeof(object)
                        && x.DeclaringType != typeof(ApplicationService)
                    ).ToList();

                    foreach (var methodInfo in methods)
                    {
                        var child = new PermissionedObjectDto()
                        {
                            Object = service.FullName + "@" + methodInfo.Name,
                            //Action = methodInfo.Name, 
                            Category = ObjectCategory, 
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