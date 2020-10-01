using Autodesk.Revit.DB;
using City2BIM.Alkis;
using City2BIM.Geometry;
using City2BIM.Semantic;
using System.Collections.Generic;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Xml;

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
        public Dictionary<string, string> createParameter(string xPlanObject, DefinitionFile defFile, List<string> paramList, XmlNode nodeSurf, XmlDocument xmlDoc, CategorySet categorySet, Logger logger,
            TopographySurface topoSurface)
        {
            string nodeInnerText;
            Dictionary<string, string> paramDict = new Dictionary<string, string>();
            Dictionary<string, string> paramDictOhne = new Dictionary<string, string>();


            System.Collections.IList selectedParams = GUI.Prop_NAS_settings.SelectedParams;

            var XmlNsmgr = new Builder.Revit_Semantic(doc);
            XmlNamespaceManager nsmgr = XmlNsmgr.GetNamespaces(xmlDoc);

            foreach (DefinitionGroup dg in defFile.Groups)
            {
                foreach (var paramName in paramList)
                {
                    if (dg.Name == "XPlanDaten")
                    {
                        XmlNode objektBezeichnung = nodeSurf.ParentNode.ParentNode.ParentNode;
                        var parameterBezeichnung = objektBezeichnung.SelectNodes(paramName, nsmgr);

                        if (selectedParams == null || selectedParams.Contains(paramName))
                        {
                            if (parameterBezeichnung != null)
                            {
                                ExternalDefinition externalDefinition = dg.Definitions.get_Item(paramName) as ExternalDefinition;

                                var getNodeContent = new GUI.XPlan2BIM.XPlan_Parameter();
                                nodeInnerText = getNodeContent.getNodeText(nodeSurf, nsmgr, xPlanObject, paramName);

                                if (paramDict.ContainsKey(paramName) == false)
                                {
                                    paramDict.Add(paramName, nodeInnerText);
                                    paramDictOhne.Add(paramName.Substring(paramName.LastIndexOf(':') + 1), nodeInnerText);

                                }

                                //Transaction tParam = new Transaction(doc, "Insert Parameter");
                                //{
                                //    tParam.Start();
                                InstanceBinding newIB = app.Create.NewInstanceBinding(categorySet);
                                    if (externalDefinition != null)
                                    {
                                        doc.ParameterBindings.Insert(externalDefinition, newIB, BuiltInParameterGroup.PG_DATA);
                                    }
                                    logger.Info("Applied Parameters to '" + paramName.Substring(paramName.LastIndexOf(':') + 1) + "'. ");
                                //}
                                //tParam.Commit();
                            }
                        }
                    }
                }
            }


            


            //// starts transaction. Transactions are needed for changes at revit documents. 
            //using (Transaction trans = new Transaction(doc, "storeData"))
            //{
            //    trans.Start();

            



            string schemaName = xPlanObject.Substring(xPlanObject.LastIndexOf(':') + 1);

            // List all existing schemas
            IList<Schema> list = Schema.ListSchemas();
            Dictionary<string,string> li = new Dictionary<string, string>();

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
                        if (xn.Name.EndsWith(f.FieldName) && xn.InnerText != null)
                        {
                            li.Add(f.FieldName, xn.InnerText);
                        }
                        
                    }
                    //li.Add(f.FieldName, paramDictOhne[f.FieldName]);
                }
                li.Add("id", gmlId);
            }
            else
            {
                string metaJsonPath = @"D:\Daten\LandBIM\AP 2\Dokumente\Skizze JSON\xplan.json";

                GUI.Properties.Wf_showProperties wf_ShowProperties = new GUI.Properties.Wf_showProperties(commandData);
                var propListNames = wf_ShowProperties.readGmlJson(metaJsonPath, xPlanObject.Substring(xPlanObject.LastIndexOf(':') + 1));
                string pathGml = wf_ShowProperties.retrieveFilePath(doc, list);

                //// get gml-id of element
                //XmlElement root = nodeSurf.ParentNode.ParentNode.ParentNode as XmlElement;
                //string gmlId = root.GetAttribute("gml:id");

                // get all nodes by gml-id
                //XmlNodeList nodes = xmlDoc.SelectNodes("//" + xPlanObject + "[@gml:id='" + gmlId + "']", nsmgr);
                XmlNodeList nodes = xmlDoc.SelectNodes("//" + xPlanObject, nsmgr);


                // new schema builder for editing schema
                SchemaBuilder schemaBuilder = new SchemaBuilder(Guid.NewGuid());
                schemaBuilder.SetSchemaName(schemaName);

                // allow anyone to read the object
                schemaBuilder.SetReadAccessLevel(AccessLevel.Public);

                //// parameter and values of datagridview are checkd and added as fields
                //foreach (var p in paramList)
                //{
                //    string paramName = p.Substring(p.LastIndexOf(':') + 1 );

                //    // adds new field to the schema
                //    FieldBuilder fieldBuilder = schemaBuilder.AddSimpleField(paramName, typeof(string));
                //    fieldBuilder.SetDocumentation("Set XPlanung properties.");
                //}

                //List<string> li = new List<string>();
                foreach (XmlNode xmlNode in nodes)
                {
                    foreach (XmlNode c in xmlNode.ChildNodes)
                    {
                        if (propListNames.Contains(c.Name.Substring(c.Name.LastIndexOf(':') + 1)))
                        {
                            if (!li.ContainsKey(c.Name.Substring(c.Name.LastIndexOf(':') + 1)))
                            {
                                li.Add(c.Name.Substring(c.Name.LastIndexOf(':') + 1), "-");
                                //li.Add(c.Name.Substring(c.Name.LastIndexOf(':') + 1), c.InnerText);
                                //FieldBuilder fieldBuilder = schemaBuilder.AddSimpleField(c.Name.Substring(c.Name.LastIndexOf(':') + 1), typeof(string));
                                //fieldBuilder.SetDocumentation("Set XPlanung properties.");
                            }
                            // parameter and values of datagridview are checkd and added as fields

                            XmlElement test = xmlNode as XmlElement;
                            if (test.GetAttribute("gml:id") == gmlId)
                            {
                                li[c.Name.Substring(c.Name.LastIndexOf(':') + 1)] = c.InnerText;
                            }


                            //foreach (var p in li)
                            //{
                            //    //string paramName = p.Substring(p.LastIndexOf(':') + 1);

                            //    // adds new field to the schema
                            //    FieldBuilder fieldBuilder = schemaBuilder.AddSimpleField(p.Key, typeof(string));
                            //    fieldBuilder.SetDocumentation("Set XPlanung properties.");
                            //}
                        }
                    }
                }

                li.Add("id", gmlId);

                foreach (var p in li)
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


            //XmlDocument xmlDoc = new XmlDocument();
            //xmlDoc.Load(pathGml);

            //string layerMitNs = "xplan:" + pickedElement.LookupParameter("Kommentare").AsString();

            //var XmlNsmgr = new Builder.Revit_Semantic(doc);
            //XmlNamespaceManager nsmgr = XmlNsmgr.GetNamespaces(xmlDoc);

            //XmlElement root = nodeSurf.ParentNode.ParentNode.ParentNode as XmlElement;
            //string gmlId = root.GetAttribute("gml:id");

            //XmlNodeList nodes = xmlDoc.SelectNodes("//" + xPlanObject + "[@gml:id='" + gmlId + "']", nsmgr);

            //foreach (XmlNode xmlNode in nodes)
            //{
            //    foreach (XmlNode c in xmlNode.ChildNodes)

            //    {
            //        if (propListNames.Contains(c.Name.Substring(c.Name.LastIndexOf(':') + 1)))
            //        {
            //            //foreach (var p in paramList)
            //            //{
            //            //    // get the field from the schema
            //            //    string fieldName = p.Substring(p.LastIndexOf(':') + 1);
            //            //    Field fieldSpliceLocation = schema.GetField(fieldName);

            //            //    // set the value for this entity
            //            //    entity.Set<string>(fieldSpliceLocation, "n/a");

            //            //    // store the entity in the element
            //            //    topoSurface.SetEntity(entity);
            //            //}
            //        }
            //    }
            //}

            //li.Add("id", gmlId);

            foreach (var p in li)
            {
                // get the field from the schema
                string fieldName = p.Key.Substring(p.Key.LastIndexOf(':') + 1);
                Field fieldSpliceLocation = schema.GetField(fieldName);

                // set the value for this entity
                entity.Set<string>(fieldSpliceLocation, li[p.Key]);

                // store the entity in the element
                topoSurface.SetEntity(entity);
            }

            //foreach (var p in paramList)
            //    {
            //        // get the field from the schema
            //        string fieldName = p.Substring(p.LastIndexOf(':') + 1);
            //    Field fieldSpliceLocation = schema.GetField(fieldName);

            //        // set the value for this entity
            //        entity.Set<string>(fieldSpliceLocation, "n/a");

            //        // store the entity in the element
            //        topoSurface.SetEntity(entity);
            //    }

            //    trans.Commit();
            //}



            paramList.Clear();
            return paramDict;
        }
    }
}
