using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms; //File Dialog

using BIMGISInteropLibs.IfcTerrain;

using System.ComponentModel; //include to be able to address background workers

using IxMilia.Dxf;  //include to be able to process DXF files

namespace IFCTerrainGUI.fileHandling
{
    /// <summary>
    /// Class to support processing (via the GUI)
    /// </summary>
    public static class dxf
    {
        /// <summary>
        /// DXF file opening; if a file was opened successfully, Json settings are made.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void openDxf(object sender, EventArgs e)
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
                IfcTerrainGUI.jSettings.fileType = IfcTerrainFileType.Dxf;

                //stoarge locaction setzen
                IfcTerrainGUI.jSettings.filePath = ofd.FileName;
                #endregion json settings

                #region gui text messages
                //placeholder
                #endregion gui text messages

                #region background worker async
                //background worker start: this lists all DXF entries (from the DXF file) in the ListBox
                IfcTerrainGUI.backgroundWorkerDxf.RunWorkerAsync(ofd.FileName);
                #endregion background worker async

                #region error handling
                //placeholder for error handling
                #endregion error handling

                #region logging
                //placeholder
                #endregion logging
            }
            //[TODO]: otherwise there have to be an error massage or hint


            return;
        }
    }
}
