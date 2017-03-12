using System;
using System.Configuration;
using System.IO;

namespace StarsBot.Config
{
    public static class ConfigHandler
    {
        private static readonly Configuration ConfigManager =
            ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        private static readonly KeyValueConfigurationCollection Settings = ConfigManager.AppSettings.Settings;

        public static int Slack_PostDelay => Convert.ToInt32(Settings["Slack_PostDelay"].Value);
        public static bool Slack_Debugging => Convert.ToBoolean(Settings["Slack_Debugging"].Value);
    }
}
