using System.Reflection;
using Abp.AspNetCore.Configuration;
using Abp.Dependency;
using Abp.Modules;

namespace Shesha.Sms.BulkSms
{
    [DependsOn(typeof(SheshaFrameworkModule), typeof(SheshaApplicationModule))]
    public class SheshaBulkSmsModule : AbpModule
    {
        public override void PreInitialize()
        {
            Configuration.Settings.Providers.Add<BulkSmsSettingProvider>();
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());

            IocManager.Register<BulkSmsGateway, BulkSmsGateway>(DependencyLifeStyle.Transient);
        }

        public override void PostInitialize()
        {
            try
            {
                Configuration.Modules.AbpAspNetCore().CreateControllersForAppServices(
                    this.GetType().Assembly,
                    moduleName: "SheshaBulkSms",
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
