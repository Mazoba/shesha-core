using System.Reflection;
using Abp.AspNetCore.Configuration;
using Abp.AutoMapper;
using Abp.Modules;
using Shesha.Scheduler;

namespace Shesha.Import
{
    [DependsOn(
        typeof(AbpAutoMapperModule),
        typeof(SheshaSchedulerModule)
    )]
    public class SheshaImportModule : AbpModule
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

        public override void PostInitialize()
        {
            try
            {
                Configuration.Modules.AbpAspNetCore().CreateControllersForAppServices(
                    typeof(SheshaImportModule).Assembly,
                    moduleName: "Import",
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
