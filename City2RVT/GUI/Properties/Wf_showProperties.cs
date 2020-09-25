using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Xml;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Fabrication;
using Autodesk.Revit.UI;

using System.IO;
using System.Globalization;
using City2RVT.Calc;

using Form = System.Windows.Forms.Form;
using MessageBox = System.Windows.Forms.MessageBox;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Formatting = Newtonsoft.Json.Formatting;
using NLog.LayoutRenderers.Wrappers;

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

        private void Wf_showProperties_Load(object sender, EventArgs e)
        {
            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            updateGml(doc, uidoc);
            updateZukunftBau(doc, uidoc);
        }    

        private void btn_apply_Click(object sender, EventArgs e)
        {
            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
            Element pickedElement = doc.GetElement(selectedIds.FirstOrDefault());
            TopographySurface pickedSurface = pickedElement as TopographySurface;
            var schemaName = pickedElement.LookupParameter("Kommentare").AsString();

            StoreDataInSurface(pickedSurface, dgv_showProperties, schemaName);
            StoreDataInSurface(pickedSurface, dgv_zukunftBau, "ZukunftBau");

            this.Close();
        }

        public void StoreDataInSurface(TopographySurface topographySurface, DataGridView dgv, string schemaName)
        {
            using (Transaction trans = new Transaction(topographySurface.Document, "tCreateAndStore"))
            {
                trans.Start();

                IList<Schema> list = Schema.ListSchemas();
                Schema schemaExist = list.Where(i => i.SchemaName == schemaName).FirstOrDefault();

                //SchemaBuilder sb = new SchemaBuilder(schemaExist.GUID);

                SchemaBuilder schemaBuilder = new SchemaBuilder(Guid.NewGuid());
                schemaBuilder.SetSchemaName(schemaName);

                // allow anyone to read the object
                schemaBuilder.SetReadAccessLevel(AccessLevel.Public);

                foreach (DataGridViewRow roow in dgv.Rows)
                {
                    string paramName = roow.Cells[0].Value.ToString().Substring(roow.Cells[0].Value.ToString().LastIndexOf(':') + 1);
                    FieldBuilder fieldBuilder = schemaBuilder.AddSimpleField(paramName, typeof(string));
                    fieldBuilder.SetDocumentation("ein paar properties.");
                }


                // register the Schema object
                Schema schema = default;
                if (schemaExist != null)
                {
                    schema = schemaExist;
                }
                else
                {
                    schema = schemaBuilder.Finish();
                }

                var lf = schema.ListFields();


                // create an entity (object) for this schema (class)
                Entity entity = new Entity(schema);

                foreach (DataGridViewRow roow in dgv.Rows)
                {
                    // get the field from the schema
                    string fieldName = roow.Cells[0].Value.ToString().Substring(roow.Cells[0].Value.ToString().LastIndexOf(':') + 1);
                    Field fieldSpliceLocation = schema.GetField(fieldName);

                    // set the value for this entity
                    entity.Set<string>(fieldSpliceLocation, (roow.Cells[1].Value.ToString()));

                    // store the entity in the element
                    topographySurface.SetEntity(entity);
                }

                trans.Commit();
            }            
        }        

        private void btn_reset_Click(object sender, EventArgs e)
        {
            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;
            dgv_showProperties.Rows.Clear();
            resetGml(doc, uidoc);
        }

        public string retrieveFilePath(Document doc, IList<Schema> list)
        {
            //retrieve Data Storage
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            var datastorageList = collector.OfClass(typeof(DataStorage)).ToList();
            DataStorage dataStorage = datastorageList.Where(n => n.Name == "DS_XPlanung").FirstOrDefault() as DataStorage;

            Schema schema = list.Where(i => i.SchemaName == "Filepaths").FirstOrDefault();

            Entity retrievedEntity = dataStorage.GetEntity(schema);
            string retrievedData = retrievedEntity.Get<string>(schema.GetField("gmlPath"));

            string pathGml = retrievedData;
            return pathGml;
        }

        public List<string> readGmlJson(string metaJsonPath, string layer)
        {
            var JSONresult = File.ReadAllText(metaJsonPath);

            var metaJson = JObject.Parse(JSONresult).SelectToken("meta").ToString();
            var jsonObject = JsonConvert.DeserializeObject<List<XPlanJSON>>(metaJson);

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
            return propListString;
        }

        public void updateGml(Document doc, UIDocument uidoc)
        {
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
            string layer = pickedElement.LookupParameter("Kommentare").AsString();

            List<string> propListString = readGmlJson(metaJsonPath, layer);

            IList<Schema> list = Schema.ListSchemas();

            string pathGml = retrieveFilePath(doc, list);

            //retrieve Data Storage
            FilteredElementCollector topoCollector = new FilteredElementCollector(doc);
            var topoList = topoCollector.OfClass(typeof(TopographySurface)).ToList();
            TopographySurface topoSurface = topoList.Where(n => n.LookupParameter("gml:id").AsString() == gmlId).FirstOrDefault() as TopographySurface;
            var schemaName = topoSurface.LookupParameter("Kommentare").AsString();

            Schema topoSchema = list.Where(i => i.SchemaName == schemaName).FirstOrDefault();

            List<string> fieldString = new List<string>();
            string topoRetrievedData = default;
            Entity topoRetrievedEntity = new Entity();
            if (topoSchema != null)
            {
                topoRetrievedEntity = topoSurface.GetEntity(topoSchema);
                var fieldsList = topoSchema.ListFields();

                foreach (var f in fieldsList)
                {
                    fieldString.Add(f.FieldName);
                }
            }
            
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

                        // check if Data Storage already contains parameter for the surface. Otherwise parameter are retrieved from the gml file. 
                        if (fieldString.Contains(c.Name.Substring(c.Name.LastIndexOf(':') + 1)) && topoRetrievedEntity.Schema != null)
                        {
                            topoRetrievedData = topoRetrievedEntity.Get<string>(topoSchema.GetField(c.Name.Substring(c.Name.LastIndexOf(':') + 1)));
                            row.Add(topoRetrievedData);
                        }
                        else
                        {
                            row.Add(c.InnerText);
                        }

                        dgv_showProperties.Rows.Add(row.ToArray());
                    }
                }

                ArrayList row2 = new ArrayList();
                row2.Add("gml:id");
                row2.Add(gmlId);
                dgv_showProperties.Rows.Add(row2.ToArray());
            }
            fieldString.Clear();
        }

        private void dgv_zukunftBau_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        public List<string> readZukunftBauJson(string metaJsonPath)
        {
            var JSONresult = File.ReadAllText(metaJsonPath);

            var metaJson = JObject.Parse(JSONresult).SelectToken("meta").ToString();
            var jsonObject = JsonConvert.DeserializeObject<List<XPlanJSON>>(metaJson);

            string layer = "Grundstück";

            List<string> propListNames = new List<string>();
            foreach (var x in jsonObject)
            {
                if (x.name == layer)
                {
                    // get all property sets from json
                    List<PropertySet> propSetList = new List<PropertySet>();
                    foreach (var s in x.propertySet)
                    {
                        propSetList.Add(s);
                    }

                    // get properties of propertysets
                    List<List<Properties>> propList = new List<List<Properties>>();
                    foreach (var r in propSetList)
                    {
                        propList.Add(r.properties);
                    }

                    // get name of properties
                    foreach (var properties in propList)
                    {
                        foreach (var property in properties)
                        {
                            propListNames.Add(property.Name);
                        }                        
                    }
                }
            }

            return propListNames;
        }

        public void updateZukunftBau(Document doc, UIDocument uidoc)
        {
            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
            Element pickedElement = doc.GetElement(selectedIds.FirstOrDefault());

            dgv_zukunftBau.ColumnCount = 1;
            dgv_zukunftBau.Columns[0].Name = "Attribut";
            dgv_zukunftBau.Columns[0].Width = 200;
            dgv_zukunftBau.Columns[0].ValueType = typeof(string);

            //Add Checkbox
            DataGridViewCheckBoxColumn chk = new DataGridViewCheckBoxColumn();
            chk.HeaderText = "Wert";
            chk.Name = "CheckBox";
            chk.Width = 125;
            chk.ValueType = typeof(bool);
            dgv_zukunftBau.Columns.Add(chk);

            string metaJsonPath = @"D:\Daten\LandBIM\AP 2\Dokumente\Skizze JSON\ZukunftBauAsJSON.json";

            // read meta json file for ZukunftBau
            var propListNames = readZukunftBauJson(metaJsonPath);

            string gmlId = pickedElement.LookupParameter("gml:id").AsString();

            IList<Schema> list = Schema.ListSchemas();


            //retrieve Data Storage
            FilteredElementCollector topoCollector = new FilteredElementCollector(doc);
            var topoList = topoCollector.OfClass(typeof(TopographySurface)).ToList();
            TopographySurface topoSurface = topoList.Where(n => n.LookupParameter("gml:id").AsString() == gmlId).FirstOrDefault() as TopographySurface;
            var schemaName = topoSurface.LookupParameter("Kommentare").AsString();

            Schema topoSchema = list.Where(i => i.SchemaName == "ZukunftBau").FirstOrDefault();

            List<string> fieldString = new List<string>();
            string topoRetrievedData = default;
            Entity topoRetrievedEntity = new Entity();
            if (topoSchema != null)
            {
                topoRetrievedEntity = topoSurface.GetEntity(topoSchema);
                var fieldsList = topoSchema.ListFields();

                foreach (var f in fieldsList)
                {
                    fieldString.Add(f.FieldName);
                }
            }

            foreach (var p in propListNames)
            {
                bool check = false;

                foreach (DataGridViewRow roow in dgv_zukunftBau.Rows)
                {
                    if (roow.Cells[0].Value.ToString() == p)
                    {
                        check = true;
                    }
                }
                if (check == false)
                {
                    ArrayList row = new ArrayList();
                    row.Add(p);
                    dgv_zukunftBau.Rows.Add(row.ToArray());
                } 
            }

            foreach (DataGridViewRow roow in dgv_zukunftBau.Rows)
            {
                if (fieldString.Contains(roow.Cells[0].Value.ToString()) /*c.Name.Substring(c.Name.LastIndexOf(':') + 1))*/ && topoRetrievedEntity.Schema != null)
                {
                    roow.Cells[chk.Name].Value = false;
                    topoRetrievedData = topoRetrievedEntity.Get<string>(topoSchema.GetField(roow.Cells[0].Value.ToString() /*c.Name.Substring(c.Name.LastIndexOf(':') + 1))*/));
                    roow.Cells[chk.Name].Value = Convert.ToBoolean(topoRetrievedData);
                }
                else
                {
                    roow.Cells[chk.Name].Value = false;
                }
            }            
        }

        public void resetGml(Document doc, UIDocument uidoc)
        {
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

            IList<Schema> list = Schema.ListSchemas();
            string pathGml = retrieveFilePath(doc, list);

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
    }
}