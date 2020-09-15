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
        }

        public class Properties
        {
            public string name { get; set; }
            public string dataType { get; set; }
        }

        private void Wf_showProperties_Load(object sender, EventArgs e)
        {
            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            string layer = Prop_NAS_settings.SelectedSingleLayer;
            string layerMitNs = "xplan:" + Prop_NAS_settings.SelectedSingleLayer;

            var path = @"D:\Daten\LandBIM\AP 2\Dokumente\Skizze JSON\xplan.json";
            var JSONresult = File.ReadAllText(path);

            var pathGml = @"D:\Daten\LandBIM\AP 2\Daten\XPlanung Import\Bergedorf\Bergedorf84.gml";

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(pathGml);

            var XmlNsmgr = new Builder.Revit_Semantic(doc);
            XmlNamespaceManager nsmgr = XmlNsmgr.GetNamespaces(xmlDoc);

            XmlNodeList gmlSurfaces = xmlDoc.SelectNodes("//gml:featureMember/" + layerMitNs /*+ "//gml:exterior"*/, nsmgr);

            List<string> xmlAttrList = new List<string>();
            List<List<Properties>> xPlanJsonDict = new List<List<Properties>>();


            foreach (XmlNode xmlNode in gmlSurfaces)
            {
                foreach (XmlNode childXmlNode in xmlNode.ChildNodes)
                {
                    if (!xmlAttrList.Contains(childXmlNode.Name.Substring(childXmlNode.Name.IndexOf(':') + 1)))
                    {
                        xmlAttrList.Add(childXmlNode.Name.Substring(childXmlNode.Name.IndexOf(':') + 1));
                    }                    
                }
                xmlAttrList.Add("gmld:id");

                var metaJson = JObject.Parse(JSONresult).SelectToken("meta").ToString();

                var rootObject = JsonConvert.DeserializeObject<List<XPlanJSON>>(metaJson);

                if (rootObject != null)
                {
                    foreach (var x in rootObject)
                    {  
                        if (x.name == layer)
                        {
                            List<Properties> propList = new List<Properties>();
                            propList = x.properties;

                            List<Properties> propListClean = new List<Properties>();

                            foreach (var p in propList)
                            {
                                if (xmlAttrList.Contains(p.name))
                                {
                                    propListClean.Add(p);
                                }
                            }
                            propListClean.Add(new Properties { name = "gml:id", dataType = "string" });

                            xPlanJsonDict.Add(propListClean);
                        }
                    }
                }
            }

            var topoCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Topography)
                .Where(a => a.LookupParameter("Kommentare").AsString() == layer);


            update();
        }

        public void update()
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

            var path = @"D:\Daten\LandBIM\AP 2\Dokumente\Skizze JSON\xplan.json";
            var JSONresult = File.ReadAllText(path);

            var metaJson = JObject.Parse(JSONresult).SelectToken("meta").ToString();
            var rootObject = JsonConvert.DeserializeObject<List<XPlanJSON>>(metaJson);

            string layer = Prop_NAS_settings.SelectedSingleLayer;

            List<string> propListString = new List<string>();
            foreach (var x in rootObject)
            {
                if (x.name == layer)
                {
                    List<Properties> propList = new List<Properties>();
                    propList = x.properties;

                    foreach (var p in propList)
                    {
                        propListString.Add(p.name);
                    }
                }
            }

            var pathGml = @"D:\Daten\LandBIM\AP 2\Daten\XPlanung Import\Bergedorf\Bergedorf84.gml";
            //string pathGml = GUI.Prop_NAS_settings.FileUrl;

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(pathGml);

            string layerMitNs = "xplan:" + Prop_NAS_settings.SelectedSingleLayer;

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


        private void dgv_showProperties_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void btn_apply_Click(object sender, EventArgs e)
        {

        }
    }
}
