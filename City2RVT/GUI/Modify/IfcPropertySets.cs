using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Form = System.Windows.Forms.Form;

namespace City2RVT.GUI.Modify
{
    public partial class IfcPropertySets : Form
    {
        Window someWindow;
        ExternalCommandData commandData;
        public IfcPropertySets(ExternalCommandData cData, Window parentWindow)
        {
            commandData = cData;
            InitializeComponent();
            someWindow = parentWindow;
            PropertySetsListbox.MouseDoubleClick += new MouseEventHandler(propertyListBox_DoubleClick);

        }

        private void PropertySetsListbox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void IfcPropertySets_Load(object sender, EventArgs e)
        {
            List<string> pSetList = new List<string>();
            pSetList.Add("BauantragGrundstück");
            pSetList.Add("BauantragGebäude");
            pSetList.Add("BauantragGeschoss");
            pSetList.Add("BauantragNutzungseinheiten");
            pSetList.Add("BauantragBruttoflächen");
            pSetList.Add("BauantragNettoflächen");
            pSetList.Add("BauantragGrundstücksflächen");
            pSetList.Add("BauantragStellplätze");
            pSetList.Add("BauantragZufahrten");

            pSetList.Sort();

            int ix = 0;
            foreach (var item in pSetList)
            {
                PropertySetsListbox.Items.Add(item);
                ix++;
            }
        }

        void propertyListBox_DoubleClick(object sender, MouseEventArgs e)
        {
            //this.Hide();
            if (PropertySetsListbox.SelectedItem != null)
            {                
                //MessageBox.Show(propertyListBox.SelectedItem.ToString());
                Prop_NAS_settings.SelectedPset = PropertySetsListbox.SelectedItem.ToString();

                editIfcProperties f1 = new editIfcProperties(commandData, this);
                _ = f1.ShowDialog();

                if (Prop_NAS_settings.SelectedPset == "BauantragGrundstück")
                {                    
                    //editIfcProperties f1 = new editIfcProperties(commandData, this);
                    //_ = f1.ShowDialog();
                }                
            }
        }

        private void IfcPropertySets_VisibleChanged(object sender, EventArgs e)
        {
            if (!this.Visible) { someWindow.Hide(); }
        }
    }
}
