using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Dependency;
using Microsoft.AspNetCore.Mvc;
using Shesha.Configuration;
using Shesha.Sms.Dtos;

namespace Shesha.Sms
{
    [AbpAuthorize()]
    [Route("api/Sms/Settings")]
    public class SmsSettingsAppService: SheshaAppServiceBase, ITransientDependency
    {
        [HttpGet, Route("")]
        public async Task<SmsSettingsDto> GetSettings()
        {
            var settings = new SmsSettingsDto
            {
                Gateway = await GetSettingValueAsync(SheshaSettingNames.Sms.SmsGateway),
                RedirectAllMessagesTo = (await GetSettingValueAsync(SheshaSettingNames.Sms.RedirectAllMessagesTo)),
            };
            
            return settings;
        }

        [HttpPut, Route("")]
        public async Task UpdateSettings(SmsSettingsDto input)
        {
            await ChangeSettingAsync(SheshaSettingNames.Sms.SmsGateway, input.Gateway);
            await ChangeSettingAsync(SheshaSettingNames.Sms.RedirectAllMessagesTo, input.RedirectAllMessagesTo);
        }
    }
}
