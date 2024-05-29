/** @type {Enterspeed.FullSchema} */
export default {
    triggers: function(context) {
      // A trigger that triggers on your source entity type(s) in your source group
      // See documentation for triggers here: https://docs.enterspeed.com/reference/js/triggers
      context.triggers('demoCms', ['contentPage'])
    },
    routes: function(sourceEntity, context) {
      // Example that generates a handle with the value of 'my-handle' to use when fetching the view from the Delivery API
      // See documentation for routes here: https://docs.enterspeed.com/reference/js/routes
      context.url(sourceEntity.url)
      context.handle(sourceEntity.url)
    },
    properties: function (sourceEntity, context) {
      // Example that returns all properties from the source entity to the view
      // See documentation for properties here: https://docs.enterspeed.com/reference/js/properties
      return {
        alias: sourceEntity.type,
        metaData: context.partial('migrationPageMetaData', sourceEntity),
        
        designation: sourceEntity.properties.content_designation,
        title: {
          editorType: "Textarea",
          value: sourceEntity.properties.content_title
        },
        abstracttitle: sourceEntity.properties.content_abstracttitle,
        abstract: {
          editorType: "Textarea",
          value: sourceEntity.properties.content_abstract
        },
        linkValue: JSON.stringify({
            id: sourceEntity.properties.content_link?.id,
            url: sourceEntity.properties.content_link?.url,
            target: sourceEntity.properties.content_link?.target,
            title: sourceEntity.properties.content_link?.text
        }),
        link: {
           editorType: "Multi URL Picker"
        },
        linktext: sourceEntity.properties.content_linktext,
        disclaimer: {
          editorType: "Richtext editor",
          value: sourceEntity.properties.content_disclaimer
        }
      }
    }
  }