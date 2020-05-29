using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace City2RVT.GUI.Modify
{
    public partial class ModifyParameterForm : Form
    {
        public ModifyParameterForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var paramList = GUI.Prop_NAS_settings.ParamList;

            int ix = 0;
            foreach (string item in paramList)
            {
                checkedListBox_selectParams.Items.Add(paramList[ix]);
                ix++;
            }

            for (int i = 0; i < checkedListBox_selectParams.Items.Count; i++)
            {
                checkedListBox_selectParams.SetItemChecked(i, true);
            }
        }

        private void checkedListBox_selectParams_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var selectedAttributes = checkedListBox_selectParams.CheckedItems;
            List<string> selectedAttributesShort = new List<string>();
            List<string> selectedAttributesPlusLayer = new List<string>();

            foreach (var x in selectedAttributes)
            {
                //selectedAttributesShort.Add(x.ToString());
                selectedAttributesShort.Add(x.ToString().Substring(0, x.ToString().IndexOf(' ')));
                selectedAttributesPlusLayer.Add(x.ToString());
            }
            selectedAttributesShort.Sort();

            GUI.Prop_NAS_settings.SelectedParams = selectedAttributesShort;
            GUI.Prop_NAS_settings.SelectedParamsPlusLayer = selectedAttributesPlusLayer;


            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
