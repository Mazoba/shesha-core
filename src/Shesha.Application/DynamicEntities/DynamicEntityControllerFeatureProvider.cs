using Abp.Dependency;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Shesha.Domain.Attributes;
using Shesha.DynamicEntities.Dtos;
using Shesha.Extensions;
using Shesha.Reflection;
using Shesha.Startup;
using Shesha.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shesha.DynamicEntities
{
    /// <summary>
    /// Controller provider that generates dynamic applicaiton services (controllers) for registered entities
    /// </summary>
    public class DynamicEntityControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        private readonly IocManager _iocManager;

        public DynamicEntityControllerFeatureProvider(IocManager iocManager)
        {
            _iocManager = iocManager;
        }

        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            var shaConfig = _iocManager.Resolve<IShaApplicationModuleConfiguration>();
            foreach (var registration in shaConfig.DynamicApplicationServiceRegistrations) 
            { 
                var entityTypes = registration.Assembly.GetExportedTypes().Where(t => t.IsEntityType()).ToList();

                var existingControllerNames = feature.Controllers.Select(c => MvcHelper.GetControllerName(c)).ToList(); 

                foreach (var entityType in entityTypes)
                {
                    var entityAttribute = entityType.GetAttribute<EntityAttribute>();
                    if (entityAttribute != null && !entityAttribute.GenerateApplicationService)
                        continue;

                    var appServiceType = GetAppServiceType(entityType);
                    if (appServiceType != null) 
                    {
                        var controllerName = MvcHelper.GetControllerName(appServiceType);
                        if (!existingControllerNames.Contains(controllerName)) 
                        {
                            feature.Controllers.Add(appServiceType.GetTypeInfo());
                        }
                    }
                }
            }
        }

        private Type GetAppServiceType(Type entityType)
        {
            var idType = entityType.GetProperty("Id")?.PropertyType;
            if (idType == null)
                return null;

            var dtoType = typeof(DynamicDto<,>).MakeGenericType(entityType, idType);

            var appServiceType = typeof(DynamicCrudAppService<,,>).MakeGenericType(entityType, dtoType, idType);

            return appServiceType;
        }
    }
}
