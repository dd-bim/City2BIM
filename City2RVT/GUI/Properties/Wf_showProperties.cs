using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Xml;
using System.Drawing;
using System.Collections;

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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Formatting = Newtonsoft.Json.Formatting;
using NLog.LayoutRenderers.Wrappers;
using System.Reflection;

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

            string layer = GetLayer(uidoc, doc);

            // retrieve data from field by name
            string source = GetSchemaFieldValue(layer, "City2BIM_Source", uidoc, doc);

            if (source == "ALKIS")
            {
                updateGml(doc, uidoc, dgv_alkis, source);
            }
            else if (source == "XPlanung")
            {
                updateGml(doc, uidoc, dgv_showProperties, source);
            }
                        
            updateZukunftBau(doc, uidoc, "ZukunftBau");
        }    

        private void btn_apply_Click(object sender, EventArgs e)
        {
            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            string layer = GetLayer(uidoc, doc);

            // retrieve data from field by name
            string schemaName = GetSchemaFieldValue(layer, "City2BIM_Type", uidoc, doc);

            StoreDataInSurface(dgv_showProperties, schemaName, uidoc, doc);
            StoreDataInSurface(dgv_alkis, schemaName, uidoc, doc);
            StoreDataInSurface(dgv_zukunftBau, "ZukunftBau", uidoc, doc);

            this.Close();
        }

        public void StoreDataInSurface(DataGridView dgv, string schemaName, UIDocument uidoc, Document doc)
        {
            // getting the Element the user selected
            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
            Element pickedElement = doc.GetElement(selectedIds.FirstOrDefault());
            TopographySurface topoSurface = pickedElement as TopographySurface;

            // starts transaction. Transactions are needed for changes at revit documents. 
            using (Transaction trans = new Transaction(topoSurface.Document, "storeData"))
            {
                trans.Start();

                // List all existing schemas
                IList<Schema> list = Schema.ListSchemas();

                // select schema from list by schema name
                Schema schemaExist = list.Where(i => i.SchemaName == schemaName).FirstOrDefault();

                // register the Schema object
                // check if schema with specific name exists. Otherwise new Schema is created. 
                Schema schema = default;
                if (schemaExist != null)
                {
                    schema = schemaExist;
                }
                else
                {
                    // new schema builder for editing schema
                    SchemaBuilder schemaBuilder = new SchemaBuilder(Guid.NewGuid());
                    schemaBuilder.SetSchemaName(schemaName);

                    // allow anyone to read the object
                    schemaBuilder.SetReadAccessLevel(AccessLevel.Public);

                    // parameter and values of datagridview are checkd and added as fields
                    foreach (DataGridViewRow roow in dgv.Rows)
                    {
                        string paramName = roow.Cells[0].Value.ToString()
                            .Substring(roow.Cells[0].Value.ToString().LastIndexOf(':') + 1);

                        // adds new field to the schema
                        FieldBuilder fieldBuilder = schemaBuilder.AddSimpleField(paramName, typeof(string));
                        fieldBuilder.SetDocumentation("Set XPlanung properties.");
                    }

                    schema = schemaBuilder.Finish();
                }

                // create an entity (object) for this schema (class)
                Entity entity = new Entity(schema);

                foreach (DataGridViewRow roow in dgv.Rows)
                {
                    // get the field from the schema
                    string fieldName = roow.Cells[0].Value.ToString()
                        .Substring(roow.Cells[0].Value.ToString().LastIndexOf(':') + 1);
                    Field fieldSpliceLocation = schema.GetField(fieldName);

                    // set the value for this entity
                    entity.Set<string>(fieldSpliceLocation, (roow.Cells[1].Value.ToString()));

                    // store the entity in the element
                    topoSurface.SetEntity(entity);
                }
                trans.Commit();
            }            
        }        

        private void btn_reset_Click(object sender, EventArgs e)
        {
            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            if (tabControl1.SelectedTab.Text == "ALKIS")
            {
                dgv_alkis.Rows.Clear();
                resetGml(doc, uidoc, dgv_alkis, "ALKIS");
            }
            else if (tabControl1.SelectedTab.Text == "XPlanung")
            {
                dgv_showProperties.Rows.Clear();
                resetGml(doc, uidoc, dgv_showProperties, "XPlanung");
            }
        }

        /// <summary>
        /// Returns path of the imported gml-file.
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public string retrieveFilePath(Document doc, IList<Schema> list)
        {
            //retrieve Data Storage
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            var datastorageList = collector.OfClass(typeof(DataStorage)).ToList();

            // get data storage by name
            DataStorage dataStorage = datastorageList.Where(n => n.Name == "DS_XPlanung").FirstOrDefault() as DataStorage;

            // get schema by name
            Schema schema = list.Where(i => i.SchemaName == "Filepaths").FirstOrDefault();

            // retrieve entity by schema
            Entity retrievedEntity = dataStorage.GetEntity(schema);

            // retrieve data from field by name
            string pathGml = retrievedEntity.Get<string>(schema.GetField("gmlPath"));

            return pathGml;
        }

        /// <summary>
        /// returns list of properties for a chosen layer from metajson file
        /// </summary>
        /// <param name="metaJsonPath"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
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
                        if (p.Name != null)
                        {
                            propListString.Add(p.Name);
                        }                       
                    }
                }
            }
            return propListString;
        }

        public void updateGml(Document doc, UIDocument uidoc, DataGridView dgv, string source)
        {
            // getting the Element the user selected
            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
            Element pickedElement = doc.GetElement(selectedIds.FirstOrDefault());
            TopographySurface topoSurface = pickedElement as TopographySurface;

            IList<Schema> list = Schema.ListSchemas();
            string layer = GetLayer(uidoc, doc);

            // retrieve data from field by name
            string gmlId = GetSchemaFieldValue(layer, "id", uidoc, doc);
            string type = GetSchemaFieldValue(layer, "City2BIM_Type", uidoc, doc);

            // title for the GUI
            this.Text = "Eigenschaften für " + gmlId;

            // define columns for datagridview
            dgv.ColumnCount = 2;
            dgv.Columns[0].Name = "Attribut";
            dgv.Columns[0].Width = 250;
            dgv.Columns[0].ValueType = typeof(string);

            dgv.Columns[1].Name = "Wert";
            dgv.Columns[1].Width = 250;
            dgv.Columns[1].ValueType = typeof(string);

            // file for metajson for xplan (later:path in plugin folder)
            string metaJsonPath = default;
            if (source == "ALKIS")
            {
                string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string twoUp = Path.GetFullPath(Path.Combine(assemblyPath, @"..\..\"));
                string subPath = "meta_json\\aaa.json";
                metaJsonPath = Path.GetFullPath(Path.Combine(twoUp, subPath));
            }
            else if ( source == "XPlanung")
            {
                string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string twoUp = Path.GetFullPath(Path.Combine(assemblyPath, @"..\..\"));
                string subPath = "meta_json\\xplan.json";
                metaJsonPath = Path.GetFullPath(Path.Combine(twoUp, subPath));
            }

            // list of properties for the layer from metajson file
            List<string> propListString = readGmlJson(metaJsonPath, layer);

            // select schema by name
            Schema topoSchema = list.Where(i => i.SchemaName == type).FirstOrDefault();

            List<string> fieldList = new List<string>();
            string topoRetrievedData = default;
            Entity topoRetrievedEntity = new Entity();
            if (topoSchema != null)
            {
                topoRetrievedEntity = topoSurface.GetEntity(topoSchema);

                // all fields from selected schema
                var listField = topoSchema.ListFields();

                // add names of fields to list
                foreach (var f in listField) 
                {
                    fieldList.Add(f.FieldName); 
                }

                ArrayList row0 = new ArrayList();
                row0.Add("City2BIM_Type");
                row0.Add(type);
                dgv.Rows.Add(row0.ToArray());

                ArrayList row1 = new ArrayList();
                row1.Add("City2BIM_Source");
                row1.Add(source);
                dgv.Rows.Add(row1.ToArray());

                foreach (var f in fieldList)
                {
                    if (propListString.Contains(f))
                    {
                        ArrayList row = new ArrayList();
                        row.Add(f);

                        topoRetrievedData = topoRetrievedEntity.Get<string>(topoSchema.GetField(f));
                        row.Add(topoRetrievedData);

                        dgv.Rows.Add(row.ToArray());
                    }
                }
                ArrayList row2 = new ArrayList();
                row2.Add("gml:id");
                row2.Add(gmlId);
                dgv.Rows.Add(row2.ToArray());
            }    
            fieldList.Clear();
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

        public void updateZukunftBau(Document doc, UIDocument uidoc, string schemaName)
        {
            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
            Element pickedElement = doc.GetElement(selectedIds.FirstOrDefault());

            dgv_zukunftBau.ColumnCount = 1;
            dgv_zukunftBau.Columns[0].Name = "Attribut";
            dgv_zukunftBau.Columns[0].Width = 250;
            dgv_zukunftBau.Columns[0].ValueType = typeof(string);

            //Add Checkbox
            DataGridViewCheckBoxColumn chk = new DataGridViewCheckBoxColumn();
            chk.HeaderText = "Wert";
            chk.Name = "CheckBox";
            chk.Width = 250;
            chk.ValueType = typeof(bool);
            dgv_zukunftBau.Columns.Add(chk);

            // file for metajson for xplan (later:path in plugin folder)
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string twoUp = Path.GetFullPath(Path.Combine(assemblyPath, @"..\..\"));
            string subPath = "meta_json\\xplan.json";
            string metaJsonPath = Path.GetFullPath(Path.Combine(twoUp, subPath));

            // read meta json file for ZukunftBau
            var propListNames = readZukunftBauJson(metaJsonPath);

            // select schema by name
            IList<Schema> list = Schema.ListSchemas();
            Schema topoSchema = list.Where(i => i.SchemaName == schemaName).FirstOrDefault();

            //retrieve picked Element as data storage
            TopographySurface topoSurface = pickedElement as TopographySurface;

            List<string> fieldString = new List<string>();
            string topoRetrievedData = default;
            Entity topoRetrievedEntity = new Entity();

            // if schema by name exists
            if (topoSchema != null)
            {
                topoRetrievedEntity = topoSurface.GetEntity(topoSchema);
                var fieldsList = topoSchema.ListFields();

                foreach (var f in fieldsList)
                {
                    fieldString.Add(f.FieldName);
                }
            }
            else
            {
                // und wenn nicht? --> das nochmal testen
            }

            // check each property name in each datagridview row. If property already exists, value is replaced. 
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
                if (fieldString.Contains(roow.Cells[0].Value.ToString()) && topoRetrievedEntity.Schema != null)
                {
                    roow.Cells[chk.Name].Value = false;
                    topoRetrievedData = topoRetrievedEntity.Get<string>(topoSchema.GetField(roow.Cells[0].Value.ToString()));
                    roow.Cells[chk.Name].Value = Convert.ToBoolean(topoRetrievedData);
                }
                else
                {
                    roow.Cells[chk.Name].Value = false;
                }
            }            
        }

        public void resetGml(Document doc, UIDocument uidoc, DataGridView dgv, string source)
        {
            dgv.ColumnCount = 2;
            dgv.Columns[0].Name = "Attribut";
            dgv.Columns[0].Width = 200;
            dgv.Columns[0].ValueType = typeof(string);
            
            dgv.Columns[1].Name = "Wert";
            dgv.Columns[1].Width = 200;
            dgv.Columns[1].ValueType = typeof(string);

            string layer = GetLayer(uidoc, doc);
            string gmlId = GetSchemaFieldValue(layer, "id", uidoc, doc);
            string type = GetSchemaFieldValue(layer, "City2BIM_Type", uidoc, doc);

            // file for metajson for xplan (later:path in plugin folder)
            string metaJsonPath = default;
            if (source == "ALKIS")
            {
                string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string twoUp = Path.GetFullPath(Path.Combine(assemblyPath, @"..\..\"));
                string subPath = "meta_json\\aaa.json";
                metaJsonPath = Path.GetFullPath(Path.Combine(twoUp, subPath));
            }
            else if (source == "XPlanung")
            {
                string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string twoUp = Path.GetFullPath(Path.Combine(assemblyPath, @"..\..\"));
                string subPath = "meta_json\\xplan.json";
                metaJsonPath = Path.GetFullPath(Path.Combine(twoUp, subPath));
            }

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

            Reader.FileDialog winexp = new Reader.FileDialog();
            string pathGml = winexp.ImportPath(Reader.FileDialog.Data.GMLXML);

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(pathGml);

            string ns = default;
            if (source == "ALKIS")
            {
                ns = "ns2:";
            }
            else if (source == "XPlanung")
            {
                ns = "xplan:";
            }

            string layerMitNs = ns + layer;

            var XmlNsmgr = new Builder.Revit_Semantic(doc);
            XmlNamespaceManager nsmgr = XmlNsmgr.GetNamespaces(xmlDoc);

            XmlNodeList nodes = xmlDoc.SelectNodes("//" + layerMitNs + "[@gml:id='" + gmlId + "']", nsmgr);

            foreach (XmlNode xmlNode in nodes)
            {
                ArrayList row0 = new ArrayList();
                row0.Add("City2BIM_Type");
                row0.Add(type);
                dgv.Rows.Add(row0.ToArray());

                ArrayList row1 = new ArrayList();
                row1.Add("City2BIM_Source");
                row1.Add(source);
                dgv.Rows.Add(row1.ToArray());

                foreach (XmlNode c in xmlNode.ChildNodes)
                {
                    if (propListString.Contains(c.Name.Substring(c.Name.LastIndexOf(':') + 1)))
                    {
                        ArrayList row = new ArrayList();
                        row.Add(c.Name);
                        row.Add(c.InnerText);

                        dgv.Rows.Add(row.ToArray());
                    }
                }

                ArrayList row2 = new ArrayList();
                row2.Add("gml:id");
                row2.Add(gmlId);
                dgv.Rows.Add(row2.ToArray());
            }
        }

        private void btn_close_Click(object sender, EventArgs e)
        {
            Close();
        }

        public string GetLayer(UIDocument uidoc, Document doc)
        {
            // getting the Element the user selected
            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
            Element pickedElement = doc.GetElement(selectedIds.FirstOrDefault());
            TopographySurface topoSurface = pickedElement as TopographySurface;

            string layer = topoSurface.LookupParameter("Kommentare").AsString();
            
            return layer;
        }

        public string GetSchemaFieldValue(string layer, string fieldName, UIDocument uidoc, Document doc)
        {
            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
            Element pickedElement = doc.GetElement(selectedIds.FirstOrDefault());
            TopographySurface topoSurface = pickedElement as TopographySurface;

            IList<Schema> list = Schema.ListSchemas();

            // get schema by name
            Schema schema = list.Where(i => i.SchemaName == layer).FirstOrDefault();

            Entity retrievedEntity = new Entity();

            if (schema != null)
            {
                // retrieve entity by schema
                retrievedEntity = topoSurface.GetEntity(schema);
            }

            // retrieve data from field by name
            string fieldValue = retrievedEntity.Get<string>(schema.GetField(fieldName));

            return fieldValue;
        }
    }
}