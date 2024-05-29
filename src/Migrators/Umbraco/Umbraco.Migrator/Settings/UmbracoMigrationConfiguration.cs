using System;

namespace Umbraco.Migrator.Settings
{
    public class UmbracoMigrationConfiguration
    {
        public static string ConfigurationKey => "UmbracoMigrationConfiguration";
        public string RootDocType { get; set; }
        public string[] CompositionKeys { get; set; } = Array.Empty<string>();
        public string ContentPropertyAlias { get; set; }
        public StartIds StartIds { get; set; } = new();
    }
}