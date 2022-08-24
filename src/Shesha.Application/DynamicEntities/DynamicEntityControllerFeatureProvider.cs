using Abp.Dependency;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Shesha.Application.Services;
using Shesha.Configuration.Runtime;
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
            var entityConfigurationStore = _iocManager.Resolve<IEntityConfigurationStore>();

            var entityControllers = feature.Controllers.Where(c => c.AsType().ImplementsGenericInterface(typeof(IEntityAppService<,>))).ToList();
            foreach (var controller in entityControllers) 
            {
                var genericInterface = controller.AsType().GetGenericInterfaces(typeof(IEntityAppService<,>)).FirstOrDefault();

                var entityType = genericInterface.GenericTypeArguments.FirstOrDefault();

                entityConfigurationStore.SetDefaultAppService(entityType, controller);
            }

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

                    var appServiceType = DynamicAppServiceHelper.MakeApplicationServiceType(entityType);
                    if (appServiceType != null) 
                    {
                        entityConfigurationStore.SetDefaultAppService(entityType, appServiceType);

                        var controllerName = MvcHelper.GetControllerName(appServiceType);
                        if (!existingControllerNames.Contains(controllerName)) 
                        {
                            feature.Controllers.Add(appServiceType.GetTypeInfo());
                            
                            if (!_iocManager.IsRegistered(appServiceType))
                                _iocManager.Register(appServiceType, lifeStyle: DependencyLifeStyle.Transient);
                        }
                    }
                }
            }
        }
    }
}
