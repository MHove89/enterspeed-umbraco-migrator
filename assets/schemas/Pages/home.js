/** @type {Enterspeed.FullSchema} */
export default {
    triggers: function(context) {
      // A trigger that triggers on your source entity type(s) in your source group
      // See documentation for triggers here: https://docs.enterspeed.com/reference/js/triggers
      context.triggers('demoCms', ['home'])
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
        
        headerimageValue: sourceEntity.properties.header_headerimage?.id,
        headerimage: {
           editorType: "Media Picker"
        },
        headertitle: sourceEntity.properties.header_headertitle,
        metacategory: sourceEntity.properties.metadata_metacategory,
        metadescription: sourceEntity.properties.metadata_metadescription,
        browsertitle: sourceEntity.properties.navigation_browsertitle,
        warningbannercontent: {
          editorType: "Richtext editor",
          value: sourceEntity.properties.warningbanner_warningbannercontent
        },
        excludefromsearch: sourceEntity.properties.search_excludefromsearch
      }
    }
  }