using System.Collections.Generic;

using Newtonsoft.Json;

namespace DataCatPlugin.ExternalDataCatalog
{
    public class ExternalDataSchemaObject
    {
        public string ObjectType { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public IfcClassification IfcClassification { get; set; }

        public ExternalDataSchemaObject(string objectType, Dictionary<string, string> props, IfcClassification ifcClassification)
        {
            this.ObjectType = objectType;
            this.Properties = props;
            this.IfcClassification = ifcClassification;
        }

        public Dictionary<string, Dictionary<string, string>> prepareForEditorWindow()
        {
            var result = new Dictionary<string, Dictionary<string, string>>();
            result.Add(this.ObjectType, this.Properties);
            return result;
        }

        public static ExternalDataSchemaObject fromJSONString(string jsonRepresentation)
        {
            return JsonConvert.DeserializeObject<ExternalDataSchemaObject>(jsonRepresentation);
        }

        public string toJSONString()
        {
            return JsonConvert.SerializeObject(this);
        }

        
    }
}
