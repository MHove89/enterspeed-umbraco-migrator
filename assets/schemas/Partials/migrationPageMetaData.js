/** @type {Enterspeed.PartialSchema} */
export default {
    properties: function (input, context) {
      // Example that returns all properties from the input object to the view
      // See documentation for properties here: https://docs.enterspeed.com/reference/js/properties
      return {
        sourceEntityAlias: input.type,
        sourceEntityName: input.type,
        contentName: input.properties.metaData.name
      }
    }
  }