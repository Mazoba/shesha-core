using System;
using System.Threading.Tasks;
using Abp.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Shesha.Sms.BulkSms
{
    /// <summary>
    /// Bulk SMS Gateway service
    /// </summary>
    [AbpAuthorize()]
    public class BulkSmsAppService : SheshaAppServiceBase, IBulkSmsAppService
    {
        /// <summary>
        /// Get Bulk SMS settings
        /// </summary>
        [HttpGet, Route("api/BulkSmsGateway/Settings")]
        public async Task<BulkSmsSettingsDto> GetSettingsAsync()
        {
            var settings = new BulkSmsSettingsDto
            {
                ApiUrl = await GetSettingValueAsync(BulkSmsSettingNames.ApiUrl),
                ApiUsername = await GetSettingValueAsync(BulkSmsSettingNames.ApiUsername),
                ApiPassword = await GetSettingValueAsync(BulkSmsSettingNames.ApiPassword),

                UseProxy = Boolean.Parse((ReadOnlySpan<char>)await GetSettingValueAsync(BulkSmsSettingNames.UseProxy)),
                WebProxyAddress = await GetSettingValueAsync(BulkSmsSettingNames.WebProxyAddress),
                UseDefaultProxyCredentials = Boolean.Parse((ReadOnlySpan<char>)await GetSettingValueAsync(BulkSmsSettingNames.UseDefaultProxyCredentials)),
                WebProxyUsername = await GetSettingValueAsync(BulkSmsSettingNames.WebProxyUsername),
                WebProxyPassword = await GetSettingValueAsync(BulkSmsSettingNames.WebProxyPassword),
            };

            return settings;
        }

        /// <summary>
        /// Update Bulk SMS settings
        /// </summary>
        [HttpPut, Route("api/BulkSmsGateway/Settings")]
        public async Task UpdateSettingsAsync(BulkSmsSettingsDto input)
        {
            await ChangeSettingAsync(BulkSmsSettingNames.ApiUrl, input.ApiUrl);
            await ChangeSettingAsync(BulkSmsSettingNames.ApiUsername, input.ApiUsername);
            await ChangeSettingAsync(BulkSmsSettingNames.ApiPassword, input.ApiPassword);

            await ChangeSettingAsync(BulkSmsSettingNames.UseProxy, input.UseProxy.ToString());
            await ChangeSettingAsync(BulkSmsSettingNames.WebProxyAddress, input.WebProxyAddress);
            await ChangeSettingAsync(BulkSmsSettingNames.UseDefaultProxyCredentials, input.UseDefaultProxyCredentials.ToString());
            await ChangeSettingAsync(BulkSmsSettingNames.WebProxyUsername, input.WebProxyUsername);
            await ChangeSettingAsync(BulkSmsSettingNames.WebProxyPassword, input.WebProxyPassword);
        }
    }
}
