using System;

namespace Enterspeed.Migrator.Settings
{
    public class EnterspeedConfiguration
    {
        public static string ConfigurationKey => "EnterspeedConfiguration";
        public string ApiKey { get; init; }
        public string NavigationHandle { get; set; }
        public string MigrationPageMetaData { get; set; }
        public string[] ComponentPropertyTypeKeys { get; set; } = Array.Empty<string>();
    }
}