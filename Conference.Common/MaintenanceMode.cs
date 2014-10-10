using System;
using System.Configuration;

namespace Conference.Common {
    /// <summary>
    /// 维护模式
    /// </summary>
    public class MaintenanceMode {
        public const string MaintenanceModeSettingName = "MaintenanceMode";
        public static bool IsInMaintainanceMode { get; internal set; }

        public static void RefreshIsInMaintainanceMode() {
            string settingValue = ConfigurationManager.AppSettings[MaintenanceModeSettingName];
            IsInMaintainanceMode = (!string.IsNullOrEmpty(settingValue) &&
                string.Equals(settingValue, "true", StringComparison.OrdinalIgnoreCase));
        }
    }
}