/** @type {Enterspeed.FullSchema} */
export default {
    triggers: function(context) {
      // A trigger that triggers on your source entity type(s) in your source group
      // See documentation for triggers here: https://docs.enterspeed.com/reference/js/triggers
      context.triggers('demoCms', [
                                    "home",
                                    "contentPage",
                                    "products",
                                    "product",
                                    "blog",
                                    "blogpost",
                                    "people",
                                    "person"
                                  ])
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