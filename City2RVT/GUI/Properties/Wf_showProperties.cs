using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Controls;
using System.Linq;


using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

using Xbim.Ifc;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using System.IO;
using System.Globalization;
using City2RVT.Calc;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.UtilityResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.Interfaces;
using IfcBuildingStorey = Xbim.Ifc4.ProductExtension.IfcBuildingStorey;
using Xbim.Ifc2x3.SharedBldgElements;
using Form = System.Windows.Forms.Form;
using MessageBox = System.Windows.Forms.MessageBox;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog.LayoutRenderers.Wrappers;

using System.Xml;
using Formatting = Newtonsoft.Json.Formatting;

namespace City2RVT.GUI.Properties
{
    public partial class Wf_showProperties : Form
    {
        ExternalCommandData commandData;
        public Wf_showProperties(ExternalCommandData cData)
        {
            commandData = cData;
            InitializeComponent();
        }

        public class XPlanJSON
        {
            public string name { get; set; }
            public string description { get; set; }
            public List<Properties> properties { get; set; }
            //public List<PropertySet> propertySet { get; set; }
        }

        public class IfcElement
        {
            public string Bezeichnung { get; set; }
            public string Guid { get; set; }
            public PropertySet propertySet { get; set; }
        }

        public class Properties
        {
            public string Name { get; set; }
            public string propertyGuid { get; set; }
            public string Value { get; set; }
        }
        public class PropertySet
        {
            public string Name { get; set; }
            public string guid { get; set; }
            public List<Properties> properties { get; set; }
        }

        public class RootObject
        {
            public List<IfcElement> IFCElements { get; set; }
        }

        private void Wf_showProperties_Load(object sender, EventArgs e)
        {
            update();
        }

        public void update()
        {
            applyGml(true);            
        }

        private void dgv_showProperties_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }        

        private void btn_apply_Click(object sender, EventArgs e)
        {
            string exportJsonPath;
            if (XPlan2BIM.Prop_XPLAN_settings.ExportJsonUrl != "" && XPlan2BIM.Prop_XPLAN_settings.ExportJsonUrl != null)
            {
                exportJsonPath = XPlan2BIM.Prop_XPLAN_settings.ExportJsonUrl;
            }
            else
            {
                exportJsonPath = @"D:\testjson2.json";
            }

            string layer = Prop_NAS_settings.SelectedSingleLayer;
            string gmlId = GUI.Prop_NAS_settings.SelecteGmlGuid;

            IfcElement elem = new IfcElement();
            PropertySet pSet = new PropertySet();

            List<Properties> properties = new List<Properties>();

            foreach (DataGridViewRow roow in dgv_showProperties.Rows)
            {    
                elem.Bezeichnung = layer;
                elem.Guid = gmlId;

                pSet.properties = properties;
                pSet.Name = "XPlanung PropertySet";

                properties.Add(new Properties { Name = roow.Cells[0].Value.ToString(), Value = roow.Cells[1].Value.ToString() });
            }

            string JSONresult;

            if (File.Exists(exportJsonPath))
            {
                JSONresult = File.ReadAllText(exportJsonPath);
                var rootObject = JsonConvert.DeserializeObject<List<IfcElement>>(JSONresult);

                List<string> elemGuidList = new List<string>();
                if (rootObject != null)
                {
                    foreach (var x in rootObject)
                    {
                        elemGuidList.Add(x.Guid.ToString());
                    }
                }

                if (!elemGuidList.Contains(gmlId))
                {
                    rootObject.Add(new IfcElement { Bezeichnung = layer, Guid = gmlId, propertySet = pSet });

                    string JSONresult2 = JsonConvert.SerializeObject(rootObject, Formatting.Indented);

                    using (var tw = new StreamWriter(exportJsonPath, false))
                    {
                        tw.WriteLine(JSONresult2.ToString());
                        tw.Close();
                    }
                }
                else if (elemGuidList.Contains(gmlId))
                {
                    var toChange = rootObject.FirstOrDefault(d => d.Guid == gmlId);
                    if (toChange != null)
                    {
                        foreach (DataGridViewRow roow in dgv_showProperties.Rows)
                        {
                            Properties toChangeInner = toChange.propertySet.properties.FirstOrDefault(d => d.Name == roow.Cells[0].Value.ToString());
                            if (toChangeInner != null)
                            {
                                toChangeInner.Value = roow.Cells[1].Value.ToString();
                            }
                        }
                    }

                    string JSONresult2 = JsonConvert.SerializeObject(rootObject, Formatting.Indented);

                    using (var tw = new StreamWriter(exportJsonPath, false))
                    {
                        tw.WriteLine(JSONresult2.ToString());
                        tw.Close();
                    }
                }
            }

            else if (!File.Exists(exportJsonPath))
            {
                JSONresult = JsonConvert.SerializeObject(elem);

                var objectToSerialize = new RootObject();
                objectToSerialize.IFCElements = new List<IfcElement>()
                          {
                             new IfcElement { Bezeichnung = layer, Guid = gmlId, propertySet=pSet },
                          };
                string json = JsonConvert.SerializeObject(objectToSerialize.IFCElements, Formatting.Indented);

                using (var tw = new StreamWriter(exportJsonPath, true))
                {
                    tw.WriteLine(json.ToString());
                    tw.Close();
                }
            }
            this.Close();
        }

        private void btn_reset_Click(object sender, EventArgs e)
        {
            dgv_showProperties.Rows.Clear();
            applyGml(false);            
        }

        public void applyGml(bool check)
        {
            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            dgv_showProperties.ColumnCount = 2;
            dgv_showProperties.Columns[0].Name = "Attribut";
            dgv_showProperties.Columns[0].Width = 200;
            dgv_showProperties.Columns[0].ValueType = typeof(string);

            dgv_showProperties.Columns[1].Name = "Wert";
            dgv_showProperties.Columns[1].Width = 200;
            dgv_showProperties.Columns[1].ValueType = typeof(string);

            string gmlId = GUI.Prop_NAS_settings.SelecteGmlGuid;

            // ______________________________________________________________
            string metaJsonPath;
            if (XPlan2BIM.Prop_XPLAN_settings.MetaJsonUrl != "" && XPlan2BIM.Prop_XPLAN_settings.MetaJsonUrl != null)
            {
                metaJsonPath = XPlan2BIM.Prop_XPLAN_settings.MetaJsonUrl;
            }
            else
            {
                metaJsonPath = @"D:\Daten\LandBIM\AP 2\Dokumente\Skizze JSON\xplan.json";
            }
            // ______________________________________________________________
            var JSONresult = File.ReadAllText(metaJsonPath);

            var metaJson = JObject.Parse(JSONresult).SelectToken("meta").ToString();
            var jsonObject = JsonConvert.DeserializeObject<List<XPlanJSON>>(metaJson);

            string layer = Prop_NAS_settings.SelectedSingleLayer;

            List<string> propListString = new List<string>();
            foreach (var x in jsonObject)
            {
                if (x.name == layer)
                {
                    List<Properties> propList = new List<Properties>();
                    propList = x.properties;

                    foreach (var p in propList)
                    {
                        propListString.Add(p.Name);
                    }
                }
            }

            string pathGml;
            if (XPlan2BIM.Prop_XPLAN_settings.GmlUrl != "" && XPlan2BIM.Prop_XPLAN_settings.GmlUrl != null)
            {
                pathGml = XPlan2BIM.Prop_XPLAN_settings.GmlUrl;
            }
            else
            {
                pathGml = @"D:\Daten\LandBIM\AP 2\Daten\XPlanung Import\Bergedorf\Bergedorf84.gml";
            }

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(pathGml);

            string layerMitNs = "xplan:" + Prop_NAS_settings.SelectedSingleLayer;

            List<string> elemGuidList = new List<string>();
            List<IfcElement> rootObject = default;
            if (check == true)
            {
                // ____________________Check exported json
                string exportJsonPath;
                if (XPlan2BIM.Prop_XPLAN_settings.ExportJsonUrl != "" && XPlan2BIM.Prop_XPLAN_settings.ExportJsonUrl != null)
                {
                    exportJsonPath = XPlan2BIM.Prop_XPLAN_settings.ExportJsonUrl;
                }
                else
                {
                    exportJsonPath = @"D:\testjson2.json";
                }

                JSONresult = File.ReadAllText(exportJsonPath);
                rootObject = JsonConvert.DeserializeObject<List<IfcElement>>(JSONresult);

                if (rootObject != null)
                {
                    foreach (var x in rootObject)
                    {
                        elemGuidList.Add(x.Guid.ToString());
                    }
                }
            }

            if (!elemGuidList.Contains(gmlId))
            {
                var XmlNsmgr = new Builder.Revit_Semantic(doc);
                XmlNamespaceManager nsmgr = XmlNsmgr.GetNamespaces(xmlDoc);

                XmlNodeList nodes = xmlDoc.SelectNodes("//" + layerMitNs + "[@gml:id='" + gmlId + "']", nsmgr);

                foreach (XmlNode xmlNode in nodes)
                {
                    foreach (XmlNode c in xmlNode.ChildNodes)
                    {
                        if (propListString.Contains(c.Name.Substring(c.Name.LastIndexOf(':') + 1)))
                        {
                            ArrayList row = new ArrayList();
                            row.Add(c.Name);
                            row.Add(c.InnerText);

                            dgv_showProperties.Rows.Add(row.ToArray());
                        }
                    }

                    ArrayList row2 = new ArrayList();
                    row2.Add("gml:id");
                    row2.Add(gmlId);
                    dgv_showProperties.Rows.Add(row2.ToArray());
                }
            }
            else if (elemGuidList.Contains(gmlId))
            {
                var toChange = rootObject.FirstOrDefault(d => d.Guid == gmlId);
                if (toChange != null)
                {
                    foreach (var jsonElem in toChange.propertySet.properties)
                    {
                        ArrayList row = new ArrayList();
                        row.Add(jsonElem.Name);
                        row.Add(jsonElem.Value);
                        dgv_showProperties.Rows.Add(row.ToArray());
                    }
                }
            }
        }
    }
}
