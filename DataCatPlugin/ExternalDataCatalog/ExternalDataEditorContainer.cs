using System.Collections.Generic;
using System.Collections.ObjectModel;

using CommonRevit.Semantics;

namespace DataCatPlugin.ExternalDataCatalog
{
    public class ExternalDataEditorContainer
    {
        public string ObjectType { get; set; }
        public ObservableCollection<AttributeContainer> Attributes { get; set; }
        public IfcClassification IfcClassification { get; set; }

        public ExternalDataEditorContainer()
        {

        }

        public ExternalDataSchemaObject toExternalDataSchemaObject()
        {
            Dictionary<string, string> attributes = new Dictionary<string, string>();
            foreach (var entry in Attributes)
            {
                attributes.Add(entry.attrName, entry.attrValue);
            }

            return new ExternalDataSchemaObject(ObjectType, attributes, IfcClassification);
        }

        public static ExternalDataEditorContainer fromExternalDataSchemaObject(ExternalDataSchemaObject edso)
        {
            ObservableCollection<AttributeContainer> attributes = new ObservableCollection<AttributeContainer>();
            foreach (KeyValuePair<string, string> entry in edso.Properties)
            {
                attributes.Add(new AttributeContainer { attrName = entry.Key, attrValue = entry.Value });
            }
            return new ExternalDataEditorContainer { Attributes = attributes, IfcClassification = edso.IfcClassification, ObjectType = edso.ObjectType };
        }
    }
}
