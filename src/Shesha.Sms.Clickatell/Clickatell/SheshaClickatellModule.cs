using System.Reflection;
using Abp.AspNetCore.Configuration;
using Abp.Dependency;
using Abp.Modules;

namespace Shesha.Sms.Clickatell
{
    [DependsOn(typeof(SheshaFrameworkModule), typeof(SheshaApplicationModule))]
    public class SheshaClickatellModule : AbpModule
    {
        public override void PreInitialize()
        {
            Configuration.Settings.Providers.Add<ClickatellSettingProvider>();
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());

            IocManager.Register<ClickatellSmsGateway, ClickatellSmsGateway>(DependencyLifeStyle.Transient);
        }

        public override void PostInitialize()
        {
            try
            {
                Configuration.Modules.AbpAspNetCore().CreateControllersForAppServices(
                    this.GetType().Assembly,
                    moduleName: "SheshaClickatell",
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
