using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;
using System.Globalization;
using Newtonsoft.Json;
using System.IO;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Form = System.Windows.Forms.Form;

namespace City2RVT.GUI.Modify
{
    public partial class editElement : Form
    {
        ExternalCommandData commandData;
        public editElement(ExternalCommandData cData)
        {
            commandData = cData;
            InitializeComponent();
        }

        private void editElement_Load(object sender, EventArgs e)
        {
            update();
            updateOriginal();
            var editProperties = new EditProperties(commandData);
            editProperties.checkJson(dgv_tabPsets,3);
        }

        public void update()
        {
            dgv_tabPsets.ColumnCount = 3;
            dgv_tabPsets.Columns[0].Name = "Grundstück";
            dgv_tabPsets.Columns[0].Width = 300;
            dgv_tabPsets.Columns[0].Visible = false;

            dgv_tabPsets.Columns[1].Name = "Bezeichnung";
            dgv_tabPsets.Columns[1].Width = 100;

            Element pickedElement = Prop_Revit.PickedElement;

            ArrayList row = new ArrayList();
            row.Add(pickedElement.UniqueId);

            ParameterSet pl = pickedElement.Parameters;
            List<string> paraList = new List<string>();
            foreach (Parameter p in pl)
            {
                paraList.Add(p.Definition.Name);
            }
            if (paraList.Contains("alkis: Flurstuecksnummer"))
            {
                Parameter kommentarParam = pickedElement.LookupParameter("alkis: Flurstuecksnummer");
                string paramValue = kommentarParam.AsString();
                row.Add(paramValue);
            }
            else
            {
                Parameter kommentarParam = pickedElement.LookupParameter("Kommentare");
                string paramValue = kommentarParam.AsString();
                row.Add(paramValue);
            }

            dgv_tabPsets.Rows.Add(row.ToArray());

            //Add Checkbox
            DataGridViewCheckBoxColumn chk = new DataGridViewCheckBoxColumn();
            chk.HeaderText = "IstGrundstücksfläche";
            chk.Name = "CheckBox";
            chk.Width = 125;
            dgv_tabPsets.Columns.Add(chk);

            dgv_tabPsets.Columns[2].Name = "GrossFloorArea";
            dgv_tabPsets.Columns[2].Width = 125;

            var area = pickedElement.get_Parameter(BuiltInParameter.PROJECTED_SURFACE_AREA).AsValueString();
            string[] areaSplit = area.Split(' ');
            string areaWithoutUnit = areaSplit[0];
            double areaWithoutUnitDouble = Convert.ToDouble(areaWithoutUnit, CultureInfo.InvariantCulture);

            row.Add(areaWithoutUnitDouble);
        }

        public void updateOriginal()
        {
            Element pickedElement = Prop_Revit.PickedElement;
            ParameterSet topoParams = pickedElement.Parameters;

            dgv_original.ColumnCount = 2;
            dgv_original.Columns[0].Name = "Parameter";
            dgv_original.Columns[0].Width = 300;
            dgv_original.Columns[1].Name = "Wert";
            dgv_original.Columns[1].Width = 300;

            ArrayList row = new ArrayList();
            row.Add("Kommentare");
            row.Add(pickedElement.LookupParameter("Kommentare").AsString());
            dgv_original.Rows.Add(row.ToArray());

            foreach (Parameter p in topoParams)
            {
                if (p.IsShared)
                {
                    string key = pickedElement.get_Parameter(new Guid(p.GUID.ToString())).Definition.Name;
                    string value = pickedElement.get_Parameter(new Guid(p.GUID.ToString())).AsString();

                    if (value != null && value != "")
                    {
                        row = new ArrayList();
                        row.Add(key);
                        row.Add(value);
                        dgv_original.Rows.Add(row.ToArray());
                    }
                }
            }
        }

        private void tabPsets_Click(object sender, EventArgs e)
        {

        }

        private void dgv_tabPsets_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void dgv_original_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void btn_applyPsets_Click(object sender, EventArgs e)
        {
            EditProperties editProperties = new EditProperties(commandData);
            int zahl = Prop_NAS_settings.ChkColumn;
            editProperties.createJSON(dgv_tabPsets, zahl);
            this.Close();
        }
    }
}
