using System;
using System.Threading.Tasks;
using Abp.Configuration;
using Abp.Dependency;
using Abp.Runtime.Session;
using Microsoft.AspNetCore.Mvc;
using Shesha.Configuration;
using Shesha.Services;
using Shesha.Utilities;

namespace Shesha.Sms.Clickatell
{
    /// inheritDoc
    public class ClickatellAppService : IClickatellAppService
    {

        /// <summary>
        /// Reference to the current Session.
        /// </summary>
        public IAbpSession AbpSession { get; set; }

        private readonly ISettingManager _settingManager;

        /// <summary>
        /// Default constructor
        /// </summary>
        public ClickatellAppService(ISettingManager settingManager)
        {
            _settingManager = settingManager;
            
            AbpSession = NullAbpSession.Instance;
        }

        /// inheritDoc
        [HttpPut, Route("api/Clickatell/Settings")]
        public async Task<bool> UpdateSettingsAsync(ClickatellSettingDto input)
        {
            await _settingManager.ChangeSettingAsync(ClickatellSettingNames.Host, input.ClickatellHost);
            await _settingManager.ChangeSettingAsync(ClickatellSettingNames.ApiId, input.ClickatellApiId);
            await _settingManager.ChangeSettingAsync(ClickatellSettingNames.ApiUsername, input.ClickatellApiUsername);
            await _settingManager.ChangeSettingAsync(ClickatellSettingNames.ApiPassword, input.ClickatellApiPassword);
            await _settingManager.ChangeSettingAsync(ClickatellSettingNames.SingleMessageMaxLength, input.SingleMessageMaxLength.ToString());
            await _settingManager.ChangeSettingAsync(ClickatellSettingNames.MessagePartLength, input.MessagePartLength.ToString());

            await _settingManager.ChangeSettingAsync(ClickatellSettingNames.UseProxy, input.UseProxy.ToString());
            await _settingManager.ChangeSettingAsync(ClickatellSettingNames.WebProxyAddress, input.WebProxyAddress);
            await _settingManager.ChangeSettingAsync(ClickatellSettingNames.UseDefaultProxyCredentials, input.UseDefaultProxyCredentials.ToString());
            await _settingManager.ChangeSettingAsync(ClickatellSettingNames.WebProxyUsername, input.WebProxyUsername);
            await _settingManager.ChangeSettingAsync(ClickatellSettingNames.WebProxyPassword, input.WebProxyPassword);

            return true;
        }

        /// inheritDoc
        [HttpGet, Route("api/Clickatell/Settings")]
        public async Task<ClickatellSettingDto> GetSettingsAsync()
        {
            var settings = new ClickatellSettingDto
            {
                ClickatellHost = await _settingManager.GetSettingValueAsync(ClickatellSettingNames.Host),
                ClickatellApiId = await _settingManager.GetSettingValueAsync(ClickatellSettingNames.ApiId),
                ClickatellApiUsername = await _settingManager.GetSettingValueAsync(ClickatellSettingNames.ApiUsername),
                ClickatellApiPassword = await _settingManager.GetSettingValueAsync(ClickatellSettingNames.ApiPassword),
                UseProxy = Boolean.Parse((ReadOnlySpan<char>)await _settingManager.GetSettingValueAsync(ClickatellSettingNames.UseProxy)),
                WebProxyAddress = await _settingManager.GetSettingValueAsync(ClickatellSettingNames.WebProxyAddress),
                UseDefaultProxyCredentials = Boolean.Parse((ReadOnlySpan<char>)await _settingManager.GetSettingValueAsync(ClickatellSettingNames.UseDefaultProxyCredentials)),
                WebProxyUsername = await _settingManager.GetSettingValueAsync(ClickatellSettingNames.WebProxyUsername),
                WebProxyPassword = await _settingManager.GetSettingValueAsync(ClickatellSettingNames.WebProxyPassword),

                SingleMessageMaxLength = (await _settingManager.GetSettingValueAsync(ClickatellSettingNames.SingleMessageMaxLength)).ToInt(ClickatellSettingProvider.DefaultSingleMessageMaxLength),
                MessagePartLength = (await _settingManager.GetSettingValueAsync(ClickatellSettingNames.MessagePartLength)).ToInt(ClickatellSettingProvider.DefaultMessagePartLength)
            };
            
            return settings;
        }

        public async Task TestSms(string mobileNumber, string body)
        {
            var gateway = StaticContext.IocManager.Resolve<ISmsGateway>();
            await gateway.SendSmsAsync(mobileNumber, body);
        }
    }
}
