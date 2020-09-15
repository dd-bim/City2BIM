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

            var selectedPset = Prop_NAS_settings.SelectedPset;
            //MessageBox.Show(selectedPset);
            

            if (selectedPset == "BauantragGrundstück")
            {
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

                updateGrundstueck(chosenList);
            }

            else if (selectedPset == "BauantragGebäude")
            {
                FilteredElementCollector roofCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Roofs);

                List<Element> chosenList = new List<Element>();

                //foreach (var r in roofCollector.FirstOrDefault().UniqueId)
                //{
                    if (!chosenList.Contains(roofCollector.LastOrDefault()))
                    {
                        chosenList.Add(roofCollector.LastOrDefault());
                    }
                //}

                updateGebaeude(chosenList);
            }

            else if (selectedPset == "BauantragGeschoss")
            {
                Builder.IfcBuilder ifcBuilder = new Builder.IfcBuilder();
                City2RVT.GUI.XPlan2BIM.Wpf_XPlan2IFC wpf_xplan2ifc = new XPlan2BIM.Wpf_XPlan2IFC(commandData);
                string original = ifcBuilder.startRevitIfcExport(wpf_xplan2ifc.ifc_Location.Text, doc, commandData);
                IfcStore model = IfcStore.Open(original);
                var buldingStory = model.Instances.OfType<IfcBuildingStorey>();

                List<IfcBuildingStorey> chosenList = new List<IfcBuildingStorey>();

                foreach (var bs in buldingStory)
                {
                    //MessageBox.Show(bs.GlobalId);
                    chosenList.Add(bs);
                }

                updateGeschoss(chosenList, model);
            }


            //checkJson(dgv_editProperties,3);
        }

        public void updateGrundstueck(List<Element> chosenList)
        {
            dgv_editProperties.ColumnCount = 3;
            dgv_editProperties.Columns[0].Name = "Grundstück";
            dgv_editProperties.Columns[0].Width = 300;
            dgv_editProperties.Columns[0].Visible = false;
            dgv_editProperties.Columns[0].ValueType = typeof(string);

            dgv_editProperties.Columns[1].Name = "Bezeichnung";
            dgv_editProperties.Columns[1].Width = 100;
            dgv_editProperties.Columns[1].ValueType = typeof(string);


            //Add Checkbox
            DataGridViewCheckBoxColumn chk = new DataGridViewCheckBoxColumn();
            chk.HeaderText = "IstGrundstücksfläche";
            chk.Name = "CheckBox";
            chk.Width = 125;
            chk.ValueType = typeof(bool);
            dgv_editProperties.Columns.Add(chk);

            dgv_editProperties.Columns[2].Name = "GrossFloorArea";
            dgv_editProperties.Columns[2].Width = 100;
            dgv_editProperties.Columns[2].ValueType = typeof(string);


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

                var area = c.get_Parameter(BuiltInParameter.PROJECTED_SURFACE_AREA).AsValueString();
                string[] areaSplit = area.Split(' ');
                string areaWithoutUnit = areaSplit[0];
                double areaWithoutUnitDouble = Convert.ToDouble(areaWithoutUnit, CultureInfo.InvariantCulture);

                row.Add(areaWithoutUnitDouble);
                dgv_editProperties.Rows.Add(row.ToArray());
            }

            GUI.Prop_NAS_settings.ChkColumn = 3;
            checkJson(dgv_editProperties, 3);
        }

        public void updateGebaeude(List<Element> chosenList)
        {
            dgv_editProperties.ColumnCount = 3;
            dgv_editProperties.Columns[0].Name = "Gebäude";
            dgv_editProperties.Columns[0].Width = 300;
            dgv_editProperties.Columns[0].Visible = false;
            dgv_editProperties.Columns[0].ValueType = typeof(string);

            dgv_editProperties.Columns[1].Name = "Bezeichnung";
            dgv_editProperties.Columns[1].Width = 100;
            dgv_editProperties.Columns[1].ValueType = typeof(string);


            //Add Checkbox
            DataGridViewCheckBoxColumn chk = new DataGridViewCheckBoxColumn();
            chk.HeaderText = "IstGebäudehülle";
            chk.Name = "CheckBox";
            chk.Width = 125;
            chk.ValueType = typeof(bool);
            dgv_editProperties.Columns.Add(chk);

            dgv_editProperties.Columns[2].Name = "Height";
            dgv_editProperties.Columns[2].Width = 100;
            dgv_editProperties.Columns[2].ValueType = typeof(string);

            foreach (var c in chosenList)
            {
                ArrayList row = new ArrayList();
                row.Add(c.UniqueId);
                row.Add("Gebäude");

                row.Add("Platzhalter");
                dgv_editProperties.Rows.Add(row.ToArray());
            }

            GUI.Prop_NAS_settings.ChkColumn = 3;
            checkJson(dgv_editProperties, 3);
        }

        public void updateGeschoss(List<IfcBuildingStorey> chosenList, IfcStore model)
        {
            dgv_editProperties.ColumnCount = 5;
            dgv_editProperties.Columns[0].Name = "Geschoss ID";
            dgv_editProperties.Columns[0].Width = 200;
            dgv_editProperties.Columns[0].Visible = false;
            dgv_editProperties.Columns[0].ValueType = typeof(string);

            dgv_editProperties.Columns[1].Name = "Bezeichnung";
            dgv_editProperties.Columns[1].Width = 100;
            dgv_editProperties.Columns[1].ValueType = typeof(string);

            //Add Checkbox
            DataGridViewCheckBoxColumn chk = new DataGridViewCheckBoxColumn();
            chk.HeaderText = "IstGF";
            chk.Name = "CheckBox";
            chk.Width = 125;
            chk.ValueType = typeof(bool);
            dgv_editProperties.Columns.Add(chk);

            //Add Checkbox
            DataGridViewCheckBoxColumn chk2 = new DataGridViewCheckBoxColumn();
            chk2.HeaderText = "IstVollgeschoss";
            chk2.Name = "CheckBox2";
            chk2.Width = 125;
            chk2.ValueType = typeof(bool);
            dgv_editProperties.Columns.Add(chk2);            

            dgv_editProperties.Columns[2].Name = "Height";
            dgv_editProperties.Columns[2].Width = 100;
            dgv_editProperties.Columns[2].ValueType = typeof(string);
            dgv_editProperties.Columns[3].Name = "GrossFloorArea";
            dgv_editProperties.Columns[3].Width = 100;
            dgv_editProperties.Columns[3].ValueType = typeof(string);
            dgv_editProperties.Columns[4].Name = "GrossVolume";
            dgv_editProperties.Columns[4].Width = 100;
            dgv_editProperties.Columns[4].ValueType = typeof(string);

            foreach (var c in chosenList)
            {
                ArrayList row = new ArrayList();
                row.Add(c.GlobalId);
                row.Add(c.Name);
                dgv_editProperties.Rows.Add(row.ToArray());

            }

            for (int i = 0; i < dgv_editProperties.Rows.Count; i++)
            {
                dgv_editProperties.Rows[i].Cells[5].Value = true;
            }


            checkJson(dgv_editProperties, 6);
            GUI.Prop_NAS_settings.ChkColumn = 6;
        }

        public void checkJson(DataGridView dgv, int i)
        {
            string fileName = @"D:\testjson2.json";

            var JSONresult = File.ReadAllText(fileName);

            var rootObject = JsonConvert.DeserializeObject<List<IfcElement>>(JSONresult);

            Dictionary<string, List<Properties>> elemGuidDict = new Dictionary<string, List<Properties>>();
            if (rootObject != null)
            {
                foreach (var x in rootObject)
                {
                    elemGuidDict.Add(x.elementGuid.ToString(), x.propertySet.properties);
                }
            }

            foreach (DataGridViewRow e in dgv.Rows)
            {
                ////var list = elemGuidDict[e.Cells[0].Value.ToString()];
                //if (elemGuidDict != null && elemGuidDict.ContainsKey(e.Cells[0].Value.ToString()) 
                //    && elemGuidDict[e.Cells[0].Value.ToString()].FirstOrDefault(d => d.Name == dgv.Columns[i].HeaderText).value == true)
                //{
                //    e.Cells[i].Value = true;
                //}
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
            public Object value { get; set; }
            //public bool boolValue { get; set; }
        }
        public class PropertySet
        {
            public string Name { get; set; }
            public string psetGuid { get; set; }
            public List<Properties> properties { get; set; }
        }               

        public class RootObject
        {
            public List<IfcElement> IFCElements { get; set; }
        }

        public void createJSON(DataGridView dgv, int i)
        {
            string path = @"D:\testjson2.json";

            IfcElement elem = new IfcElement();
            PropertySet pSet = new PropertySet();
            //List<Properties> properties = new List<Properties>();

            foreach (DataGridViewRow roow in dgv.Rows)
            {
                List<Properties> properties = new List<Properties>();

                elem.propertySet = pSet;
                elem.Bezeichnung = roow.Cells[1].Value.ToString();
                elem.elementGuid = roow.Cells[0].Value.ToString();

                pSet.properties = properties;
                pSet.Name = Prop_NAS_settings.SelectedPset;

                foreach (DataGridViewColumn column in dgv.Columns)
                {
                    if (column.Index > 1)
                    {
                        var vt = column.ValueType;
                        Object valueO = default(Object);

                        if (vt.Name == "String")
                        {
                            if ((roow.Cells[column.Index].Value != null))
                            {
                                valueO = (roow.Cells[column.Index].Value).ToString();
                            }
                            else
                            {
                                valueO = null;
                            }
                        }
                        else if (vt.Name == "Boolean")
                        {
                            valueO = Convert.ToBoolean(roow.Cells[column.Index].Value);
                        }
                        properties.Add(new Properties { Name = dgv.Columns[column.Index].HeaderText, value = valueO  });
                    }

                }


                //properties.Add(new Properties { Name = dgv.Columns[i].HeaderText, value = Convert.ToBoolean(roow.Cells[i].Value) });

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
                        if (toChange != null) 
                        {
                            var toChangeInner = toChange.propertySet.properties.FirstOrDefault(d => d.Name == dgv.Columns[i].HeaderText);
                            if (toChangeInner != null)
                            { 
                                toChangeInner.value = (roow.Cells[i].Value).ToString(); 
                            }
                        }

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
            int zahl = Prop_NAS_settings.ChkColumn;
            createJSON(dgv_editProperties, zahl);
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
                DataGridViewCheckBoxCell chk = (DataGridViewCheckBoxCell)row.Cells[3];
                //chk.Value = !(chk.Value == null ? false : (bool)chk.Value); //because chk.Value is initialy null
                chk.Value = true;
            }
        }

        private void UncheckAll_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgv_editProperties.Rows)
            {
                DataGridViewCheckBoxCell chk = (DataGridViewCheckBoxCell)row.Cells[3];
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
