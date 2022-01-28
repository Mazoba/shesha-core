using Abp.Dependency;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Shesha.DynamicEntities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shesha.Startup
{
    /// <summary>
    /// Configuration of the <see cref="SheshaApplicationModule"/>
    /// </summary>
    public class ShaApplicationModuleConfiguration : IShaApplicationModuleConfiguration
    {
        public List<DynamicAppServiceRegistration> DynamicApplicationServiceRegistrations { get; private set; }
        public IocManager IocManager { get; set; }

        public ShaApplicationModuleConfiguration()
        {
            DynamicApplicationServiceRegistrations = new List<DynamicAppServiceRegistration>();
        }

        public void CreateAppServicesForEntities(Assembly assembly, string moduleName)
        {
            var partManager = IocManager.Resolve<ApplicationPartManager>();

            var feature = GetFeature(partManager);

            if (feature == null)
                partManager.FeatureProviders.Add(feature = new DynamicEntityControllerFeatureProvider(IocManager));


            RegisterDynamicApplicationServices(assembly, moduleName);
        }

        public void RegisterDynamicApplicationServices(Assembly assembly, string moduleName)
        {
            DynamicApplicationServiceRegistrations.Add(new DynamicAppServiceRegistration
            {
                Assembly = assembly,
                ModuleName = moduleName,
            });
        }

        private DynamicEntityControllerFeatureProvider GetFeature(ApplicationPartManager partManager) 
        {
            return partManager.FeatureProviders.OfType<DynamicEntityControllerFeatureProvider>().FirstOrDefault();
        } 
    }
}
