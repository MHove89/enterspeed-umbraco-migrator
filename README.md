# Enterspeed migrator

## Concept
The migration tools consist of 4 things. A source CMS, Enterspeed, migration engine, and a target CMS that we are migrating data to. Enterspeed gives us the flexibility to form the data structure to our specific needs and essentially decouples the data architecture from the source CMS. 

We have been working on some concepts utilising this flexibility to make a migration from an old version of a CMS to a new version. Due to the data being decoupled, there is a bonus of being able to migrate from one system to another, since we have control over the delivery structure and output from Enterspeed.

### Purpose
This is a framework for rapid data migration between systems. It is a time-consuming and expensive process to upgrade your old system to the newest version. We provide you with tools to get started migrating your data fast, instead of upgrading your old system. 

## High-level overview
The migration tool consists of multiple parts. We will dive deeper into these here, for you to understand the working parts and give you an idea of how to work with the migration tool. 

#### Source CMS
The source CMS is the system that you want to push your data to Enterspeed from. 
You will need a way to ingest data into Enterspeed from your source system. Luckily this is already made easy for you, with some great documentation on the Ingest API. We also have some premade __[connectors](https://docs.enterspeed.com/integrations)__, so you can get started immediately.

When you have set a connection up between your source CMS and Enterspeed, you can start ingesting data into Enterspeed.

#### Enterspeed
The data now exists as source entities in Enterspeed and can be formed and modeled easily with data mapping in Enterspeed schemas. The schemas define the api endpoint and data structure for your data. This is where the decoupling is happening, since you are specifying the new structure of your data through schema mappings, and generating views based on the mappings. 

It is important to understand that we are not seeing the output of the source entities, but generating a view that is based on the schema mapping. The source entities are the raw data, the schemas are where we define how we want the output, and views are the output itself. 

To get started with Enterspeed you should start reading the __[getting started](https://docs.enterspeed.com/getting-started)__ guide, as well as reading about the key concepts of Enterspeed. 

#### Migrator
The migrator is software that should be considered as the layer between the output of Enterspeed and the target CMS.

## Digging a bit deeper
The migrator solution consists of two layers. The __`Enterspeed.Migrator`__ layer is the generic layer that calls the Enterspeed API and turns the response into usable structured data in .NET objects. 
This response is then used by the CMS-specific converter. In this example, we are showing the __`Umbraco.Migrator`__ project. This layer interprets the objects we receive from the generic layer and converts them into Umbraco-specific types and saves them in the target CMS system. 

![migrator-solution](/assets/images/migrator-solution_cmeg05yrn.png)

### Generic migration engine
The generic business logic is confined to three different services. The APIService, PageResolver and SchemaBuilders. 

![migrator-solution](/assets/images/generic-migrator.png)

The APIService has the responsibility of establishing a connection to Enterspeed. 
It contains 2 public methods. 
- `GetNavigationAsyc`
- `GetPageResponsesAsync`

`GetNavigationAsync` calls a handle that you have set up. This handle returns a list of routable urls in a tree structure. 
This response has to conform to a specific format. You can get the example schema setup for the handle __[here](/assets/schemas/)__. 

`GetPageResponsesAsync` Returns a list of responses. It calls all the URLs from the navigation data, and converts them into PageResponse objects, which wraps the delivery api response type, and also allows for page responses to contain children. Thereby keeping a 1-to-1 relation to the page structure that we are getting from Enterspeed.

Example of the PageData objects returned from GetPageResponsesAsync
```cs title="PageData object"
public class PageResponse
{
    public Dictionary<string, object> Data { get; set; }
    public List<PageResponse> Children { get; set; }
}
```
The `PageResolver` takes the page responses and converts them to page data objects. Below is the types used in the page data objects.

```cs title="PageData object"
public class PageData
{
    public MetaSchema MetaSchema { get; set; }
    public List<EnterspeedPropertyType> Properties { get; set; }
    public List<PageData> Children { get; set; }
}
```

```cs title="EnterspeedPropertyType"
public class EnterspeedPropertyType
{
    public string Name { get; set; }
    public string Alias { get; set; }
    public JsonValueKind DataType { get; set; }
    public string EditorType { get; set; }
    public object Value { get; set; }
    public List<EnterspeedPropertyType> ChildProperties { get; set; }
}
```

As you can see we are allowing this object to contain children of itself. This is to mimic a content tree structure. 
We also have the property types defined here as well. This setup allows multiple nested complex types, and can therefore support nested arrays, objects and much more. These are all mapped recursively.

We also have a `EditorType` property which is used if you want to control which editor is used in the source system. Maybe you want some string properties to be mapped to a single line text field but other string properties should be mapped to a textarea or a rich text editor.
You define this in the mapping schemas and you can see examples of it in the page schema examples __[here](/assets/schemas/Pages/)__.

If no editor type is defined a default editor will be used based on the `DataType`.

The `SchemaBuilder` converts all page data into a set of unique objects. The schemas are the sum of all the data and types that were received in the earlier steps. It has figured out how many property types Schema xx should have, their names as well as their value types. A schema is best compared to a document type in Umbraco.

Schema builder and the schema class should not be confused with a schema in Enterspeed. (naming for this might be changed in a later version to avoid confusion)

```cs title="Schema"
public class Schema
{
    public MetaSchema MetaSchema { get; set; }
    public List<EnterspeedPropertyType> Properties { get; set; }
}
```

So now we have a clean set of data, with all its unique schemas that we can work with. This is where the CMS-specific conversion starts.

### CMS-specific data converter
We have built an Umbraco 10 specific converter, that converts the output of the generic migrator to Umbraco data. 
The  __[DocumentTypeBuilder](/src/Migrators/Umbraco/Umbraco.Migrator/DocumentTypes/DocumentTypeBuilder.cs)__ receives the schemas from the generic migrator and interprets the different schemas and saves them as document types, elements, and compositions in Umbraco. Properties are all interpreted and added to before mentioned types of data in Umbraco. 

The simple types are managed through a switch statement, that converts these to the equivalent in Umbraco, unless an editor type in defined as mentioned above. 
For the complex types, we have set up a component builder handler, that requires you to create your builders. This is for scenarios where you want to convert for example grid components to element types used in a blocklist or nested content. 
There are quite a few samples __[here](/src/Migrators/Umbraco/Umbraco.Migrator/DocumentTypes/Components/Builders/)__.

You define an alias in the component builder and when the converter hits an alias with that specific value, your custom component builder will get executed.


## Getting started moving data into Umbraco 10+
You need to pull down the source code and reference your Umbraco project to the `Umbraco.Migrator.Package` project. This should give you the required references to get started. Still pretty early, so no official package has been created for this project. 

#### App settings
For the migrator to work, we need to specify a few settings in the target CMS (in this case Umbraco 10). 
These are all values that are used for the CMS-specific converter to interpret the data. The data is set in the app-settings.json file.

```json 
{
  "EnterspeedConfiguration": {
    "ApiKey": "", // key for the Delivery API endpoint
    "NavigationHandle": "navigation-en-us", // navigation handle route
    "MigrationPageMetaData": "migrationPageMetaData", // alias of the property that contains meta data for the specific page
    "ComponentPropertyTypeKeys": [ // alias of the component types that you have build component builders for
      "rte",
      "embed",
      "headline",
      "macro",
      "media",
      "quote"
    ]
  },
  "UmbracoMigrationConfiguration": {
    "RootDocType": "home", // alias of your root document type
    "ContentPropertyAlias": "blockListContent", // blockList component (Currently only blocklist are supported)
    "CompositionKeys": [
      "basePage", // alias of your properties
      "footer" // alias of your properties
    ],
    "StartIds": {
      "SourceId": "", // the id of the start entity in the source system
      "TargetParentId": 0 // the id of the parent entity in the target system
    }
  }
}
```

All alias values are property names that are a part of the page responses from Enterspeed. We are for example telling the converter when there is a match for `basePage`, take all the properties of this object, and map it into a composition type. 
The fact that a property of this alias exists in the page response, also tells the converter that this page should have the `basePage` composition assigned. All properties in the base page object will be added to the base page composition. 
Below is an example of a page response. This should connect the dots.

```json 
{
    "meta": {
        "status": 200,
        "redirect": null
    },
    "route": {
        "type": "home",
        "basePage": {
            "hideInNavigation": false
        },
        "footer": {
            "footerCtaCaption": "Read All on the Blog",
            "footerHeader": "Umbraco Demo",
            "footerDescription": "Curabitur arcu erat, accumsan id imperdiet et, porttitor at sem. Curabitur arcu erat, accumsan id imperdiet et, porttitor at sem. Vivamus suscipit tortor eget felis porttitor volutpat",
            "footerAddress": "Umbraco HQ - Unicorn Square - Haubergsvej 1 - 5000 Odense C - Denmark - +45 70 26 11 62"
        },
        "migrationPageMetaData": {
            "sourceEntityAlias": "home",
            "sourceEntityName": "home",
            "contentName": "Home"
          }
    },
    "views": {}
}
```
#### Navigation handle
The `NavigationHandle` is very important. You need to create a handle in Enterspeed, that returns all routable views, that you would like to migrate into Umbraco 10. This handle has to conform to some specific requirements for this to work. You can get a sample of the schema setup for the handle __[here](/assets/schemas/)__.

#### Schema requirements
Some meta-data is required for this to work. As you can see in the example, we have the `migrationPageMetaData` as we defined earlier. This contains 3 properties: `sourceEntityAlias`, `sourceEntityName` and `contentName`. These 3 properties are required for the migration engine to work. It's used to create the unique schema types and later on Document Types for Umbraco. 

This means that you would have to create a Partial schema in Enterspeed and reference the metadata in all routed responses. 
You can call the meta-data object what you want, as long as you reference it correctly in appsettings and add the required properties. 


##### Page schema and migrationPageMetaData 
First of let´s show an example of a schema for a routeable page. As you can see the schema is referencing the before-mentioned migrationPageMetaData partial. 

##### Page schema
```json
{
	"triggers": {
		"demoCMS": [
			"contentPage"
		]
	},
	"route": {
		"url": "{url}"
	},
	"properties": {
		"type": "{type}",
		"pageTitle": "{p.pageTitle}",
		"sections": {
			"type": "array",
			"input": "{p.bodyText.sections}",
			"items": {
				"type": "partial",
				"input": "{item}",
				"alias": "section"
			}
		},
		"basePage": {
			"type": "partial",
			"input": "{root}",
			"alias": "basePage"
		},
		"migrationPageMetaData": {
			"type": "partial",
			"input": "{root}",
			"alias": "migrationPageMetaData"
		}
	}
}

```

##### Partial schema example of migrationPageMetaData
```json
{
	"properties": {
		"sourceEntityAlias": "{item.type}",
		"sourceEntityName": "{item.type}",
		"contentName": {
			"type": "string",
			"value": {
				"$exp": "{item.p.metaData.name}"
			}
		},
		"culture": "{item.p.metaData.culture}",
		"createDate": "{item.p.metaData.createDate}",
		"updateDate": "{item.p.metaData.updateDate}",
		"parentId": "{item.originParentId}",
		"url": "{item.url}",
		"contentPath": {
			"type": "array",
			"input": "{item.p.metaData.nodePath}",
			"var": "path",
			"items": {
				"type": "object",
				"properties": {
					"id": "{path}"
				}
			}
		}
	}
}
```

#### Working with complex data
Imagine that you have json objects of complex data, that you would like to store in your target cms (this case Umbraco). You would need to tell the cms specific converter what to do with it. This is done by adding the alias to ComponentPropertyTypeKeys in appsettings. Furthermore, you would need to create your ComponentBuilder for this. This example has been used to move a grid editor called Quote, from Umbraco 7 and moved it to an element type in Umbraco 10, to be used in a block list editor.

```cs
public class QuoteComponentBuilder : ComponentBuilder
{
    private const string PropertyAlias = "quote";

    public QuoteComponentBuilder(IContentTypeService contentTypeService, IShortStringHelper shortStringHelper, IDataTypeService dataTypeService)
        : base(contentTypeService, shortStringHelper, dataTypeService)
    {
        Alias = "quote";
        Name = "Quote";
    }

    public override bool CanBuild(string propertyAlias)
    {
        return PropertyAlias.Equals(propertyAlias);
    }

    public override void Build()
    {
        AddProperty("quote", "Quote", Constants.DataTypes.Textarea);
        Save();
    }

    public override object MapData(EnterspeedPropertyType enterspeedProperty)
    {
        var data = new Dictionary<string, object>();
        var quote = GetValue(enterspeedProperty, "value").ToString();
        data.Add("quote", quote);

        return data;
    }
}
```

#### Data importer
When importing data, you are getting page data objects in the same structure as your navigation structure. Page data objects contain meta-data. The sourceEntityAlias value will match up against your page document type, that's why the converter knows how to save the data in Umbraco. The page data objects also contain all properties + their values, these will also match up against their corresponding properties in Umbraco based on the alias, and save them accordingly. 

#### Starting an import
Importing consists of 2 steps 
- Importing document type 
- Importing data

Navigate to settings -> Enterspeed data migrator (as shown in the picture)
Press `Import document` types and wait for all document types to be imported.
When this process is done, you can execute `Import` content.

![migrator-solution](/assets/images/import-document-types.png)
