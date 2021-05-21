using System.Reflection;
using Abp.AspNetCore.Configuration;
using Abp.Dependency;
using Abp.Localization.Dictionaries.Xml;
using Abp.Localization.Sources;
using Abp.Modules;
using Abp.Zero;
using Shesha.Firebase;
using Shesha.Firebase.Configuration;

namespace Shesha
{
    /// <summary>
    /// This module extends module zero to add Firebase notifications.
    /// </summary>
    [DependsOn(typeof(AbpZeroCommonModule))]
    public class SheshaFirebaseModule : AbpModule
    {
        public override void PreInitialize()
        {
            //IocManager.Register<ISheshaFirebaseModuleConfig, SheshaFirebaseModuleConfig>();

            Configuration.Localization.Sources.Extensions.Add(
                new LocalizationSourceExtensionInfo(
                    AbpZeroConsts.LocalizationSourceName,
                    new XmlEmbeddedFileLocalizationDictionaryProvider(
                        Assembly.GetExecutingAssembly(),
                        "Shesha.Ldap.Localization.Source")
                )
            );

            Configuration.Settings.Providers.Add<FirebaseSettingProvider>();
        }

        public override void Initialize()
        {
            IocManager.Register<FirebaseAppService, FirebaseAppService>(DependencyLifeStyle.Transient);

            IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());
        }

        public override void PostInitialize()
        {
            try
            {
                Configuration.Modules.AbpAspNetCore().CreateControllersForAppServices(
                    this.GetType().Assembly,
                    moduleName: "SheshaFirebase",
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
