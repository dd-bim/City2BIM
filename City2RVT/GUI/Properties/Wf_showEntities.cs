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

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Form = System.Windows.Forms.Form;

namespace City2RVT.GUI.Properties
{
    public partial class Wf_showEntities : Form
    {
        ExternalCommandData commandData;
        public Wf_showEntities(ExternalCommandData cData)
        {
            commandData = cData;
            InitializeComponent();
        }

        private void Wf_showEntities_Load(object sender, EventArgs e)
        {
            //MessageBox.Show("test");
            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            string layer = Prop_NAS_settings.SelectedSingleLayer;

            var topoCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Topography)
                .Where(a => a.LookupParameter("Kommentare").AsString() == layer);

            //MessageBox.Show(topoCollector.Count().ToString());

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

        private void dgv_showEntites_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            
        }

        public void update(List<Element> chosenList)
        {
            dgv_showEntites.ColumnCount = 1;
            dgv_showEntites.Columns[0].Name = "Entity";
            dgv_showEntites.Columns[0].Width = 300;
            //dgv_showEntites.Columns[0].Visible = false;
            //dgv_showEntites.Columns[0].ValueType = typeof(string);

            foreach (Element c in chosenList)
            {
                ArrayList row = new ArrayList();
                row.Add(c.LookupParameter("gml:id").AsString());
                dgv_showEntites.Rows.Add(row.ToArray());
            }
        }

        private void dgv_showEntites_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            string clickedGuid = Convert.ToString(dgv_showEntites.Rows[e.RowIndex].Cells[0].Value);
            GUI.Prop_NAS_settings.SelecteGmlGuid = clickedGuid;

            Wf_showProperties f1 = new Wf_showProperties(commandData);
            _ = f1.ShowDialog();
        }
    }
}
