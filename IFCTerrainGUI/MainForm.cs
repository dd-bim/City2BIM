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

using IFCTerrainGUI.fileHandling;

namespace IFCTerrainGUI
{
    /// <summary>
    /// Class to interact with the GUI (main control)
    /// </summary>
    public partial class MainForm : Form
    {
        #region gui settings        
        /// <summary>
        /// process dxf via gui
        /// </summary>

        private dxfHandling dxfHandler;

        


        #region create background worker
        //for DXF processing
        /// <summary>
        /// create Background Worker for DXF items listing
        /// </summary>
        public static BackgroundWorker backgroundWorkerDxf;
        #endregion create background worker

        /// <summary>
        /// initalize Graphical User Interface (GUI);
        /// </summary>
        public MainForm()
        {
            //
            this.dxfHandler = new dxfHandling();

            #region backgroundWorker DXF
            //create new instance of the DXF-BackgroundWorker
            backgroundWorkerDxf = new BackgroundWorker();

            //add do task of an DXF BackgroundWorker
            backgroundWorkerDxf.DoWork += new DoWorkEventHandler(backgroundWorkerDxf_DoWork);
            //add task, if do task is completed
            backgroundWorkerDxf.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorkerDxf_RunWorkerCompleted);

            InitializeComponent(); //do not remove this
            #endregion backgroundWorker DXF

            #region placeholder
            #endregion placeholder
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
        public DxfFile dxfFile = null; //is initially created empty

        /// <summary>
        /// Once the "Read DXF" button has been pressed this function will start
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void btnReadDxf_Click(object sender, EventArgs e)
        {
            //now the function openDxf is called
            dxfHandler.openDxf(sender, e);
            return;
        }

        /// <summary>
        /// The DXF file is read (in the background)
        /// </summary>
        private void backgroundWorkerDxf_DoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = BIMGISInteropLibs.DXF.ReaderTerrain.ReadFile((string)e.Argument, out this.dxfFile) ? (string)e.Argument : "";
        }

        /// <summary>
        /// After successful reading of the DXF file this function is performed
        /// Target: all items (layer) of the DXF file are listed in the ListBox 
        /// </summary>
        private void backgroundWorkerDxf_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //storage location from dxf file
            string name = (string)e.Result;

            //suspend layout from previous BackgroundWorkers
            this.lbDxfDtmLayer.SuspendLayout();         //dtm layer selection
            this.lbDxfBreaklineLayer.SuspendLayout();   //breakline layer selection

            //delete items from list box
            this.lbDxfDtmLayer.Items.Clear();       //dtm layer selection
            this.lbDxfBreaklineLayer.Items.Clear(); //breakline layer selection

            //if a DXF file is empty, an error message is generated here (TODO: check if this is required)
            if (string.IsNullOrEmpty(name))
            {
                //the dxf file is written as null --> thus previous processing is overwritten if necessary
                this.dxfFile = null;
                //TODO Logging & Error Message
            }
            //if the dxf file is not empty, continue here
            else
            {
                //loop to go through each layer of the DXF file
                foreach (var layer in this.dxfFile.Layers)
                {
                    //list the layer in the ListBox (so it can be selected later)
                    this.lbDxfDtmLayer.Items.Add(layer.Name);

                    //list the layer in ListBox (for Breaklines)
                    this.lbDxfBreaklineLayer.Items.Add(layer.Name);
                }
            }
            //layout, so the user can now make a selection
            this.lbDxfDtmLayer.ResumeLayout();          //dtm layer selection
            this.lbDxfBreaklineLayer.ResumeLayout();    //breakline layer selection
            //TODO Logging

            //release gui again (otherwise the user can't select anything)
            this.Enabled = true;            

            return; //do not add anything after this
        }
        #endregion dxf processing

    }


}
