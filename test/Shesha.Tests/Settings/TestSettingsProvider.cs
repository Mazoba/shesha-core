using System.Collections.Generic;
using Abp.Configuration;
using Shesha.Configuration;

namespace Shesha.Tests.DynamicEntities
{
    public class TestSettingsProvider : SettingProvider
    {
        public override IEnumerable<SettingDefinition> GetSettingDefinitions(SettingDefinitionProviderContext context)
        {
            return new[] { new SettingDefinition("TestSetting", "Default Test setting value" ) };
        }
    }
}