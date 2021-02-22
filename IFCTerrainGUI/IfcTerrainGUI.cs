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
    /// <summary>
    /// Class to interact with the GUI (main control)
    /// </summary>
    public partial class IfcTerrainGUI : Form
    {
        #region gui settings
        /// <summary>
        /// initalize Graphical User Interface (GUI);
        /// do not change or add something in this function
        /// </summary>
        public IfcTerrainGUI()
        {
            InitializeComponent();
        }

        /// <summary>
        /// JSON (settings) - passing information (will be necessary for further processing)
        /// </summary>
        public static JsonSettings jSettings { get; set; } = new JsonSettings();
        #endregion gui settings


        #region dxf processing
        /// <summary>
        /// Once the "Read DXF" button has been pressed this function will start
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnReadDxf_Click(object sender, EventArgs e)
        {
            fileHandling.dxf.openDxf(sender, e);


            
        }


        #endregion dxf processing



    }
}
