using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Abp.Application.Services;
using Abp.Dependency;
using Abp.Reflection;
using Shesha.Permissions;
using Shesha.Reflection;
using Shesha.Utilities;

namespace Shesha.Permissions
{
    public class CrudPermissionedObjectProvider : PermissionedObjectProviderBase, IPermissionedObjectProvider
    {

        public CrudPermissionedObjectProvider(IAssemblyFinder assembleFinder) : base(assembleFinder)
        {
        }

        private Dictionary<string, string> CrudMethods = new Dictionary<string, string>
        {
            { "Get", "Get" }, 
            { "GetAll", "Get" },
            { "Create", "Create" },
            { "Update", "Update" },
            { "Delete", "Delete" },
            { "Remove", "Delete" }
        };

        public const string ObjectCategory = "crudServices";

        public string GetCategory()
        {
            return ObjectCategory;
        }

        public string GetCategoryByType(Type type)
        {
            var shaServiceType = typeof(SheshaAppServiceBase);
            var crudServiceType = typeof(IAsyncCrudAppService<,,,,,,>);

            return type.IsPublic && !type.IsAbstract && shaServiceType.IsAssignableFrom(type)
                   && type.GetInterfaces().Any(x =>
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
            var crudServiceInt = typeof(IAsyncCrudAppService<,,,,,,>);
            var crudServiceType = typeof(AbpAsyncCrudAppService<,,,,,,,>);

            foreach (var assembly in assemblies)
            {
                var services = assembly.GetTypes()
                    .Where(t => t.IsPublic && !t.IsAbstract && shaServiceType.IsAssignableFrom(t) && t.GetInterfaces().Any(x =>
                                   x.IsGenericType &&
                                   x.GetGenericTypeDefinition() == crudServiceInt))
                    .ToList();
                foreach (var service in services)
                {
                    var btype = service;
                    while (btype != null && (!btype.IsGenericType || btype.GetGenericTypeDefinition() != crudServiceType))
                    {
                        btype = btype.BaseType;
                    }

                    var entityType = btype?.GetGenericArguments()[0];

                    var parent = new PermissionedObjectDto()
                    {
                        Object = service.FullName, 
                        Category = ObjectCategory, 
                        Description = GetDescription(service),
                        Dependency = entityType != null 
                            ? entityType.FullName
                            : null
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
                        var methodName = methodInfo.Name.RemovePostfix("Async");

                        var child = new PermissionedObjectDto()
                        {
                            Object = service.FullName + "@" + methodInfo.Name, 
                            //Action = methodInfo.Name, 
                            Category = ObjectCategory, 
                            Parent = service.FullName, 
                            Description = GetDescription(methodInfo),
                            Dependency = entityType != null && CrudMethods.ContainsKey(methodName)
                                ? entityType.FullName + "@" + CrudMethods.GetValueOrDefault(methodName)
                                : null
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