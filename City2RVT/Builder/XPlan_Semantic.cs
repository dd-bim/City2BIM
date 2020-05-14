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

using NLog;
using NLog.Targets;
using NLog.Config;

namespace City2RVT.Builder
{
    class XPlan_Semantic
    {
        private readonly Document doc;
        private readonly Autodesk.Revit.ApplicationServices.Application app;

        public XPlan_Semantic(Document doc, Autodesk.Revit.ApplicationServices.Application app)
        {
            this.doc = doc;
            this.app = app;
        }
        public Dictionary<string, string> createParameter(string xPlanObject, DefinitionFile defFile, List<string> paramList, XmlNode nodeSurf, XmlDocument xmlDoc, CategorySet categorySet, Logger logger)
        {
            string nodeInnerText;
            Dictionary<string, string> paramDict = new Dictionary<string, string>();

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
                                }

                                Transaction tParam = new Transaction(doc, "Insert Parameter");
                                {
                                    tParam.Start();
                                    InstanceBinding newIB = app.Create.NewInstanceBinding(categorySet);
                                    if (externalDefinition != null)
                                    {
                                        doc.ParameterBindings.Insert(externalDefinition, newIB, BuiltInParameterGroup.PG_DATA);
                                    }
                                    logger.Info("Applied Parameters to '" + paramName.Substring(paramName.LastIndexOf(':') + 1) + "'. ");
                                }
                                tParam.Commit();
                            }
                        }
                    }
                }
            }
            paramList.Clear();
            return paramDict;
        }
    }
}
