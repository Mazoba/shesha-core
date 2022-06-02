using System.Reflection;
using Abp.AspNetCore;
using Abp.AspNetCore.Configuration;
using Abp.Dependency;
using Abp.Modules;

namespace Shesha.Sms.Clickatell
{
    [DependsOn(typeof(SheshaFrameworkModule), typeof(SheshaApplicationModule), typeof(AbpAspNetCoreModule))]
    public class SheshaClickatellModule : AbpModule
    {
        public override void PreInitialize()
        {
            Configuration.Settings.Providers.Add<ClickatellSettingProvider>();

            Configuration.Modules.AbpAspNetCore().CreateControllersForAppServices(
                this.GetType().Assembly,
                moduleName: "SheshaClickatell",
                useConventionalHttpVerbs: true);
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());

            IocManager.Register<ClickatellSmsGateway, ClickatellSmsGateway>(DependencyLifeStyle.Transient);
        }
    }
}
