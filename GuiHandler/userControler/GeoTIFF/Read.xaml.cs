using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;        //file handling
using Microsoft.Win32;  //used for file handling

namespace GuiHandler.userControler.GeoTIFF
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

        private void btnReadTiff_Click(object sender, RoutedEventArgs e)
        {
            //get config from data context
            var config = DataContext as BIMGISInteropLibs.IfcTerrain.Config;

            //add new FileDialog handler 
            OpenFileDialog ofd = new OpenFileDialog();

            //set filtering so that the following selection is possible (these also represent only the selected files)
            ofd.Filter = "TIFF files (*.tif;*.tiff)|*.tif;*.tiff|All files (*.*)|*.*";
            ofd.FilterIndex = 1;

            //opens the dialog window (if a file is selected, everything inside the loop is executed)
            if (ofd.ShowDialog() == true)
            {
                //set JSON settings of file format 
                //(Referencing to the BIMGISInteropsLibs, for which fileTypes an enumeration is used).
                config.fileType = BIMGISInteropLibs.IfcTerrain.IfcTerrainFileType.GeoTIFF;

                //set JSON settings of file path
                config.filePath = ofd.FileName;

                //set JSON settings of file name
                config.fileName = Path.GetFileName(ofd.FileName);
            }
        }

        private void ucGeoTiff_Loaded(object sender, RoutedEventArgs e)
        {
            var config = DataContext as BIMGISInteropLibs.IfcTerrain.Config;

            if (config.fileType.Equals(BIMGISInteropLibs.IfcTerrain.IfcTerrainFileType.GeoTIFF))
            {
                config.readPoints = true;
            }
        }
    }
}
