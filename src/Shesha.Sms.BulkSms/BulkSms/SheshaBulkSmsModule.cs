using System.Reflection;
using Abp.AspNetCore;
using Abp.AspNetCore.Configuration;
using Abp.Dependency;
using Abp.Modules;

namespace Shesha.Sms.BulkSms
{
    [DependsOn(typeof(SheshaFrameworkModule), typeof(SheshaApplicationModule), typeof(AbpAspNetCoreModule))]
    public class SheshaBulkSmsModule : AbpModule
    {
        public override void PreInitialize()
        {
            Configuration.Settings.Providers.Add<BulkSmsSettingProvider>();

            Configuration.Modules.AbpAspNetCore().CreateControllersForAppServices(
                this.GetType().Assembly,
                moduleName: "SheshaBulkSms",
                useConventionalHttpVerbs: true);
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());

            IocManager.Register<BulkSmsGateway, BulkSmsGateway>(DependencyLifeStyle.Transient);
        }
    }
}
