using Enterspeed.Migrator.Constants;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Enterspeed.Migrator.EditorTypes;

namespace Enterspeed.Migrator.ValueTypes
{
    public class EnterspeedPropertyType
    {
        public EnterspeedPropertyType(JsonProperty jsonProperty)
        {
            Value = jsonProperty.Value;
            Alias = jsonProperty.Name;
            Name = jsonProperty.Name;
            DataType = jsonProperty.Value.ValueKind;
            ChildProperties = new List<EnterspeedPropertyType>();

            if (TryGetAsEditorValue(jsonProperty, out var editorValue))
            {
                EditorType = editorValue.EditorType;
                Value = editorValue.Value;
            }
        }

        public EnterspeedPropertyType()
        {
            ChildProperties = new List<EnterspeedPropertyType>();
        }

        public string Name { get; set; }
        public string Alias { get; set; }
        public JsonValueKind DataType { get; set; }
        public string EditorType { get; set; }
        public object Value { get; set; }
        public List<EnterspeedPropertyType> ChildProperties { get; set; }

        public bool IsComponent()
        {
            return ChildProperties.Any(p => p.Alias == EnterspeedPropertyConstants.IsComponentAlias);
        }

        private static bool TryGetAsEditorValue(JsonProperty jsonProperty, out EditorValue editorValue)
        {
            editorValue = null;
            var potentialEditorValueObject = jsonProperty.Value;

            if (potentialEditorValueObject.ValueKind is not JsonValueKind.Object)
            {
                return false;
            }

            string editorType = null;
            if (potentialEditorValueObject.TryGetProperty("editorType", out var editorTypeElement))
            {
                editorType = editorTypeElement.GetString();
            }

            object value = null;
            if (potentialEditorValueObject.TryGetProperty("value", out var valueElement))
            {
                value = valueElement;
            }

            if (string.IsNullOrWhiteSpace(editorType))
            {
                return false;
            }

            editorValue = new EditorValue(editorType, value);
            return true;
        }
    }
}