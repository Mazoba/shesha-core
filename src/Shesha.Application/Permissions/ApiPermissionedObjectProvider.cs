using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Abp.Application.Services;
using Abp.AspNetCore.Mvc.Extensions;
using Abp.Modules;
using Abp.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Shesha.Utilities;

namespace Shesha.Permissions
{
    public class ApiPermissionedObjectProvider : PermissionedObjectProviderBase, IPermissionedObjectProvider
    {

        private readonly IApiDescriptionGroupCollectionProvider _apiDescriptionsProvider;

        public ApiPermissionedObjectProvider(IAssemblyFinder assembleFinder, IApiDescriptionGroupCollectionProvider apiDescriptionsProvider) : base(assembleFinder)
        {
            _apiDescriptionsProvider = apiDescriptionsProvider;
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

        public List<string> GetObjectTypes()
        {
            return new List<string> () {PermissionedObjectsSheshaTypes.WebApi, PermissionedObjectsSheshaTypes.WebCrudApi };
        }

        private bool IsCrud(Type type)
        {
            if (type.Name.Contains("Crud"))
                return true;
            if (type.BaseType != null)
                return IsCrud(type.BaseType);
            return false;
        }

        public string GetObjectType(Type type)
        {
            var shaServiceType = typeof(ApplicationService);
            var controllerType = typeof(ControllerBase);
            
            return !(type.IsPublic && !type.IsAbstract 
                                 && (shaServiceType.IsAssignableFrom(type) || controllerType.IsAssignableFrom(type)))
                   ? null // if not controller
                   : IsCrud(type)
                        ? PermissionedObjectsSheshaTypes.WebCrudApi
                        : PermissionedObjectsSheshaTypes.WebApi;
        }

        private string GetMethodType(string parentType)
        {
            return parentType == PermissionedObjectsSheshaTypes.WebApi
                ? PermissionedObjectsSheshaTypes.WebApiAction
                : parentType == PermissionedObjectsSheshaTypes.WebCrudApi
                    ? PermissionedObjectsSheshaTypes.WebCrudApiAction
                    : null;
        }

        public List<PermissionedObjectDto> GetAll(string objectType = null)
        {
            if (!GetObjectTypes().Contains(objectType)) return new List<PermissionedObjectDto>();

            var api = _apiDescriptionsProvider.ApiDescriptionGroups.Items.SelectMany(g => g.Items.Select(a =>
            {
                var descriptor = a.ActionDescriptor.AsControllerActionDescriptor();
                var module = descriptor.ControllerTypeInfo.AsType()
                    .Assembly.GetTypes().FirstOrDefault(t => t.IsPublic && !t.IsAbstract && typeof(AbpModule).IsAssignableFrom(t));
                return new ApiDescriptor()
                {
                    Module = module,
                    Service = descriptor.ControllerTypeInfo.AsType(),
                    HttpMethod = a.HttpMethod,
                    Endpoint = a.RelativePath,
                    Action = descriptor.MethodInfo
                };
            })).ToList();

            var allApiPermissions = new List<PermissionedObjectDto>();

            var modules = api.Select(x => x.Module).Distinct().ToList();
            foreach (var module in modules)
            {
                var services = api.Where(a => a.Module == module).Select(x => x.Service).Distinct().ToList();

                foreach (var service in services)
                {
                    var objType = GetObjectType(service);

                    Type entityType = null;

                    if (objType == PermissionedObjectsSheshaTypes.WebCrudApi)
                    {
                        var crudServiceType = typeof(AbpAsyncCrudAppService<,,,,,,,>);
                        var btype = service;
                        while (btype != null && (!btype.IsGenericType || btype.GetGenericTypeDefinition() != crudServiceType))
                        {
                            btype = btype.BaseType;
                        }

                        entityType = btype?.GetGenericArguments()[0];
                    }

                    if (objectType != null && objType != objectType) continue;
                    
                    var parent = new PermissionedObjectDto()
                    {
                        Object = service.FullName, 
                        Module = module?.FullName ?? "",
                        Name = GetName(service),
                        Type = objType, 
                        Description = GetDescription(service),
                        Dependency = entityType != null
                        ? entityType.FullName
                        : null
                    };
                    allApiPermissions.Add(parent);

                    var methods = api.Where(a => a.Module == module && a.Service == service).ToList();

                    foreach (var methodInfo in methods)
                    {
                        var methodName = methodInfo.Action.Name.RemovePostfix("Async");

                        var child = new PermissionedObjectDto()
                        {
                            Object = service.FullName + "@" + methodInfo.Action.Name,
                            Module = module?.FullName ?? "",
                            Name = GetName(methodInfo.Action),
                            Type = GetMethodType(objType),
                            Parent = service.FullName, 
                            Description = GetDescription(methodInfo.Action),
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

        private class ApiDescriptor
        {
            public Type Module { get; set; }
            public Type Service { get; set; }
            public MethodInfo Action { get; set; }
            public string HttpMethod { get; set; }
            public string Endpoint { get; set; }

        }
    }
}