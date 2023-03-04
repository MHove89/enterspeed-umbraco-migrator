﻿using Enterspeed.Migrator;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Migrator.Package
{
    public class PackageComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.RegisterEnterspeedServices(builder.Config);
            builder.Services.RegisterUmbraco9MigratorService(builder.Config);
            builder.Dashboards().Add<EnterspeedMigratorDashboard>();
        }
    }
}