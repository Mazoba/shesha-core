using System.Reflection;
using Abp.AspNetCore;
using Abp.AspNetCore.Configuration;
using Abp.AutoMapper;
using Abp.Modules;

namespace Shesha.Web
{
    [DependsOn(typeof(AbpAutoMapperModule), typeof(AbpAspNetCoreModule))]
    public class SheshaWebControlsModule : AbpModule
    {
        public override void Initialize()
        {
            var thisAssembly = Assembly.GetExecutingAssembly();

            Configuration.Modules.AbpAutoMapper().Configurators.Add(
                // Scan the assembly for classes which inherit from AutoMapper.Profile
                cfg => cfg.AddMaps(thisAssembly)
            );
            IocManager.RegisterAssemblyByConvention(thisAssembly);
        }

        public override void PreInitialize()
        {
            Configuration.Modules.AbpAspNetCore().CreateControllersForAppServices(
                this.GetType().Assembly,
                moduleName: "Shesha",
                useConventionalHttpVerbs: true);
        }
    }
}
