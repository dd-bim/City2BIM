using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO; //requiered for file handling


namespace IFCTerrainGUI
{
    public partial class IfcTerrainGUI : Form
    {
        //initalize GUI
        //do not change
        public IfcTerrainGUI()
        {
            InitializeComponent();
        }

        #region dxf processing

        private void btnReadDxf_Click(object sender, EventArgs e)
        {

            var ofd = new OpenFileDialog
            {
                Filter = "DXF Files *.dxf, *.dxb|*.dxf;*.dxb"
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {

            }
        }


        #endregion dxf processing



    }
}
