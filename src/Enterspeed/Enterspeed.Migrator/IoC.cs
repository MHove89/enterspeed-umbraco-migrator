using Enterspeed.Delivery.Sdk.Api.Providers;
using Enterspeed.Delivery.Sdk.Api.Services;
using Enterspeed.Delivery.Sdk.Configuration;
using Enterspeed.Delivery.Sdk.Domain.Connection;
using Enterspeed.Delivery.Sdk.Domain.Providers;
using Enterspeed.Delivery.Sdk.Domain.Services;
using Enterspeed.Delivery.Sdk.Domain.SystemTextJson;
using Enterspeed.Migrator.Enterspeed;
using Enterspeed.Migrator.Enterspeed.Contracts;
using Enterspeed.Migrator.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Enterspeed.Migrator
{
    public static class IoC
    {
        public static void RegisterEnterspeedServices(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddTransient<IApiService, ApiService>();
            serviceCollection.AddTransient<ISchemaBuilder, SchemaBuilder>();
            serviceCollection.AddTransient<IEnterspeedDeliveryService, EnterspeedDeliveryService>();
            serviceCollection.AddTransient<IEnterspeedConfigurationProvider, InMemoryConfigurationProvider>();
            serviceCollection.AddTransient<IPagesResolver, PagesResolver>();
            serviceCollection.AddTransient<EnterspeedDeliveryConnection>();
            serviceCollection.AddSingleton(new EnterspeedDeliveryConfiguration { DeliveryVersion = "2" });
            serviceCollection.AddTransient<IJsonSerializer, SystemTextJsonSerializer>();
            serviceCollection.AddOptions();

            serviceCollection.Configure<EnterspeedConfiguration>(setting => configuration.GetSection(EnterspeedConfiguration.ConfigurationKey).Bind(setting));
        }
    }
}