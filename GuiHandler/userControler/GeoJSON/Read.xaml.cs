using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32; //File Dialog

//shortcut to set json settings
using init = GuiHandler.InitClass;

//shortcut to set logging messages
using guiLog = GuiHandler.GuiSupport;

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
            ofd.Filter = "GeoJSON files *.GeoJSON|*.geojson";

            //task to run when file has been selected
            if (ofd.ShowDialog().GetValueOrDefault())
            {
                //set file path
                init.config.filePath = ofd.FileName;

                //set file name
                init.config.fileName = Path.GetFileName(ofd.FileName);

                //set file type
                init.config.fileType = BIMGISInteropLibs.IfcTerrain.IfcTerrainFileType.GeoJSON;

                btnProcessGeoJson.IsEnabled = true;
            }
        }

        private void btnProcessGeoJson_Click(object sender, RoutedEventArgs e)
        {
            //
            guiLog.taskfileOpening = true;
            
            //
            guiLog.rdyDTM2BIM();
            
            //
            guiLog.setLog("GeoJSON settings applyed.");

            //
            guiLog.fileReaded();
            
            //
            guiLog.readyState();
            
        }
    }
}
