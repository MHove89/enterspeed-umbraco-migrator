using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Enterspeed.Migrator.Constants;
using Enterspeed.Migrator.Enterspeed.Contracts;
using Enterspeed.Migrator.Models;
using Enterspeed.Migrator.Models.Response;
using Enterspeed.Migrator.Settings;
using Enterspeed.Migrator.ValueTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterspeed.Migrator.Enterspeed
{
    public class PagesResolver : IPagesResolver
    {
        private readonly EnterspeedConfiguration _configuration;
        private readonly ILogger<PagesResolver> _logger;

        public PagesResolver(
            IOptions<EnterspeedConfiguration> configuration,
            ILogger<PagesResolver> logger)
        {
            _logger = logger;
            _configuration = configuration?.Value;
        }

        /// <summary>
        /// Gets meta data objects for pages
        /// </summary>
        /// <param name="deliveryResponse"></param>ƒ
        /// <returns></returns>
        public MetaSchema GetMetaData(Dictionary<string, object> data)
        {
            if (data == null)
            {
                _logger.LogError("Route was not found for");
                return null;
            }

            if (!data.TryGetValue(_configuration.MigrationPageMetaData, out var migrationPageMetaData))
            {
                throw new NullReferenceException($"{_configuration.MigrationPageMetaData} not found on the schema for {JsonSerializer.Serialize(data)}");
            }

            var serialized = JsonSerializer.Serialize(migrationPageMetaData);
            var parsedMetaData = JsonSerializer.Deserialize<MetaSchema>(serialized);
            if (parsedMetaData != null)
            {
                return parsedMetaData;
            }

            throw new NullReferenceException("Something went wrong when trying to return metadata ");
        }

        public List<PageData> ResolveFromRoot(PageResponse pageResponse)
        {
            var pageEntityTypes = new List<PageData>();
            if (pageResponse.Data != null)
            {
                var page = GetPageData(pageResponse.Data);

                foreach (var deliveryResponse in pageResponse.Children)
                {
                    if (deliveryResponse.Data != null)
                    {
                        page.Children.AddRange(ResolveFromRoot(deliveryResponse));
                    }
                }

                pageEntityTypes.Add(page);
                return pageEntityTypes;
            }

            return null;
        }

        private PageData GetPageData(Dictionary<string, object> data)
        {
            var routeSerialized = JsonSerializer.SerializeToElement(data);

            var pageEntityType = new PageData
            {
                MetaSchema = GetMetaData(data)
            };

            MapData(pageEntityType, routeSerialized);

            return pageEntityType;
        }

        /// <summary>
        /// An element can be multiple things. 
        /// It is essentially a "chunk" of a json response.
        /// </summary>
        /// <param name="pageData"></param>
        /// <param name="element">A "chunk" of a json response. It can be a full page response, or individual parts of the response, after traversing has happened</param>
        /// <param name="parentEnterspeedProperty"></param>
        private void MapData(PageData pageData, JsonElement element, EnterspeedPropertyType parentEnterspeedProperty = null)
        {
            if (element.ValueKind != JsonValueKind.Null)
            {
                var elementObject = element.EnumerateObject();

                // We match up against the appsettings configuration to check if we hit a component. If match is true, a property is assigned to the object. This property is called "isComponent".
                // This will be used to conditionally resolve component builders at a later stage. Note we are ONLY looking for properties directly on the object. No traversing is happening 
                // here, this is why the property called Alias on the component should be present directly as a property on the object.
                // Example of a json element that is returned from Enterspeed (the element parameter in this method)
                // JsonElement
                // componentObject {
                // Alias = "rteComponent", // This is the value assigned in the below logic
                // RteContent = "Lots of content",
                // Image = Complex json object
                //}
                var alias = elementObject.GetEnumerator().FirstOrDefault(p => p.Name == EnterspeedPropertyConstants.AliasOf.Alias);
                var isComponent = IsComponent(alias);

                if (parentEnterspeedProperty != null && isComponent)
                {
                    MarkObjectAsComponent(parentEnterspeedProperty);
                }

                foreach (var jsonProperty in element.EnumerateObject())
                {
                    MapData(pageData, jsonProperty, parentEnterspeedProperty);
                }
            }
        }

        private static void MarkObjectAsComponent(EnterspeedPropertyType parentEnterspeedProperty)
        {
            parentEnterspeedProperty.ChildProperties.Add(new EnterspeedPropertyType
            {
                Name = EnterspeedPropertyConstants.IsComponentName,
                Alias = EnterspeedPropertyConstants.IsComponentAlias,
                Value = true
            });
        }

        private bool IsComponent(JsonProperty alias)
        {
            return _configuration.ComponentPropertyTypeKeys.Any(p => p == alias.Value.ToString());
        }

        private void MapData(PageData pageData, JsonProperty jsonProperty, EnterspeedPropertyType parentEnterspeedProperty = null)
        {
            switch (jsonProperty.Value.ValueKind)
            {
                case JsonValueKind.Object:
                    CreateObjectType(pageData, jsonProperty, parentEnterspeedProperty);
                    break;
                case JsonValueKind.Array:
                    CreateArrayType(pageData, jsonProperty, parentEnterspeedProperty);
                    break;
                default:
                    CreateSimpleType(pageData, jsonProperty, parentEnterspeedProperty);
                    break;
            }
        }

        private void CreateArrayType(PageData pageData, JsonProperty jsonProperty, EnterspeedPropertyType parentEnterspeedProperty = null)
        {
            if (jsonProperty.Value.ValueKind == JsonValueKind.Array && jsonProperty.Value.GetArrayLength() > 0)
            {
                var currentProperty = new EnterspeedPropertyType(jsonProperty);
                if (parentEnterspeedProperty != null)
                {
                    parentEnterspeedProperty.ChildProperties.Add(currentProperty);
                }

                var arrayOfElements = jsonProperty.Value.EnumerateArray();
                foreach (var element in arrayOfElements)
                {
                    if (element.ValueKind == JsonValueKind.Object)
                    {
                        var objectOfElement = element.EnumerateObject();
                        var newArrayItem = new EnterspeedPropertyType()
                        {
                            Name = "arrayObject",
                            Alias = "arrayObject",
                            DataType = JsonValueKind.Object,
                            Value = objectOfElement
                        };

                        // Add arrayitem directly to array property
                        currentProperty.ChildProperties.Add(newArrayItem);
                        MapData(pageData, element, newArrayItem);
                    }
                }

                // Ensure that we do not add nested properties to the root level of the properties for the page.
                if (parentEnterspeedProperty == null)
                {
                    pageData.Properties.Add(currentProperty);
                }
            }
        }

        private void CreateObjectType(PageData pageData, JsonProperty jsonProperty, EnterspeedPropertyType parentEnterspeedProperty = null)
        {
            // If is a complex type 
            if (jsonProperty.Value.ValueKind == JsonValueKind.Object)
            {
                var elmenent = jsonProperty.Value;
                var currentProperty = new EnterspeedPropertyType(jsonProperty);

                parentEnterspeedProperty?.ChildProperties.Add(currentProperty);

                if (string.IsNullOrWhiteSpace(currentProperty.EditorType))
                {
                    MapData(pageData, elmenent, currentProperty);
                }

                if (parentEnterspeedProperty is null)
                {
                    pageData.Properties.Add(currentProperty);
                }
            }
        }

        private static void CreateSimpleType(PageData pageData, JsonProperty jsonProperty, EnterspeedPropertyType parentEnterspeedProperty = null)
        {
            var property = new EnterspeedPropertyType(jsonProperty);
            if (parentEnterspeedProperty != null)
            {
                parentEnterspeedProperty.ChildProperties.Add(property);
            }
            else
            {
                pageData.Properties.Add(property);
            }
        }
    }
}