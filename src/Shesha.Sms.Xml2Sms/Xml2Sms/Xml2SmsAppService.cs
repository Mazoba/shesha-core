using System;
using System.Threading.Tasks;
using Abp.Configuration;
using Abp.Dependency;
using Microsoft.AspNetCore.Mvc;
using Shesha.Configuration;
using Shesha.Services;

namespace Shesha.Sms.Xml2Sms
{
    /// inheritDoc
    public class Xml2SmsAppService : IXml2SmsAppService
    {
        private readonly ISettingManager _settingManager;

        /// <summary>
        /// Default constructor
        /// </summary>
        public Xml2SmsAppService(ISettingManager settingManager)
        {
            _settingManager = settingManager;
        }

        /// inheritDoc
        [HttpPut, Route("api/Xml2Sms/Settings")]
        public async Task<bool> UpdateSettingsAsync(Xml2SmsSettingDto input)
        {
            await _settingManager.ChangeSettingAsync(Xml2SmsSettingNames.Host, input.Xml2SmsHost);
            await _settingManager.ChangeSettingAsync(Xml2SmsSettingNames.ApiPassword, input.Xml2SmsPassword);
            await _settingManager.ChangeSettingAsync(Xml2SmsSettingNames.ApiUsername, input.Xml2SmsUsername);
            await _settingManager.ChangeSettingAsync(Xml2SmsSettingNames.UseProxy, input.UseProxy.ToString());
            await _settingManager.ChangeSettingAsync(Xml2SmsSettingNames.WebProxyAddress, input.WebProxyAddress);
            await _settingManager.ChangeSettingAsync(Xml2SmsSettingNames.UseDefaultProxyCredentials, input.UseDefaultProxyCredentials.ToString());
            await _settingManager.ChangeSettingAsync(Xml2SmsSettingNames.WebProxyUsername, input.WebProxyUsername);
            await _settingManager.ChangeSettingAsync(Xml2SmsSettingNames.WebProxyPassword, input.WebProxyPassword);

            return true;
        }

        /// inheritDoc
        [HttpGet, Route("api/Xml2Sms/Settings")]
        public async Task<Xml2SmsSettingDto> GetSettingsAsync()
        {
            var settings = new Xml2SmsSettingDto
            {
                Xml2SmsHost = await _settingManager.GetSettingValueAsync(Xml2SmsSettingNames.Host),
                Xml2SmsPassword = await _settingManager.GetSettingValueAsync(Xml2SmsSettingNames.ApiPassword),
                Xml2SmsUsername = await _settingManager.GetSettingValueAsync(Xml2SmsSettingNames.ApiUsername),
                UseProxy = Boolean.Parse((ReadOnlySpan<char>)await _settingManager.GetSettingValueAsync(Xml2SmsSettingNames.UseProxy)),
                WebProxyAddress = await _settingManager.GetSettingValueAsync(Xml2SmsSettingNames.WebProxyAddress),
                UseDefaultProxyCredentials = Boolean.Parse((ReadOnlySpan<char>)await _settingManager.GetSettingValueAsync(Xml2SmsSettingNames.UseDefaultProxyCredentials)),
                WebProxyUsername = await _settingManager.GetSettingValueAsync(Xml2SmsSettingNames.WebProxyUsername),
                WebProxyPassword = await _settingManager.GetSettingValueAsync(Xml2SmsSettingNames.WebProxyPassword),
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
