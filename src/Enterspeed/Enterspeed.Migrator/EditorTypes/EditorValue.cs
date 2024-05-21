namespace Enterspeed.Migrator.EditorTypes
{
    public class EditorValue
    {
        public EditorValue(string editorType, object value)
        {
            EditorType = editorType;
            Value = value;
        }

        public string EditorType { get; set; }
        public object Value { get; set; }
    }
}