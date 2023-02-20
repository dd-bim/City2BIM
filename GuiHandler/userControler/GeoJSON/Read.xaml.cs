using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32; //File Dialog

//shortcut to set logging messages
using guiLog = GuiHandler.GuiSupport;

using BIMGISInteropLibs.IfcTerrain;
namespace GuiHandler.userControler.GeoJSON
{
    /// <summary>
    /// Interaktionslogik für Read.xaml
    /// </summary>
    public partial class Read : UserControl
    {
        public Read()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Event to handle file dialog and set file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnReadGeoJson_Click(object sender, RoutedEventArgs e)
        {
            //create new file dialog
            OpenFileDialog ofd = new OpenFileDialog();
            //set filter
            ofd.Filter = "GeoJSON files *.GeoJSON|*.geojson|Json *.json|*.json";

            //task to run when file has been selected
            if (ofd.ShowDialog().GetValueOrDefault())
            {
                //get config (casted as Config)
                var config = DataContext as Config;

                //set file path & fiel name
                config.filePath = ofd.FileName;
                config.fileName = Path.GetFileName(ofd.FileName);

                guiLog.setLog(BIMGISInteropLibs.Logging.LogType.info, "GeoJSON file read: " + config.fileName);
            }
        }

        /// <summary>
        /// TODO
        /// </summary>
        private void btnOpenBreaklineFile_Click(object sender, RoutedEventArgs e)
        {
            /*
            //create new file dialog
            OpenFileDialog ofd = new OpenFileDialog();
            //set filter
            ofd.Filter = "GeoJSON files *.GeoJSON|*.geojson|Json *.json|*.json";

            //task to run when file has been selected
            if (ofd.ShowDialog().GetValueOrDefault())
            {
                //set file path
                config.breaklineFile = ofd.FileName;

                cbGeomTypeBreakline.IsEnabled = true;
            }
            */
        }
    }
}
