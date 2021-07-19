using System.Linq;
using System.Reflection;
using Abp;
using Abp.AutoMapper;
using Abp.Configuration;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Modules;
using Abp.Net.Mail;
using Abp.Net.Mail.Smtp;
using Abp.Notifications;
using Abp.Reflection;
using Castle.MicroKernel.Registration;
using Shesha.Authorization;
using Shesha.Configuration;
using Shesha.Email;
using Shesha.Notifications;
using Shesha.Otp.Configuration;
using Shesha.Push;
using Shesha.Push.Configuration;
using Shesha.Reflection;
using Shesha.Sms;
using Shesha.Sms.Configuration;

namespace Shesha
{
    [DependsOn(
        typeof(AbpKernelModule),
        typeof(SheshaCoreModule), 
        typeof(AbpAutoMapperModule))]
    public class SheshaApplicationModule : AbpModule
    {
        public override void PreInitialize()
        {
            Configuration.Settings.Providers.Add<SmsSettingProvider>();
            Configuration.Settings.Providers.Add<PushSettingProvider>();
            Configuration.Settings.Providers.Add<EmailSettingProvider>();
            Configuration.Notifications.Providers.Add<ShaNotificationProvider>();
            Configuration.Notifications.Notifiers.Add<EmailRealTimeNotifier>();
            Configuration.Notifications.Notifiers.Add<SmsRealTimeNotifier>();
            Configuration.Notifications.Notifiers.Add<PushRealTimeNotifier>();

            Configuration.Authorization.Providers.Add<SheshaAuthorizationProvider>();
            
            // replace email sender
            Configuration.ReplaceService<ISmtpEmailSenderConfiguration, SmtpEmailSenderSettings>(DependencyLifeStyle.Transient);

            Configuration.Settings.Providers.Add<OtpSettingProvider>();

            Configuration.Notifications.Distributers.Clear();
            Configuration.Notifications.Distributers.Add<ShaNotificationDistributer>();

            Configuration.ReplaceService<INotificationPublisher, ShaNotificationPublisher>(DependencyLifeStyle.Transient);

            IocManager.IocContainer.Register(
                Component.For<IEmailSender>().Forward<ISheshaEmailSender>().Forward<SheshaEmailSender>().ImplementedBy<SheshaEmailSender>().LifestyleTransient()
            );

            #region Push notifications

            IocManager.Register<NullPushNotifier, NullPushNotifier>(DependencyLifeStyle.Transient);
            IocManager.IocContainer.Register(
                Component.For<IPushNotifier>().UsingFactoryMethod(f =>
                {
                    var settings = f.Resolve<ISettingManager>();
                    var pushNotifier = settings.GetSettingValue(SheshaSettingNames.Push.PushNotifier);

                    var pushNotifierType = !string.IsNullOrWhiteSpace(pushNotifier)
                        ? f.Resolve<ITypeFinder>().Find(t => typeof(IPushNotifier).IsAssignableFrom(t) && t.GetClassUid() == pushNotifier).FirstOrDefault()
                        : null;
                    return pushNotifierType != null
                        ? f.Resolve(pushNotifierType) as IPushNotifier
                        : null;
                }, managedExternally: true).LifestyleTransient()
            );

            #endregion

            #region SMS Gateways

            IocManager.Register<NullSmsGateway, NullSmsGateway>(DependencyLifeStyle.Transient);
            IocManager.IocContainer.Register(
                Component.For<ISmsGateway>().UsingFactoryMethod(f =>
                {
                    var settings = f.Resolve<ISettingManager>();
                    var gatewayUid = settings.GetSettingValue(SheshaSettingNames.Sms.SmsGateway);

                    var gatewayType = !string.IsNullOrWhiteSpace(gatewayUid)
                        ? f.Resolve<ITypeFinder>().Find(t => typeof(ISmsGateway).IsAssignableFrom(t) && t.GetClassUid() == gatewayUid).FirstOrDefault()
                        : null;

                    var gateway = gatewayType != null
                        ? f.Resolve(gatewayType) as ISmsGateway
                        : null;

                    return gateway ?? new NullSmsGateway();
                }, managedExternally: true).LifestyleTransient()
            );

            #endregion
        }

        public override void Initialize()
        {
            var thisAssembly = Assembly.GetExecutingAssembly();
            IocManager.RegisterAssemblyByConvention(thisAssembly);

            Configuration.Modules.AbpAutoMapper().Configurators.Add(
                // Scan the assembly for classes which inherit from AutoMapper.Profile
                cfg => cfg.AddMaps(thisAssembly)
            );
        }
    }
}
