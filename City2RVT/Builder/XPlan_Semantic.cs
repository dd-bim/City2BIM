using City2BIM.Alkis;
using City2BIM.Geometry;
using City2BIM.Semantic;
using System.Collections.Generic;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Xml;
using System.IO;
using System.Reflection;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Fabrication;
using Autodesk.Revit.UI;

using NLog;
using NLog.Targets;
using NLog.Config;

namespace City2RVT.Builder
{
    class XPlan_Semantic
    {
        ExternalCommandData commandData;
        private readonly Document doc;
        private readonly Autodesk.Revit.ApplicationServices.Application app;

        public XPlan_Semantic(Document doc, Autodesk.Revit.ApplicationServices.Application app)
        {
            this.doc = doc;
            this.app = app;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="xPlanObject"></param>
        /// <param name="defFile"></param>
        /// <param name="paramList"></param>
        /// <param name="nodeSurf"></param>
        /// <param name="xmlDoc"></param>
        /// <param name="categorySet"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public Dictionary<string, string> createParameter(string xPlanObject, List<string> paramList, XmlNode nodeSurf, XmlDocument xmlDoc, CategorySet categorySet, Logger logger,
            TopographySurface topoSurface)
        {
            // Shared Parameter Variante
            Dictionary<string, string> paramDict = new Dictionary<string, string>();

            var XmlNsmgr = new Builder.Revit_Semantic(doc);
            XmlNamespaceManager nsmgr = XmlNsmgr.GetNamespaces(xmlDoc);

            string schemaName = xPlanObject.Substring(xPlanObject.LastIndexOf(':') + 1);

            // List all existing schemas
            IList<Schema> list = Schema.ListSchemas();
            Dictionary<string,string> di = new Dictionary<string, string>();

            // select schema from list by schema name
            Schema schemaExist = list.Where(i => i.SchemaName == schemaName).FirstOrDefault();

            // get gml-id of element
            XmlElement root = nodeSurf.ParentNode.ParentNode.ParentNode as XmlElement;
            string gmlId = root.GetAttribute("gml:id");

            // register the Schema object
            // check if schema with specific name exists. Otherwise new Schema is created. 
            Schema schema = default;
            if (schemaExist != null)
            {
                schema = schemaExist;
                var lf = schema.ListFields();

                foreach (var f in lf)
                {
                    foreach (XmlNode xn in root)
                    {
                        if (xn.Name.Substring(xn.Name.LastIndexOf(':') + 1) == f.FieldName  && xn.InnerText != null)
                        {
                            var xnName = xn.Name.Substring(xn.Name.LastIndexOf(':') + 1);
                            di.Add(xnName, xn.InnerText);
                        }                        
                    }
                }
                di.Add("id", gmlId);
                di.Add("City2BIM_Type", schemaName);
                di.Add("City2BIM_Source", "XPlanung");
            }
            else
            {
                string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string twoUp = Path.GetFullPath(Path.Combine(assemblyPath, @"..\..\"));
                string subPath = "meta_json\\xplan.json";
                string metaJsonPath = Path.GetFullPath(Path.Combine(twoUp, subPath));

                GUI.Properties.Wf_showProperties wf_ShowProperties = new GUI.Properties.Wf_showProperties(commandData);
                var propListNames = wf_ShowProperties.readGmlJson(metaJsonPath, xPlanObject.Substring(xPlanObject.LastIndexOf(':') + 1));
                string pathGml = wf_ShowProperties.retrieveFilePath(doc, list);

                // get all nodes by gml-id
                XmlNodeList nodes = xmlDoc.SelectNodes("//" + xPlanObject, nsmgr);

                // new schema builder for editing schema
                SchemaBuilder schemaBuilder = new SchemaBuilder(Guid.NewGuid());
                schemaBuilder.SetSchemaName(schemaName);

                // allow anyone to read the object
                schemaBuilder.SetReadAccessLevel(AccessLevel.Public);

                //List<string> li = new List<string>();
                foreach (XmlNode xmlNode in nodes)
                {
                    foreach (XmlNode c in xmlNode.ChildNodes)
                    {
                        if (propListNames.Contains(c.Name.Substring(c.Name.LastIndexOf(':') + 1)))
                        {
                            if (!di.ContainsKey(c.Name.Substring(c.Name.LastIndexOf(':') + 1)))
                            {
                                di.Add(c.Name.Substring(c.Name.LastIndexOf(':') + 1), "-");
                            }
                            // parameter and values of datagridview are checkd and added as fields

                            XmlElement test = xmlNode as XmlElement;
                            if (test.GetAttribute("gml:id") == gmlId)
                            {
                                di[c.Name.Substring(c.Name.LastIndexOf(':') + 1)] = c.InnerText;
                            }
                        }
                    }
                }

                di.Add("id", gmlId);
                di.Add("City2BIM_Type", schemaName);
                di.Add("City2BIM_Source", "XPlanung");

                foreach (var p in di)
                {
                    string paramName = p.Key.Substring(p.Key.LastIndexOf(':') + 1);

                    // adds new field to the schema
                    FieldBuilder fieldBuilder = schemaBuilder.AddSimpleField(paramName, typeof(string));
                    fieldBuilder.SetDocumentation("Set XPlanung properties.");
                }

                schema = schemaBuilder.Finish();
            }

            // create an entity (object) for this schema (class)
            Entity entity = new Entity(schema);

            foreach (var p in di)
            {
                // get the field from the schema
                string fieldName = p.Key.Substring(p.Key.LastIndexOf(':') + 1);
                Field fieldSpliceLocation = schema.GetField(fieldName);

                // set the value for this entity
                entity.Set<string>(fieldSpliceLocation, di[p.Key]);

                // store the entity in the element
                topoSurface.SetEntity(entity);
            }

            paramList.Clear();
            return paramDict;
        }
    }
}
