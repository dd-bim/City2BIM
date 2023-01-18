using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

using Newtonsoft.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB.ExtensibleStorage;
using System.Linq;

namespace CityBIM.Reader
{
    class MetaInformation
    {
        public static void createALKISSchema(Autodesk.Revit.DB.Document doc)
        {
            //check if schema exists -> dirty hack TODO: make one xplan schema and sub schemas for objects
            Schema xPlanSchema = utils.getSchemaByName("AX_Flurstueck");
            if (xPlanSchema != null)
            {
                return;
            }

            string alkisJSON = Resource_MetaJSONs.aaaNeu;

            var ALKISSchemaDict = getSchemaDataFromJSONString(alkisJSON);

            using (Transaction trans = new Transaction(doc, "ALKIS Schema Creation"))
            {
                trans.Start();

                //loop creates new schema for each ALKIS object type
                foreach (KeyValuePair<String, List<String>> entry in ALKISSchemaDict)
                {

                    if (utils.getSchemaByName(entry.Key) != null)
                    {
                        continue;
                    }

                    SchemaBuilder sb = new SchemaBuilder(Guid.NewGuid());
                    sb.SetSchemaName(entry.Key);
                    sb.SetReadAccessLevel(AccessLevel.Public);
                    sb.SetWriteAccessLevel(AccessLevel.Public);

                    //add gmlid as Field
                    sb.AddSimpleField("gmlid", typeof(string));

                    //hack to get rid of duplicates that are present due to inheritance
                    var attrList = entry.Value.Distinct().ToList();

                    //loop for adding attributes of object type into a field of the currrent schema
                    foreach(string attrName in attrList)
                    {
                        FieldBuilder fb = sb.AddSimpleField(attrName, typeof(string));
                    }

                    sb.Finish();
                }

                trans.Commit();
            }
            
        }

        public static void createXPlanungSchema(Autodesk.Revit.DB.Document doc)
        {
            //check if schema exists -> dirty hack TODO: make one xplan schema and sub schemas for objects
            Schema xPlanSchema = utils.getSchemaByName("BP_Bereich");
            if (xPlanSchema != null)
            {
                return;
            }

            string xPlanJSON = Resource_MetaJSONs.xplan;
            var xPlanSchemaDict = getSchemaDataFromJSONString(xPlanJSON);

            using (Transaction trans = new Transaction(doc, "XPlanung Schema Creation"))
            {
                trans.Start();

                //loop creates new schema for each ALKIS object type
                foreach (KeyValuePair<String, List<String>> entry in xPlanSchemaDict)
                {

                    if (utils.getSchemaByName(entry.Key) != null)
                    {
                        continue;
                    }

                    SchemaBuilder sb = new SchemaBuilder(Guid.NewGuid());
                    sb.SetSchemaName(entry.Key);
                    sb.SetReadAccessLevel(AccessLevel.Public);
                    sb.SetWriteAccessLevel(AccessLevel.Public);

                    //add gmlid as Field
                    sb.AddSimpleField("gmlid", typeof(string));

                    //hack to get rid of duplicates that are present due to inheritance
                    var attrList = entry.Value.Distinct().ToList();

                    //loop for adding attributes of object type into a field of the currrent schema
                    foreach (string attrName in attrList)
                    {
                        FieldBuilder fb = sb.AddSimpleField(attrName, typeof(string));
                    }

                    sb.Finish();
                }

                trans.Commit();
            }

        }

        private static Dictionary<String, List<String>> getSchemaDataFromJSONString(string jsonString)
        {
            //var JSONAsText = File.ReadAllText(jsonPath);
            dynamic result = JsonConvert.DeserializeObject(jsonString);

            var schemaDict = new Dictionary<String, List<String>>();

            foreach (var obj in result.meta)
            {
                var key = obj.name.Value;
                var values = new List<String>();

                foreach (var prop in obj.properties)
                {
                    values.Add(prop.name.Value);
                }

                schemaDict.Add(key, values);
            }

            return schemaDict;
        }
    }
}
