using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Enterspeed.Migrator.Models;
using Enterspeed.Migrator.Settings;
using Enterspeed.Migrator.ValueTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;
using Umbraco.Migrator.DocumentTypes.Components;
using Umbraco.Migrator.Extensions;
using Umbraco.Migrator.Settings;

namespace Umbraco.Migrator.DocumentTypes
{
    public class DocumentTypeBuilder : IDocumentTypeBuilder
    {
        private readonly ILogger<DocumentTypeBuilder> _logger;
        private readonly IContentTypeService _contentTypeService;
        private readonly IShortStringHelper _shortStringHelper;
        private readonly IDataTypeService _dataTypeService;
        private readonly IConfigurationEditorJsonSerializer _configurationEditorJsonSerializer;
        private readonly List<IDataType> _dataTypes;
        private readonly IComponentBuilderHandler _componentBuilderHandler;
        private readonly BlockListPropertyEditor _blockListPropertyEditor;
        private readonly UmbracoMigrationConfiguration _umbracoMigrationConfiguration;
        private readonly IEnumerable<string> _contentTypeAliasList;
        private const string PagesFolderName = "Migrated Page Types";
        private const string CompositionsFolderName = "Migrated Compositions";
        private const string ComponentsFolderName = "Migrated Components";
        private const string DataTypeFolderName = "Migrated Data Types";
        private const string BlockListName = "BlockList.Custom";
        private readonly EnterspeedConfiguration _enterspeedConfiguration;

        public DocumentTypeBuilder(ILogger<DocumentTypeBuilder> logger,
            IContentTypeService contentTypeService,
            IShortStringHelper shortStringHelper,
            IDataTypeService dataTypeService,
            BlockListPropertyEditor blockListPropertyEditor,
            IConfigurationEditorJsonSerializer configurationEditorJsonSerializer,
            IOptions<UmbracoMigrationConfiguration> umbracoMigrationConfiguration,
            IComponentBuilderHandler componentBuilderHandler,
            IOptions<EnterspeedConfiguration> enterspeedConfiguration)
        {
            _logger = logger;
            _contentTypeService = contentTypeService;
            _shortStringHelper = shortStringHelper;
            _dataTypeService = dataTypeService;
            _blockListPropertyEditor = blockListPropertyEditor;
            _configurationEditorJsonSerializer = configurationEditorJsonSerializer;
            _umbracoMigrationConfiguration = umbracoMigrationConfiguration?.Value;
            _dataTypes = _dataTypeService.GetAll().ToList();
            _componentBuilderHandler = componentBuilderHandler;
            _contentTypeAliasList = _contentTypeService.GetAllContentTypeAliases();
            _enterspeedConfiguration = enterspeedConfiguration?.Value;
        }

        public void BuildDocTypes(Schemas schemas)
        {
            try
            {
                var pagesFolder = GetOrCreateDocumentTypeFolder(PagesFolderName);
                var componentsFolder = GetOrCreateDocumentTypeFolder(ComponentsFolderName);
                var compositionsFolder = GetOrCreateDocumentTypeFolder(CompositionsFolderName);
                var dataTypeFolder = GetOrCreateDataTypeFolder(DataTypeFolderName);

                // Build components
                foreach (var componentAlias in _enterspeedConfiguration.ComponentPropertyTypeKeys)
                {
                    _componentBuilderHandler.BuildComponent(componentAlias, componentsFolder.Id);
                }

                // TODO: Assumption. This needs to be handled in a different way.
                // Create block list data type, and add config with all element types
                CreateBlockListDataType(dataTypeFolder);

                // Create pages
                foreach (var page in schemas.Pages)
                {
                    CreatePageDocType(page, pagesFolder, compositionsFolder);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Something went wrong when building schemas");
                throw;
            }
        }

        private void CreateBlockListDataType(IEntity dataTypeFolder)
        {
            // Create data type
            var dataType = new DataType(_blockListPropertyEditor, _configurationEditorJsonSerializer, dataTypeFolder.Id)
            {
                Name = BlockListName
            };

            // Create element types
            var elementTypes = _contentTypeService.GetAll().Where(c => c.IsElement).Select(block =>
                new BlockListConfiguration.BlockConfiguration()
                {
                    ContentElementTypeKey = block.Key,
                    Label = block.Name
                }).ToArray();

            // Configure datatype
            dataType.Configuration = new BlockListConfiguration()
            {
                Blocks = elementTypes
            };

            // Save
            _dataTypeService.Save(dataType);
            _dataTypes.Add(dataType);
        }

        private void CreatePageDocType(Schema page, IEntity pagesFolder, IEntity compositionsFolder)
        {
            if (!_contentTypeAliasList.Any(c => c.Equals(page.MetaSchema.SourceEntityAlias)))
            {
                // Build new doc type
                var newPageDocumentType = BuildNewPageDocType(page, pagesFolder);

                // Add the rest of the properties
                AddProperties(page, newPageDocumentType, compositionsFolder);

                // TODO: Ensure that this is handled in another way. This is an assumption!
                newPageDocumentType.AddPropertyType(new PropertyType(_shortStringHelper,
                    _dataTypes.Find(d => d.Name == BlockListName))
                {
                    Name = _umbracoMigrationConfiguration.ContentPropertyAlias.ToUmbracoName(),
                    Alias = _umbracoMigrationConfiguration.ContentPropertyAlias
                }, "content");

                // Save the new document type
                _contentTypeService.Save(newPageDocumentType);
            }
        }

        private ContentType BuildNewPageDocType(Schema page, IEntity pagesFolder)
        {
            var newPageDocumentType = new ContentType(_shortStringHelper, pagesFolder.Id)
            {
                Alias = page.MetaSchema.SourceEntityAlias.ToFirstLowerInvariant(),
                Name = page.MetaSchema.SourceEntityName.ToUmbracoName(),
                AllowedAsRoot = string.Equals(page.MetaSchema.SourceEntityAlias, _umbracoMigrationConfiguration.RootDocType,
                    StringComparison.InvariantCultureIgnoreCase),
                Icon = "icon-item-arrangement"
            };

            return newPageDocumentType;
        }

        private IContentType CreateComposition(IEntity compositionsFolder, EnterspeedPropertyType enterspeedProperty)
        {
            var composition = new ContentType(_shortStringHelper, compositionsFolder.Id)
            {
                Alias = enterspeedProperty.Alias,
                Name = enterspeedProperty.Name.ToFirstUpper(),
                IsElement = true,
                Icon = "icon-item-arrangement"
            };

            return composition;
        }

        private void AddProperties(Schema schema, ContentType pageDocumentType, IEntity compositionsFolder)
        {
            if (_dataTypes?.Any() == true)
            {
                foreach (var enterspeedProperty in schema.Properties)
                {
                    var isComposition = _umbracoMigrationConfiguration.CompositionKeys.Any(p => p == enterspeedProperty.Alias);
                    if (isComposition)
                    {
                        HandleComposition(pageDocumentType, compositionsFolder, enterspeedProperty);
                    }

                    AddProperties(enterspeedProperty, pageDocumentType);
                }
            }
        }

        private void HandleComposition(ContentType pageDocumentType, IEntity compositionsFolder, EnterspeedPropertyType enterspeedProperty)
        {
            var compositionExists = pageDocumentType.ContentTypeCompositionExists(enterspeedProperty.Alias);
            if (!compositionExists)
            {
                // Check if already exists
                var composition = _contentTypeService.Get(enterspeedProperty.Alias);
                if (composition == null)
                {
                    composition = CreateComposition(compositionsFolder, enterspeedProperty);

                    // Check if any properties on composition
                    if (!enterspeedProperty.ChildProperties.Any()) return;

                    // Add properties to composition
                    foreach (var childProperty in enterspeedProperty.ChildProperties)
                    {
                        AddProperties(childProperty, composition, true);
                    }

                    _contentTypeService.Save(composition);
                }

                pageDocumentType.AddContentType(composition);
            }
        }

        private void AddProperties(EnterspeedPropertyType enterspeedProperty, IContentTypeBase documentType, bool isComposition = false)
        {
            IDataType dataType = null;
            if (!string.IsNullOrWhiteSpace(enterspeedProperty.EditorType))
            {
                dataType = _dataTypes.Find(d => string.Equals(d.Name, enterspeedProperty.EditorType, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                switch (enterspeedProperty.DataType)
                {
                    case JsonValueKind.Undefined:
                        _logger.LogError("Property type is undefined");
                        break;
                    case JsonValueKind.Object:
                        // Check if we are a component
                        break;
                    case JsonValueKind.Array:
                        // Check if we are a component or simple type of array
                        var elementValueKind = GetValueKindOfFirstElementInArray(enterspeedProperty);

                        if (elementValueKind is JsonValueKind.String or JsonValueKind.Number)
                        {
                            dataType = _dataTypes.Find(d => d.Name?.ToLower() == "tags");
                        }

                        break;
                    case JsonValueKind.String:
                        dataType = _dataTypes.Find(d => d.Name?.ToLower() == "textstring");
                        break;
                    case JsonValueKind.Number:
                        dataType = _dataTypes.Find(d => d.Name?.ToLower() == "number");
                        break;
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        dataType = _dataTypes.Find(d => d.Name?.ToLower() == "true/false");
                        break;
                    case JsonValueKind.Null:
                        _logger.LogError("Property type is null");
                        break;
                }
            }

            if (dataType != null)
            {
                if (isComposition)
                {
                    documentType.AddPropertyType(new PropertyType(_shortStringHelper, dataType)
                    {
                        Name = enterspeedProperty.Name.ToUmbracoName(),
                        Alias = enterspeedProperty.Alias.ToFirstLowerInvariant()
                    }, documentType.Alias + "Content", documentType.Name.ToUmbracoName(new List<string>
                    {
                        "Content"
                    }));
                }
                else
                {
                    documentType.AddPropertyType(new PropertyType(_shortStringHelper, dataType)
                    {
                        Name = enterspeedProperty.Name.ToUmbracoName(),
                        Alias = enterspeedProperty.Alias.ToFirstLowerInvariant(),
                    }, "content", "Page content");
                }
            }
        }

        private EntityContainer GetOrCreateDocumentTypeFolder(string folderName)
        {
            var createContainerAttempt = _contentTypeService.CreateContainer(-1, Guid.NewGuid(), folderName);
            if (createContainerAttempt.Success)
            {
                var containerSaved = _contentTypeService.SaveContainer(createContainerAttempt.Result.Entity);
                if (containerSaved.Success)
                {
                    return createContainerAttempt.Result.Entity;
                }

                _logger.LogError(containerSaved.Exception, $"Something went wrong when saving folder {folderName}");
            }

            var existingContainer = _contentTypeService.GetContainers(folderName, 1).FirstOrDefault();
            return existingContainer;
        }

        private EntityContainer GetOrCreateDataTypeFolder(string folderName)
        {
            var createContainerAttempt = _dataTypeService.CreateContainer(-1, Guid.NewGuid(), folderName);
            if (createContainerAttempt.Success)
            {
                var containerSaved = _dataTypeService.SaveContainer(createContainerAttempt.Result.Entity);
                if (containerSaved.Success)
                {
                    return createContainerAttempt.Result.Entity;
                }

                _logger.LogError(containerSaved.Exception, $"Something went wrong when saving folder {folderName}");
            }

            var existingContainer = _dataTypeService.GetContainers(folderName, 1).FirstOrDefault();
            return existingContainer;
        }

        private static JsonValueKind GetValueKindOfFirstElementInArray(EnterspeedPropertyType enterspeedProperty)
        {
            var valueKind = JsonValueKind.Undefined;
            var array = ((JsonElement)enterspeedProperty.Value).EnumerateArray().ToArray();

            if (array.Any())
            {
                valueKind = array.First().ValueKind;
            }

            return valueKind;
        }
    }
}