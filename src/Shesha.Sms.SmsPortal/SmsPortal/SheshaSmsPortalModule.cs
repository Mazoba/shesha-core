using Abp.AspNetCore.Configuration;
using Abp.Modules;
using System.Reflection;
using Abp.Dependency;
using Abp.AspNetCore;

namespace Shesha.Sms.SmsPortal
{
    [DependsOn(typeof(SheshaFrameworkModule), typeof(SheshaApplicationModule), typeof(AbpAspNetCoreModule))]
    public class SheshaSmsPortalModule : AbpModule
    {
        public override void PreInitialize()
        {
            Configuration.Settings.Providers.Add<SmsPortalSettingProvider>();

            Configuration.Modules.AbpAspNetCore().CreateControllersForAppServices(
                this.GetType().Assembly,
                moduleName: "SheshaSmsPortal",
                useConventionalHttpVerbs: true);
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());

            IocManager.Register<SmsPortalGateway, SmsPortalGateway>(DependencyLifeStyle.Transient);
        }
    }
}
