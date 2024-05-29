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
      context.handle(`navigation-${sourceEntity.properties.metaData.culture}`)
    },
    properties: function (sourceEntity, context) {
      // Example that returns all properties from the source entity to the view
      // See documentation for properties here: https://docs.enterspeed.com/reference/js/properties
      return {
        self: context.reference('navigationLinkItem').self(),
        children: context.reference('navigationItem').children()
      }
    }
  }