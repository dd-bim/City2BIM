using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Controls;
using System.Linq;


using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Fabrication;
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
            public List<PropertySet> propertySet { get; set; }
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
            updateGml();
            updateZukunftBau();
        }

        public void updateGml()
        {
            applyGml();            
        }

        private void dgv_showProperties_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }        

        private void btn_apply_Click(object sender, EventArgs e)
        {
            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            Autodesk.Revit.UI.Selection.Selection selection = uidoc.Selection;
            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
            Element pickedElement = doc.GetElement(selectedIds.FirstOrDefault());
            TopographySurface pickedSurface = pickedElement as TopographySurface;

            StoreDataInSurface(pickedSurface);

            this.Close();
        }

        public void StoreDataInSurface(TopographySurface topographySurface)
        {
            Transaction createSchemaAndStoreData= new Transaction(topographySurface.Document, "tCreateAndStore");

            createSchemaAndStoreData.Start();

            SchemaBuilder schemaBuilder = new SchemaBuilder(Guid.NewGuid());

            // allow anyone to read the object
            schemaBuilder.SetReadAccessLevel(AccessLevel.Public);

            foreach (DataGridViewRow roow in dgv_showProperties.Rows)
            {
                FieldBuilder fieldBuilder = schemaBuilder.AddSimpleField(roow.Cells[0].Value.ToString().Substring(roow.Cells[0].Value.ToString().LastIndexOf(':') + 1), typeof(string));
                fieldBuilder.SetDocumentation("ein paar properties.");
            }

            schemaBuilder.SetSchemaName("XPlanung");

            Schema schema = schemaBuilder.Finish(); // register the Schema object

            // create an entity (object) for this schema (class)
            Entity entity = new Entity(schema);

            foreach (DataGridViewRow roow in dgv_showProperties.Rows)
            {
                // get the field from the schema
                Field fieldSpliceLocation = schema.GetField(roow.Cells[0].Value.ToString().Substring(roow.Cells[0].Value.ToString().LastIndexOf(':') + 1));

                entity.Set<string>(fieldSpliceLocation, (roow.Cells[1].Value.ToString())); // set the value for this entity

                topographySurface.SetEntity(entity); // store the entity in the element
            }

            // get the data back from the wall
            Entity retrievedEntity = topographySurface.GetEntity(schema);

            string retrievedData = retrievedEntity.Get<string>(schema.GetField("text"));

            createSchemaAndStoreData.Commit();
        }        

        private void btn_reset_Click(object sender, EventArgs e)
        {
            dgv_showProperties.Rows.Clear();
            applyGml();
            applyZukunftBau();
        }

        public void applyGml()
        {
            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            Autodesk.Revit.UI.Selection.Selection selection = uidoc.Selection;
            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
            Element pickedElement = doc.GetElement(selectedIds.FirstOrDefault());

            dgv_showProperties.ColumnCount = 2;
            dgv_showProperties.Columns[0].Name = "Attribut";
            dgv_showProperties.Columns[0].Width = 200;
            dgv_showProperties.Columns[0].ValueType = typeof(string);

            dgv_showProperties.Columns[1].Name = "Wert";
            dgv_showProperties.Columns[1].Width = 200;
            dgv_showProperties.Columns[1].ValueType = typeof(string);

            string gmlId = pickedElement.LookupParameter("gml:id").AsString();

            string metaJsonPath = @"D:\Daten\LandBIM\AP 2\Dokumente\Skizze JSON\xplan.json";

            var JSONresult = File.ReadAllText(metaJsonPath);

            var metaJson = JObject.Parse(JSONresult).SelectToken("meta").ToString();
            var jsonObject = JsonConvert.DeserializeObject<List<XPlanJSON>>(metaJson);

            string layer = pickedElement.LookupParameter("Kommentare").AsString();

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

            string pathGml = @"D:\Daten\LandBIM\AP 2\Daten\XPlanung Import\Bergedorf\Bergedorf84.gml";

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(pathGml);

            string layerMitNs = "xplan:" + pickedElement.LookupParameter("Kommentare").AsString();

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

        public void applyZukunftBau()
        {

        }

        private void dgv_zukunftBau_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        public void updateZukunftBau()
        {
            dgv_zukunftBau.ColumnCount = 1;
            dgv_zukunftBau.Columns[0].Name = "Attribut";
            dgv_zukunftBau.Columns[0].Width = 200;
            dgv_zukunftBau.Columns[0].ValueType = typeof(string);

            //dgv_zukunftBau.Columns[1].Name = "Wert";
            //dgv_zukunftBau.Columns[1].Width = 200;
            //dgv_zukunftBau.Columns[1].ValueType = typeof(string);

            //Add Checkbox
            DataGridViewCheckBoxColumn chk = new DataGridViewCheckBoxColumn();
            chk.HeaderText = "Wert";
            chk.Name = "CheckBox";
            chk.Width = 125;
            chk.ValueType = typeof(bool);
            dgv_zukunftBau.Columns.Add(chk);

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

            var JSONresult = File.ReadAllText(metaJsonPath);

            var metaJson = JObject.Parse(JSONresult).SelectToken("meta").ToString();
            var jsonObject = JsonConvert.DeserializeObject<List<XPlanJSON>>(metaJson);

            string layer = "Grundstück";

            List<string> propListString = new List<string>();
            foreach (var x in jsonObject)
            {
                if (x.name == layer)
                {
                    List<PropertySet> propSetList = new List<PropertySet>();
                    foreach (var s in x.propertySet)
                    {
                        propSetList.Add(s);
                    }
                    List<Properties> propList = new List<Properties>();

                    foreach (var r in propSetList)
                    {
                        propList = r.properties;
                    }
                    propList = x.propertySet[0].properties;

                    foreach (var p in propList)
                    {
                        propListString.Add(p.Name);
                    }
                }
            }

            foreach (var p in propListString)
            {
                //ArrayList row = new ArrayList();
                //row.Add(p);

                //DataGridViewCheckBoxCell checkboxCell = new DataGridViewCheckBoxCell()
                //{
                //    TrueValue = "1",
                //    FalseValue = "0",
                //};
                //checkboxCell.Style.NullValue = false;
                //row.Add(checkboxCell);
                ////row.Add(p.InnerText);

                //dgv_zukunftBau.Rows.Add(row.ToArray());

                int rowId = dgv_zukunftBau.Rows.Add();

                DataGridViewRow dgvrow = dgv_zukunftBau.Rows[rowId];
                //var index = dgv_zukunftBau.Rows.Add(dgvrow);
                dgvrow.Cells[0].Value = p;
                //dgvrow.Cells[1].Value = checkboxCell;

                //dgv_zukunftBau.Rows.Add(dgvrow);
            }
        }
    }
}
