using System;
using System.Threading.Tasks;
using Abp.Configuration;
using Microsoft.AspNetCore.Mvc;
using Shesha.Configuration;

namespace Shesha.Sms.SmsPortal
{
    /// inheritedDoc
    public class SmsPortalAppService: ISmsPortalAppService
    {
        private readonly ISettingManager _settingManager;

        /// <summary>
        /// Default constructor
        /// </summary>
        public SmsPortalAppService(ISettingManager settingManager)
        {
            _settingManager = settingManager;
        }

        /// inheritedDoc
        [HttpPut, Route("api/SmsPortal/Settings")]
        public async Task<bool> UpdateSettingsAsync(SmsPortalSettingsDto input)
        {
            await _settingManager.ChangeSettingAsync(SmsPortalSettingNames.Host, input.Host);
            await _settingManager.ChangeSettingAsync(SmsPortalSettingNames.Username, input.Username);
            await _settingManager.ChangeSettingAsync(SmsPortalSettingNames.Password, input.Password);

            await _settingManager.ChangeSettingAsync(SmsPortalSettingNames.UseProxy, input.UseProxy.ToString());
            await _settingManager.ChangeSettingAsync(SmsPortalSettingNames.WebProxyAddress, input.WebProxyAddress);
            await _settingManager.ChangeSettingAsync(SmsPortalSettingNames.UseDefaultProxyCredentials, input.UseDefaultProxyCredentials.ToString());
            await _settingManager.ChangeSettingAsync(SmsPortalSettingNames.WebProxyUsername, input.WebProxyUsername);
            await _settingManager.ChangeSettingAsync(SmsPortalSettingNames.WebProxyPassword, input.WebProxyPassword);

            return true;

        }

        /// inheritedDoc
        [HttpGet, Route("api/SmsPortal/Settings")]
        public async Task<SmsPortalSettingsDto> GetSettingsAsync()
        {
            var settings = new SmsPortalSettingsDto
            {
                Host = await _settingManager.GetSettingValueAsync(SmsPortalSettingNames.Host),
                Username = await _settingManager.GetSettingValueAsync(SmsPortalSettingNames.Username),
                Password = await _settingManager.GetSettingValueAsync(SmsPortalSettingNames.Password),
                UseProxy = Boolean.Parse((ReadOnlySpan<char>)await _settingManager.GetSettingValueAsync(SmsPortalSettingNames.UseProxy)),
                WebProxyAddress = await _settingManager.GetSettingValueAsync(SmsPortalSettingNames.WebProxyAddress),
                UseDefaultProxyCredentials = Boolean.Parse((ReadOnlySpan<char>)await _settingManager.GetSettingValueAsync(SmsPortalSettingNames.UseDefaultProxyCredentials)),
                WebProxyUsername = await _settingManager.GetSettingValueAsync(SmsPortalSettingNames.WebProxyUsername),
                WebProxyPassword = await _settingManager.GetSettingValueAsync(SmsPortalSettingNames.WebProxyPassword),
            };

            return settings;
        }
    }
}
