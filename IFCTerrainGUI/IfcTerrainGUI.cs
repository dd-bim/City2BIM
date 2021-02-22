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

//IxMilia for DXF processing
using IxMilia.Dxf;

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
        /// transfer object to be able to process DXF files
        /// </summary>
        private DxfFile dxfFile = null; //is initially created empty

        /// <summary>
        /// Once the "Read DXF" button has been pressed this function will start
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnReadDxf_Click(object sender, EventArgs e)
        {
            fileHandling.dxf.openDxf(sender, e);
        }
        
        /// <summary>
        /// Background Worker for Reading DXF file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void backgroundWorkerDxf_DoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = BIMGISInteropLibs.DXF.ReaderTerrain.ReadFile((string)e.Argument, out this.dxfFile) ? (string)e.Argument : "";
        }

        /// <summary>
        /// after the DXF file has been read in the background, the results are displayed here in the GUI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundWorkerDXF_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //storage location of the read DXF file
            string name = (string)e.Result;

            //Layout (suspend) so the items can be cleared
            lbDxfDtmLayer.SuspendLayout();

            //List-Items from previous processing is deleted
            lbDxfDtmLayer.Items.Clear();

            //following loop is executed when a DXF file wasn't read
            if (string.IsNullOrEmpty(name))
            {
                this.dxfFile = null; //dxf exchange object will be written as empty
                //TODO logging
            }
            //if a dxf file could be read, the following loop is executed
            else
            {
                //pass through each layer in the DXF file
                foreach (var l in dxfFile.Layers)
                {
                    //add the layer to the ListBox, this will create a listing in the GUI
                    lbDxfDtmLayer.Items.Add(l.Name);
                }
            }
            //Apply layout
            lbDxfDtmLayer.ResumeLayout();
        }
        #endregion dxf processing


    }
}
