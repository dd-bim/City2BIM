using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms; //File Dialog

using BIMGISInteropLibs.IfcTerrain; 

namespace IFCTerrainGUI.fileHandling
{
    /// <summary>
    /// Class to support processing (via the GUI)
    /// </summary>
    public class dxf
    {
        /// <summary>
        /// DXF file opening; if a file was opened successfully, Json settings are made.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void openDxf(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "DXF Files *.dxf, *.dxb|*.dxf;*.dxb"
            };
            //if a file was selected via the filter (above) 
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                IfcTerrainGUI.jSettings.fileType = IfcTerrainFileType.Dxf;
                MessageBox.Show("DEV TEST");
            }
            //[TODO]: otherwise there have to be an error massage or hint


            return;
        }
    }
}
