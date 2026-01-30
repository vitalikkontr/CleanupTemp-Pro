using System;
using System.Configuration;

namespace CleanupTemp_Pro.Properties
{
    internal sealed partial class Settings : ApplicationSettingsBase
    {
        private static Settings defaultInstance = ((Settings)(ApplicationSettingsBase.Synchronized(new Settings())));

        public static Settings Default
        {
            get
            {
                return defaultInstance;
            }
        }

        [UserScopedSetting()]
        [DefaultSettingValue("Dark")]
        public string Theme
        {
            get
            {
                return ((string)(this["Theme"]));
            }
            set
            {
                this["Theme"] = value;
            }
        }
    }
}
