using System;
using System.Linq;
using System.Threading.Tasks;
using Enterspeed.Migrator.Enterspeed.Contracts;
using Microsoft.Extensions.Logging;
using Umbraco.Migrator.Content;
using Umbraco.Migrator.DocumentTypes;

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

        public UmbracoMigratorService(
            ILogger<UmbracoMigratorService> logger,
            IPagesResolver pagesResolver,
            IApiService apiService,
            ISchemaBuilder schemaBuilder,
            IDocumentTypeBuilder documentTypeBuilder,
            IContentBuilder contentBuilder)
        {
            _logger = logger;
            _pagesResolver = pagesResolver;
            _apiService = apiService;
            _schemaBuilder = schemaBuilder;
            _documentTypeBuilder = documentTypeBuilder;
            _contentBuilder = contentBuilder;
        }

        public async Task ImportDocumentTypesAsync()
        {
            try
            {
                _logger.LogInformation("Starting importing document types");

                // Enterspeed responses
                _logger.LogInformation("Loading navigation from Enterspeed");
                var navigation = await _apiService.GetNavigationAsync();

                _logger.LogInformation("Loading pages from Enterspeed");
                var rootLevelResponse = await _apiService.GetPageResponsesAsync(navigation);

                // All pages with all data
                _logger.LogInformation("Resolving pages");
                var pages = _pagesResolver.ResolveFromRoot(rootLevelResponse).ToList();

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

                _logger.LogInformation("Loading pages from Enterspeed");
                var rootLevelResponse = await _apiService.GetPageResponsesAsync(navigation);

                // All pages with all data
                _logger.LogInformation("Resolving pages");
                var pages = _pagesResolver.ResolveFromRoot(rootLevelResponse).Where(p => p.MetaSchema != null).ToList();

                // Build content based on pages
                _logger.LogInformation("Building Umbraco content pages");
                _contentBuilder.BuildContentPages(pages);

                _logger.LogInformation("Finish importing content pages");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "something went wrong when seeding data");
                throw;
            }
        }
    }
}