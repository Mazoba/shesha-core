using System.Collections.Generic;
using Abp.Configuration;

namespace Shesha.Configuration
{
    public class SheshaSettingProvider : SettingProvider
    {
        public override IEnumerable<SettingDefinition> GetSettingDefinitions(SettingDefinitionProviderContext context)
        {
            return new[]
            {
                new SettingDefinition(
                    SheshaSettingNames.UploadFolder,
                    "~/App_Data/Upload"
                ),
                new SettingDefinition(
                    SheshaSettingNames.ExchangeName,
                    ""
                ),
                new SettingDefinition(
                    SheshaSettingNames.Security.AutoLogoffTimeout,
                    0.ToString()
                ),
            };
        }
    }
}
