using System.Reflection;
using Abp.AspNetCore;
using Abp.AspNetCore.Configuration;
using Abp.Dependency;
using Abp.Modules;

namespace Shesha.Sms.Xml2Sms
{
    [DependsOn(typeof(SheshaFrameworkModule), typeof(SheshaApplicationModule), typeof(AbpAspNetCoreModule))]
    public class SheshaXml2SmsModule : AbpModule
    {
        public override void PreInitialize()
        {
            Configuration.Settings.Providers.Add<Xml2SmsSettingProvider>();

            Configuration.Modules.AbpAspNetCore().CreateControllersForAppServices(
                this.GetType().Assembly,
                moduleName: "SheshaXml2Sms",
                useConventionalHttpVerbs: true);
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());

            IocManager.Register<Xml2SmsGateway, Xml2SmsGateway>(DependencyLifeStyle.Transient);
        }
    }
}
