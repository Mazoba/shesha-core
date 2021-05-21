using System.Reflection;
using Abp.AspNetCore.Configuration;
using Abp.AutoMapper;
using Abp.Modules;

namespace Shesha.Web.FormsDesigner
{
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

        public override void PostInitialize()
        {
            try
            {
                Configuration.Modules.AbpAspNetCore().CreateControllersForAppServices(
                    typeof(SheshaFormsDesignerModule).Assembly,
                    moduleName: "Shesha",
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
