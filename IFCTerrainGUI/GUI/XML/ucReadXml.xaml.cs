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
using System.Windows.Shapes;

using Microsoft.Win32; //used for file handling

namespace IFCTerrainGUI.GUI.XML
{
    /// <summary>
    /// Interaktionslogik für ucReadXml.xaml
    /// </summary>
    public partial class ucReadXml : UserControl
    {
        /// <summary>
        /// create the instance of userControl Tin
        /// </summary>
        public ucReadXml()
        {
            //create the GUI elements
            InitializeComponent();
        }

        /// <summary>
        /// read XML file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnReadXml_Click(object sender, RoutedEventArgs e)
        {
            //add new FileDialog handler 
            OpenFileDialog ofd = new OpenFileDialog();
            //set filtering so that the following selection is possible (these also represent only the selected files)
            ofd.Filter = "LandXML *.xml|*.xml|CityGML *.gml|*.gml";
            if (ofd.ShowDialog() == true)
            {
                //use only, because CityGML or LandXML can be selected here --> thus case differentiation becomes possible
                switch (ofd.FilterIndex)
                {
                    //jump to this case if LandXML was selected
                    case 1:
                        //json settings set the file type (via enumeration from logic)
                        MainWindow.jSettings.fileType = BIMGISInteropLibs.IfcTerrain.IfcTerrainFileType.LandXml;
                        break;

                    //jump to this case if CityGML was selected
                    case 2:
                        //json settings set the file type (via enumeration from logic)
                        MainWindow.jSettings.fileType = BIMGISInteropLibs.IfcTerrain.IfcTerrainFileType.CityGml;
                        break;
                }

                //set the save path of the file to be converted
                MainWindow.jSettings.filePath = ofd.FileName;
                //TODO GUI feedback
                //TODO logging
                return;
            }

        }
    }
}
