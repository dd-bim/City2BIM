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

namespace City2RVT.GUI.Modify
{
    public partial class EditProperties : Form
    {
        ExternalCommandData commandData;
        public EditProperties(ExternalCommandData cData)
        {
            commandData = cData;
            InitializeComponent();
        }

        private void showRelatives_Load(object sender, EventArgs e)
        {            
            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;
            var selectedElement = GUI.Prop_NAS_settings.SelectedElement;

            var topoCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Topography)
                .Where(a => a.LookupParameter("Kommentare").AsString() == selectedElement);

            List<Element> chosenList = new List<Element>();

            foreach (var t in topoCollector)
            {
                if (chosenList.Contains(t) == false)
                {
                    chosenList.Add(t);
                }
            }

            update(chosenList);
            checkJson(dgv_editProperties);

        }

        public void update(List<Element> chosenList)
        {
            dgv_editProperties.ColumnCount = 2;
            dgv_editProperties.Columns[0].Name = "Grundstück";
            dgv_editProperties.Columns[0].Width = 600;
            dgv_editProperties.Columns[0].Visible = false;

            dgv_editProperties.Columns[1].Name = "Bezeichnung";
            dgv_editProperties.Columns[1].Width = 300;

            foreach (var c in chosenList)
            {
                ArrayList row = new ArrayList();
                row.Add(c.UniqueId.ToString());

                ParameterSet pl = c.Parameters;
                List<string> paraList = new List<string>();
                foreach (Parameter p in pl)
                {
                    paraList.Add(p.Definition.Name);
                }
                if (paraList.Contains("alkis: Flurstuecksnummer"))
                {
                    Parameter kommentarParam = c.LookupParameter("alkis: Flurstuecksnummer");
                    string paramValue = kommentarParam.AsString();
                    row.Add(paramValue);
                }
                else
                {
                    Parameter kommentarParam = c.LookupParameter("Kommentare");
                    string paramValue = kommentarParam.AsString();
                    row.Add(paramValue);
                }

                //Parameter kommentarParam = c.LookupParameter("Kommentare");
                //string paramValue = kommentarParam.AsString();
                //row.Add(paramValue);
                dgv_editProperties.Rows.Add(row.ToArray());
            }

            //Add Checkbox
            DataGridViewCheckBoxColumn chk = new DataGridViewCheckBoxColumn();
            chk.HeaderText = "IstGrundstücksfläche";
            chk.Name = "CheckBox";
            chk.Width = 225;
            dgv_editProperties.Columns.Add(chk);
        }

        public void checkJson(DataGridView dgv)
        {
            string fileName = @"D:\testjson2.json";

            var JSONresult = File.ReadAllText(fileName);

            var rootObject = JsonConvert.DeserializeObject<List<IfcElement>>(JSONresult);

            Dictionary<string, bool> elemGuidDict = new Dictionary<string, bool>();
            if (rootObject != null)
            {
                foreach (var x in rootObject)
                {
                    elemGuidDict.Add(x.elementGuid.ToString(), x.propertySet.properties.value);
                }
            }

            foreach (DataGridViewRow e in dgv.Rows)
            {
                if (elemGuidDict != null && elemGuidDict.ContainsKey(e.Cells[0].Value.ToString()) && elemGuidDict[e.Cells[0].Value.ToString()] == true)
                {
                    e.Cells[2].Value = true;
                }
            }
        }


        private void relativesTable_Paint(object sender, PaintEventArgs e)
        {

        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            
        }

        public class IfcElement
        {
            public string Bezeichnung { get; set; }
            public string elementGuid { get; set; }
            public PropertySet propertySet { get; set; }
        }
        public class Properties
        {
            public string Name { get; set; }
            public string propertyGuid { get; set; }
            public bool value { get; set; }
        }
        public class PropertySet
        {
            public string Name { get; set; }
            public string psetGuid { get; set; }
            public Properties properties { get; set; }
        }               

        public class RootObject
        {
            public List<IfcElement> IFCElements { get; set; }
        }

        public void createJSON(DataGridView dgv)
        {
            string path = @"D:\testjson2.json";

            IfcElement elem = new IfcElement();
            PropertySet pSet = new PropertySet();
            Properties properties = new Properties();

            foreach (DataGridViewRow roow in dgv.Rows)
            {
                elem.propertySet = pSet;
                elem.Bezeichnung = roow.Cells[1].Value.ToString();
                elem.elementGuid = roow.Cells[0].Value.ToString();

                pSet.properties = properties;
                pSet.Name = Prop_NAS_settings.SelectedPset;

                properties.Name = dgv.Columns[2].HeaderText;
                properties.value = Convert.ToBoolean(roow.Cells[2].Value);

                string JSONresult;

                if (File.Exists(path))
                {
                    JSONresult = File.ReadAllText(path);
                    var rootObject = JsonConvert.DeserializeObject<List<IfcElement>>(JSONresult);

                    List<string> elemGuidList = new List<string>();
                    if (rootObject != null)
                    {
                        foreach (var x in rootObject)
                        {
                            elemGuidList.Add(x.elementGuid.ToString());
                        }
                    }

                    if (!elemGuidList.Contains(roow.Cells[0].Value.ToString()))
                    {
                        rootObject.Add(new IfcElement { propertySet = pSet, Bezeichnung = roow.Cells[1].Value.ToString(), elementGuid = roow.Cells[0].Value.ToString() });

                        string JSONresult2 = JsonConvert.SerializeObject(rootObject, Formatting.Indented);

                        using (var tw = new StreamWriter(path, false))
                        {
                            tw.WriteLine(JSONresult2.ToString());
                            tw.Close();
                        }
                    }
                    else if (elemGuidList.Contains(roow.Cells[0].Value.ToString()))
                    {
                        var toChange = rootObject.FirstOrDefault(d => d.elementGuid == roow.Cells[0].Value.ToString());
                        if (toChange != null) { toChange.propertySet.properties.value = Convert.ToBoolean(roow.Cells[2].Value); }

                        string JSONresult2 = JsonConvert.SerializeObject(rootObject, Formatting.Indented);

                        using (var tw = new StreamWriter(path, false))
                        {
                            tw.WriteLine(JSONresult2.ToString());
                            tw.Close();
                        }
                    }
                }
                else if (!File.Exists(path))
                {
                    JSONresult = JsonConvert.SerializeObject(elem);

                    var objectToSerialize = new RootObject();
                    objectToSerialize.IFCElements = new List<IfcElement>()
                          {
                             new IfcElement { propertySet = pSet, Bezeichnung = roow.Cells[1].Value.ToString(), elementGuid = roow.Cells[0].Value.ToString() },
                          };
                    string json = JsonConvert.SerializeObject(objectToSerialize.IFCElements);

                    //File.WriteAllText(path, JSONresult);
                    using (var tw = new StreamWriter(path, true))
                    {
                        tw.WriteLine(json.ToString());
                        tw.Close();
                    }
                }
            }
        }

        private void applyButton_Click(object sender, EventArgs e)
        {
            createJSON(dgv_editProperties);
            this.Close();
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0 || e.ColumnIndex == 1)
            {
                string clickedGuid = Convert.ToString(dgv_editProperties.Rows[e.RowIndex].Cells[0].Value);
                string clickedName = Convert.ToString(dgv_editProperties.Rows[e.RowIndex].Cells[1].Value);
                GUI.Prop_NAS_settings.SelectedId = clickedGuid;

                Modify.showProperties f1 = new Modify.showProperties(commandData);
                f1.Text = clickedName;
                _ = f1.ShowDialog();
            }
        }

        private void CheckAll_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgv_editProperties.Rows)
            {
                DataGridViewCheckBoxCell chk = (DataGridViewCheckBoxCell)row.Cells[2];
                //chk.Value = !(chk.Value == null ? false : (bool)chk.Value); //because chk.Value is initialy null
                chk.Value = true;
            }
        }

        private void UncheckAll_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgv_editProperties.Rows)
            {
                DataGridViewCheckBoxCell chk = (DataGridViewCheckBoxCell)row.Cells[2];
                chk.Value = false;
            }
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void editPropertiesGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
