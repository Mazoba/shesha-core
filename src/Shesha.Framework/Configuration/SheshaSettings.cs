using System;
using Abp.Configuration;
using Abp.Dependency;
using Shesha.Domain;

namespace Shesha.Configuration
{
    public class SheshaSettings: ISheshaSettings, ITransientDependency
    {
        private readonly ISettingManager _settingManager;

        public SheshaSettings(ISettingManager settingManager)
        {
            _settingManager = settingManager;
        }

        /// <summary>
        /// Upload folder for stored files (<see cref="StoredFile"/>) 
        /// </summary>
        public string UploadFolder => _settingManager.GetSettingValue(SheshaSettingNames.UploadFolder);
        public string ExchangeName => _settingManager.GetSettingValue(SheshaSettingNames.ExchangeName);
        /// <summary>
        /// Auto logoff timeout (0 - disabled)
        /// </summary>
        public string AutoLogoffTimeout => _settingManager.GetSettingValue(SheshaSettingNames.Security.AutoLogoffTimeout);
    }
}
