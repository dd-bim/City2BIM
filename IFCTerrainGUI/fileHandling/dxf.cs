using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms; //File Dialog

using BIMGISInteropLibs.IfcTerrain;

using System.ComponentModel; //include to be able to address background workers

using IxMilia.Dxf;  //include to be able to process DXF files

using IFCTerrainGUI;


namespace IFCTerrainGUI.fileHandling
{
    /// <summary>
    /// Class to support processing (via the GUI)
    /// </summary>
    public class dxfHandling
    {
        /// <summary>
        /// DXF file opening; if a file was opened successfully, Json settings are made.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns>true if a file could be opened</returns>
        public void openDxf(object sender, EventArgs e)
        {
            //Dialog for opening a file
            var ofd = new OpenFileDialog
            {
                //preseting a filter to open dxf files
                Filter = "DXF Files *.dxf, *.dxb|*.dxf;*.dxb"
            };

            //if a file was selected via the filter (above) 
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                #region json settings
                //set file type to dxf
                MainForm.jSettings.fileType = IfcTerrainFileType.Dxf;

                //stoarge locaction setzen
                MainForm.jSettings.filePath = ofd.FileName;
                #endregion json settings

                #region gui text messages
                //placeholder
                #endregion gui text messages

                #region start backgroundWorker 
                
                //start Background Worker otherwise it wouldn't read dxf file
                MainForm.backgroundWorkerDxf.RunWorkerAsync(ofd.FileName);

                //deactivate gui, so the user can no longer make any entries while the DXF file is being read
                
                

                #endregion start backgroundWorker 

              
              

                #region error handling



                //placeholder for error handling
                #endregion error handling

                #region logging
                //placeholder
                #endregion logging
            }
            //[TODO]: otherwise there have to be an error massage or hint
            return; //do not add any program code after that, otherwise it will not be executed
        }
    }
}