﻿using Enterspeed.Umbraco.Migrator.Enterspeed.Contracts;
using Enterspeed.Umbraco.Migrator.Models;
using Enterspeed.Umbraco.Migrator.Settings;
using Enterspeed.Umbraco.Migrator.Umbraco.Contracts;
using Microsoft.Extensions.Logging;

namespace Enterspeed.Umbraco.Migrator.Umbraco
{
    public class DocumentTypeBuilder : IDocumentTypeBuilder
    {
        private readonly ISchemaImporter _schemaImporter;
        private readonly EnterspeedConfiguration _enterspeedConfiguration;
        private readonly ILogger<DocumentTypeBuilder> _logger;

        public DocumentTypeBuilder(ISchemaImporter schemaImporter,
            ILogger<DocumentTypeBuilder> logger, EnterspeedConfiguration enterspeedConfiguration)
        {
            _schemaImporter = schemaImporter;
            _enterspeedConfiguration = enterspeedConfiguration;
            _logger = logger;
        }

        public async Task<IEnumerable<UmbracoDoctype>> BuildDoctypesAsync(List<Schema> schemas)
        {
            try
            {
                if (schemas != null && schemas.Any())
                    return schemas.Select(s => new UmbracoDoctype(s));

                _logger.LogWarning("No schemas found in import");
                return new List<UmbracoDoctype>();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Something went wrong when building schemas");
                throw;
            }
        }
    }
}
