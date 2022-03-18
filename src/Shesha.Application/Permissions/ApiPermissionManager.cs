using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Abp.Application.Services;
using Abp.Dependency;
using Abp.Reflection;
using Shesha.Domain;
using Shesha.Reflection;

namespace Shesha.Permissions
{
    public class ApiPermissionManager : ISingletonDependency
    {
        // ToDo: AS - temporary
        private readonly IAssemblyFinder _assembleFinder;

        public ApiPermissionManager(IAssemblyFinder assembleFinder)
        {
            _assembleFinder = assembleFinder;
        }

        private List<string> IgnoredMethods = new List<string>() {"ToString", "GetType", "Equals", "GetHashCode"};

        public List<ProtectedObject> GetAllApi()
        {
            var assemblies = _assembleFinder.GetAllAssemblies().Distinct(new AssemblyFullNameComparer()).Where(a => !a.IsDynamic).ToList();
            var apiPermissions = new List<ProtectedObject>();
            var allApiPermissions = new List<ProtectedObject>();

            var shaServiceType = typeof(SheshaAppServiceBase);

            foreach (var assembly in assemblies)
            {
                var services = assembly.GetTypes().Where(x => x.IsPublic && !x.IsAbstract && shaServiceType.IsAssignableFrom(x)).ToList();
                foreach (var service in services)
                {
                    var parent = new ProtectedObject() {Object = service.FullName};
                    apiPermissions.Add(parent);
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
                        var child = new ProtectedObject() { Object = service.FullName, Action = methodInfo.Name, Parent = parent};
                        parent.Child.Add(child);
                        allApiPermissions.Add(child);
                    }
                }
            }

            return allApiPermissions;
        }
    }
}