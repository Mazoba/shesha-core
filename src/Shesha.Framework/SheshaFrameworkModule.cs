using System.Reflection;
using Abp.AutoMapper;
using Abp.Dependency;
using Abp.Modules;
using Castle.MicroKernel.Registration;
using Microsoft.Extensions.Configuration;
using Shesha.Authorization;
using Shesha.Configuration;
using Shesha.Locks;
using Shesha.Services;
using Shesha.Services.StoredFiles;

namespace Shesha
{
    [DependsOn(typeof(AbpAutoMapperModule))]
    public class SheshaFrameworkModule : AbpModule
    {
        public SheshaFrameworkModule()
        {
        }

        public override void PreInitialize()
        {
            Configuration.Settings.Providers.Add<SheshaSettingProvider>();
        }

        public override void Initialize()
        {
            var thisAssembly = Assembly.GetExecutingAssembly();

            Configuration.Modules.AbpAutoMapper().Configurators.Add(
                // Scan the assembly for classes which inherit from AutoMapper.Profile
                cfg => cfg.AddMaps(thisAssembly)
            );

            IocManager.Register<IShaPermissionChecker, PermissionChecker>(DependencyLifeStyle.Transient);
            

            IocManager.Register<ILockFactory, NullLockFactory>(DependencyLifeStyle.Singleton);

            IocManager.Register<StoredFileService, StoredFileService>(DependencyLifeStyle.Transient);
            IocManager.Register<AzureStoredFileService, AzureStoredFileService>(DependencyLifeStyle.Transient);
            IocManager.IocContainer.Register(
                Component.For<IStoredFileService>().UsingFactoryMethod(f =>
                {
                    // IConfiguration configuration
                    var configuration = f.Resolve<IConfiguration>();
                    var isAzureEnvironment = configuration.GetValue<bool>("IsAzureEnvironment");

                    return isAzureEnvironment
                        ? f.Resolve<AzureStoredFileService>() as IStoredFileService
                        : f.Resolve<StoredFileService>() as IStoredFileService;
                })
            );
            
            IocManager.RegisterAssemblyByConvention(thisAssembly);
        }
        public override void PostInitialize()
        {
        }
    }
}