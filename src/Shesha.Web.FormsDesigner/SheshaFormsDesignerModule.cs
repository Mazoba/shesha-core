using Abp.AspNetCore;
using Abp.AspNetCore.Configuration;
using Abp.AutoMapper;
using Abp.Modules;
using Abp.Reflection.Extensions;
using System.Reflection;

namespace Shesha.Web.FormsDesigner
{
    [DependsOn(typeof(AbpAspNetCoreModule))]
    public class SheshaFormsDesignerModule : AbpModule
    {
        public override void Initialize()
        {
            var thisAssembly = Assembly.GetExecutingAssembly();

            IocManager.RegisterAssemblyByConvention(thisAssembly);

            Configuration.Modules.AbpAutoMapper().Configurators.Add(
                // Scan the assembly for classes which inherit from AutoMapper.Profile
                cfg => cfg.AddMaps(thisAssembly)
            );
        }

        public override void PreInitialize()
        {
            Configuration.Modules.AbpAspNetCore().CreateControllersForAppServices(
                typeof(SheshaFormsDesignerModule).GetAssembly(),
                moduleName: "Shesha",
                useConventionalHttpVerbs: true);
        }
    }
}
