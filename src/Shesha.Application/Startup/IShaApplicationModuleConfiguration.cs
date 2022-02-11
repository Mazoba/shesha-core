using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.Startup
{
    /// <summary>
    /// Configuration of the <see cref="SheshaApplicationModule"/>
    /// </summary>
    public interface IShaApplicationModuleConfiguration
    {
        List<DynamicAppServiceRegistration> DynamicApplicationServiceRegistrations { get; }

        void CreateAppServicesForEntities(
            Assembly assembly,
            string moduleName
        );
    }
}
