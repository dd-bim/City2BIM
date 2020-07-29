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

namespace City2RVT.GUI.Modify
{
    public partial class showProperties : Form
    {
        ExternalCommandData commandData;
        public showProperties(ExternalCommandData cData)
        {
            commandData = cData;
            InitializeComponent();
        }

        private void showProperties_Load(object sender, EventArgs e)
        {
            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;
            var selectedId = GUI.Prop_NAS_settings.SelectedId;

            //var selectedId = "d6ac4e1b-0d17-46a3-8589-d1c211b74b69-00076da5";

            var elem = doc.GetElement(selectedId);

            ParameterSet topoParams = elem.Parameters;

            showPropertiesGrid.ColumnCount = 2;
            showPropertiesGrid.Columns[0].Name = "Parameter";
            showPropertiesGrid.Columns[0].Width = 300;
            showPropertiesGrid.Columns[1].Name = "Wert";
            showPropertiesGrid.Columns[1].Width = 300;

            foreach (Parameter p in topoParams)
            {
                if  (p.IsShared)
                {
                    string key = elem.get_Parameter(new Guid(p.GUID.ToString())).Definition.Name;
                    string value = elem.get_Parameter(new Guid(p.GUID.ToString())).AsString();

                    if (value != null && value != "")
                    {
                        ArrayList row = new ArrayList();
                        row.Add(key);
                        row.Add(value);
                        showPropertiesGrid.Rows.Add(row.ToArray());
                    }

                    
                }
            }

            //var parameters = elem.GetParameters();

        }

        private void showPropertiesGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
