using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enterspeed.Migrator.Enterspeed.Contracts;
using Enterspeed.Migrator.Models.Response;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Migrator.Content;
using Umbraco.Migrator.DocumentTypes;
using Umbraco.Migrator.Settings;

namespace Umbraco.Migrator
{
    public class UmbracoMigratorService : IUmbracoMigratorService
    {
        private readonly IPagesResolver _pagesResolver;
        private readonly IDocumentTypeBuilder _documentTypeBuilder;
        private readonly IApiService _apiService;
        private readonly ILogger<UmbracoMigratorService> _logger;
        private readonly ISchemaBuilder _schemaBuilder;
        private readonly IContentBuilder _contentBuilder; 
        private readonly UmbracoMigrationConfiguration _umbracoMigrationConfiguration;

        public UmbracoMigratorService(
            ILogger<UmbracoMigratorService> logger,
            IPagesResolver pagesResolver,
            IApiService apiService,
            ISchemaBuilder schemaBuilder,
            IDocumentTypeBuilder documentTypeBuilder,
            IContentBuilder contentBuilder,
            IOptions<UmbracoMigrationConfiguration> umbracoMigrationConfiguration)
        {
            _logger = logger;
            _pagesResolver = pagesResolver;
            _apiService = apiService;
            _schemaBuilder = schemaBuilder;
            _documentTypeBuilder = documentTypeBuilder;
            _contentBuilder = contentBuilder;
            _umbracoMigrationConfiguration = umbracoMigrationConfiguration?.Value;
        }

        public async Task ImportDocumentTypesAsync()
        {
            try
            {
                _logger.LogInformation("Starting importing document types");

                // Enterspeed navigation response
                _logger.LogInformation("Loading navigation from Enterspeed");
                var navigation = await _apiService.GetNavigationAsync();

                _logger.LogInformation("Finding start page");
                var startPage = FindStartPage(navigation, _umbracoMigrationConfiguration.StartIds.SourceId);

                _logger.LogInformation("Loading pages from Enterspeed");
                var pageResponse = await _apiService.GetPageResponsesAsync(startPage);

                // All pages with all data
                _logger.LogInformation("Resolving pages");
                var pages = _pagesResolver.ResolveFromRoot(pageResponse).ToList();

                // Mapped out data structures in schemas based on the pages
                _logger.LogInformation("Building page schemas");
                var pageSchemas = _schemaBuilder.BuildPageSchemas(pages);

                // Build document types based on schemas
                _logger.LogInformation("Building Umbraco doc types");
                _documentTypeBuilder.BuildDocTypes(pageSchemas);

                _logger.LogInformation("Finished importing document types");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "something went wrong when building document types");
                throw;
            }
        }

        public async Task ImportDataAsync()
        {
            try
            {
                _logger.LogInformation("Starting importing content pages");

                // Enterspeed responses
                _logger.LogInformation("Loading navigation from Enterspeed");
                var navigation = await _apiService.GetNavigationAsync();

                LimitTreeDepth(navigation, _umbracoMigrationConfiguration.ContentImportMaxTreeLevel);

                _logger.LogInformation("Finding start page");
                var startPage = FindStartPage(navigation, _umbracoMigrationConfiguration.StartIds.SourceId);

                _logger.LogInformation("Loading pages from Enterspeed");
                var pageResponse = await _apiService.GetPageResponsesAsync(startPage);

                // All pages with all data
                _logger.LogInformation("Resolving pages");
                var pages = _pagesResolver.ResolveFromRoot(pageResponse).Where(p => p.MetaSchema != null).ToList();

                var umbracoStartParentNode = _contentBuilder.FindUmbracoStartParentNode(_umbracoMigrationConfiguration.StartIds.TargetParentId);

                // Build content based on pages
                _logger.LogInformation("Building Umbraco content pages");
                _contentBuilder.BuildContentPages(pages, umbracoStartParentNode);

                _logger.LogInformation("Finish importing content pages");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "something went wrong when seeding data");
                throw;
            }
        }

        private static void LimitTreeDepth(EnterspeedResponse navigationResponse, int maxLevel)
        {
            ArgumentNullException.ThrowIfNull(navigationResponse?.Views?.Navigation?.Self);

            if (maxLevel == 0)
            {
                return;
            }

            if (maxLevel == 1)
            {
                navigationResponse.Views.Navigation.Children = new List<Item>();
                return;
            }

            var currentLevel = 1;
            if (navigationResponse.Views.Navigation.Children.Any())
            {
                LimitTreeDepth(navigationResponse.Views.Navigation.Children, maxLevel, currentLevel);
            }
        }

        private static void LimitTreeDepth(List<Item> children, int maxLevel, int currentLevel)
        {
            currentLevel += 1;
            foreach (var child in children.Where(x => x is not null))
            {
                if (currentLevel >= maxLevel)
                {
                    child.Children = new List<Item>();
                }
                else
                {
                    LimitTreeDepth(child.Children, maxLevel, currentLevel);
                }
            }
        }

        private static Item FindStartPage(EnterspeedResponse navigationResponse, string startId)
        {
            if (navigationResponse?.Views?.Navigation?.Self is null)
            {
                throw new ArgumentNullException(nameof(navigationResponse), "navigationResponse.Views.Navigation.Self must not be null");
            }

            if (string.IsNullOrWhiteSpace(startId))
            {
                return navigationResponse.Views.Navigation;
            }

            if (navigationResponse.Views.Navigation.Self.SourceId == startId)
            {
                return navigationResponse.Views.Navigation;
            }

            Item startPage = null;
            if (navigationResponse.Views.Navigation.Children.Any())
            {
                startPage = FindStartPage(navigationResponse.Views.Navigation.Children, startId);
            }

            return startPage ?? navigationResponse.Views.Navigation;
        }

        private static Item FindStartPage(List<Item> children, string startId)
        {
            foreach (var child in children)
            {
                if (child.Self?.SourceId == startId)
                {
                    return child;
                }

                if (child.Children.Any())
                {
                    var startPage = FindStartPage(child.Children, startId);
                    if (startPage is not null)
                    {
                        return startPage;
                    }
                }
            }

            return null;
        }
    }
}