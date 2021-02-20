using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms; //File Dialog

namespace IFCTerrainGUI.GUI
{
    public class dxfHandling
    {
        public static string openDxf(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "DXF Files *.dxf, *.dxb|*.dxf;*.dxb"
            };

            if(ofd.ShowDialog() == DialogResult.OK)
            {

            }
            

            return null;
        }
    }
}
