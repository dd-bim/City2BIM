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

namespace City2RVT.GUI.Modify
{
    public partial class editProperties : Form
    {
        ExternalCommandData commandData;
        public editProperties(ExternalCommandData cData)
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

        }

        public void update(List<Element> chosenList)
        {
            editPropertiesGrid.ColumnCount = 1;
            editPropertiesGrid.Columns[0].Name = "Grundstück";
            editPropertiesGrid.Columns[0].Width = 150;

            foreach (var c in chosenList)
            {
                ArrayList row = new ArrayList();
                row.Add(c.UniqueId.ToString());
                editPropertiesGrid.Rows.Add(row.ToArray());
            }

            //Add Checkbox
            DataGridViewCheckBoxColumn chk = new DataGridViewCheckBoxColumn();
            chk.HeaderText = "IstGrundstücksfläche";
            chk.Name = "CheckBox";
            chk.Width = 225;
            editPropertiesGrid.Columns.Add(chk);
        }


        private void relativesTable_Paint(object sender, PaintEventArgs e)
        {

        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            
        }

        private void applyButton_Click(object sender, EventArgs e)
        {
            string fileName = @"D:\testjson.json";

            var text = File.ReadAllText(fileName);
            var dict = JsonConvert.DeserializeObject<Dictionary<string, bool>>(text);

            foreach (DataGridViewRow roow in editPropertiesGrid.Rows)
            {
                DataGridViewCheckBoxCell chkchecking = roow.Cells[1] as DataGridViewCheckBoxCell;

                if (dict.ContainsKey(roow.Cells[0].Value.ToString()) == false)
                {
                    dict.Add(roow.Cells[0].Value.ToString(), Convert.ToBoolean(chkchecking.Value));
                }
                else if (dict.ContainsKey(roow.Cells[0].Value.ToString()) == true)
                {
                    dict[roow.Cells[0].Value.ToString()] = Convert.ToBoolean(chkchecking.Value);
                }
            }

            var json = JsonConvert.SerializeObject(dict, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(fileName, json);
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0)
            {
                string clickedValue = Convert.ToString(editPropertiesGrid.Rows[e.RowIndex].Cells[0].Value);
                GUI.Prop_NAS_settings.SelectedId = clickedValue;

                Modify.showProperties f1 = new Modify.showProperties(commandData);
                _ = f1.ShowDialog();
            }
        }

        private void CheckAll_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in editPropertiesGrid.Rows)
            {
                DataGridViewCheckBoxCell chk = (DataGridViewCheckBoxCell)row.Cells[1];
                //chk.Value = !(chk.Value == null ? false : (bool)chk.Value); //because chk.Value is initialy null
                chk.Value = true;
            }
        }

        private void UncheckAll_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in editPropertiesGrid.Rows)
            {
                DataGridViewCheckBoxCell chk = (DataGridViewCheckBoxCell)row.Cells[1];
                chk.Value = false;
            }
        }
    }
}
