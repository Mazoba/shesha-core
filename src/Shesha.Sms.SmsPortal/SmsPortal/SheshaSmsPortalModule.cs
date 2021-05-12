using Abp.AspNetCore.Configuration;
using Abp.Modules;
using System.Reflection;
using Abp.Dependency;

namespace Shesha.Sms.SmsPortal
{
    [DependsOn(typeof(SheshaFrameworkModule), typeof(SheshaApplicationModule))]
    public class SheshaSmsPortalModule : AbpModule
    {
        public override void PreInitialize()
        {
            Configuration.Settings.Providers.Add<SmsPortalSettingProvider>();
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());

            IocManager.Register<SmsPortalGateway, SmsPortalGateway>(DependencyLifeStyle.Transient);
        }

        public override void PostInitialize()
        {
            try
            {
                Configuration.Modules.AbpAspNetCore().CreateControllersForAppServices(
                    this.GetType().Assembly,
                    moduleName: "SheshaSmsPortal",
                    useConventionalHttpVerbs: true);
            }
            catch
            {
                // note: we mute exceptions for unit tests only
                // todo: refactor and remove this try-catch block
            }
        }
    }
}
