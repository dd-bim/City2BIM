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


using BIMGISInteropLibs.IfcTerrain; //so that functionalities like ConnectionInterface, Result, JsonSeetings are available!


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
        /// <summary>
        /// JSON (settings) - passing information (will be necessary for further processing)
        /// </summary>
        public JsonSettings jSettings { get; set; } = new JsonSettings();




        #region dxf processing

        private void btnReadDxf_Click(object sender, EventArgs e)
        {



            var ofd = new OpenFileDialog
            {
                Filter = "DXF Files *.dxf, *.dxb|*.dxf;*.dxb"
            };
            //if a file was selected via the filter (above) 
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                
            }
            //[TODO]: otherwise there have to be an error massage or hint
        }


        #endregion dxf processing



    }
}
