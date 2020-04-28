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
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var paramList = GUI.Prop_NAS_settings.ParamList;

            int ix = 0;
            foreach (string item in paramList)
            {
                checkedListBox1.Items.Add(paramList[ix]);
                ix++;
            }

            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                checkedListBox1.SetItemChecked(i, true);
            }



        }

        private void attributesCheckedListbox_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var selectedAttributes = checkedListBox1.CheckedItems;

            GUI.Prop_NAS_settings.SelectedParams = selectedAttributes;

            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
