using System.Collections.Generic;

using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB;

namespace CommonRevit.Semantics
{
    internal class SchemaUtils
    {
        public static Schema getSchemaByName(string schemaName)
        {
            var schemaList = Schema.ListSchemas();
            foreach (var schema in schemaList)
            {
                if (schema.SchemaName == schemaName)
                {
                    return schema;
                }
            }
            return null;
        }

        public static List<Schema> getHTWSchemas()
        {
            var schemaList = Schema.ListSchemas();
            List<Schema> htwSchemas = new List<Schema>();
            foreach (var s in schemaList)
            {
                if (s.VendorId == "HTWDRESDEN")
                {
                    htwSchemas.Add(s);
                }
            }
            return htwSchemas;
        }

        public static List<Dictionary<string, Dictionary<string, string>>> getSchemaAttributesForElement(Element element)
        {
            List<Dictionary<string, Dictionary<string, string>>> schemaAndAttributeList = new List<Dictionary<string, Dictionary<string, string>>>();

            var schemaGUIDList = element.GetEntitySchemaGuids();

            foreach (var schemaGUID in schemaGUIDList)
            {
                var schema = Schema.Lookup(schemaGUID);

                Entity ent = element.GetEntity(schema);

                Dictionary<string, string> attrDict = new Dictionary<string, string>();
                foreach (var field in schema.ListFields())
                {
                    var value = ent.Get<string>(field);
                    if (value != null && value != string.Empty)
                    {
                        attrDict.Add(field.FieldName, value);
                    }
                }

                schemaAndAttributeList.Add(new Dictionary<string, Dictionary<string, string>>() { { schema.SchemaName, attrDict } });
            }

            return schemaAndAttributeList;
        }
    }
}
