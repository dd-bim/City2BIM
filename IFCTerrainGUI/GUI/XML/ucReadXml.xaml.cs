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

using IFCTerrainGUI.GUI.MainWindowLogic;

using Microsoft.Win32; //used for file handling

namespace IFCTerrainGUI.GUI.XML
{
    /// <summary>
    /// Interaction logic for ucReadXml.xaml
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
                        MainWindow.jSettings.fileType = BIMGISInteropLibs.IfcTerrain.IfcTerrainFileType.LandXML;

                        //Stack panel visible as user can decide whether to process break edges
                        stpXmlSelectBreakline.Visibility = Visibility;
                        break;

                    //jump to this case if CityGML was selected
                    case 2:
                        //json settings set the file type (via enumeration from logic)
                        MainWindow.jSettings.fileType = BIMGISInteropLibs.IfcTerrain.IfcTerrainFileType.CityGML;

                        //activate button process xml (otherwise the processing can't go on)
                        btnProcessXml.IsEnabled = true;
                        break;
                }

                //set the save path of the file to be converted
                MainWindow.jSettings.filePath = ofd.FileName;

                //TODO logging
                return;
            }
        }

        /// <summary>
        /// is activated as soon as the radio button is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rbXmlBltrue_Checked(object sender, RoutedEventArgs e)
        {
            //set settings (break edges) to true (so they should be processed by the reader)
            MainWindow.jSettings.breakline = true;

            //activate button process xml (otherwise the processing can't go on)
            btnProcessXml.IsEnabled = true;
        }

        /// <summary>
        /// is activated as soon as the radio button is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rbXmlBlfalse_Checked(object sender, RoutedEventArgs e)
        {
            //set settings (break edges) to FALSE (so they will not be processed by the reader)
            MainWindow.jSettings.breakline = false;

            //activate button process xml (otherwise the processing can't go on)
            btnProcessXml.IsEnabled = true;
        }

        /// <summary>
        /// passes to the current GUI (the readed JSON settings)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnProcessXml_Click(object sender, RoutedEventArgs e)
        {
            //storage location
            MainWindowBib.setTextBoxText(((MainWindow)Application.Current.MainWindow).iPTBFileName, MainWindow.jSettings.filePath);

            //file tpye
            MainWindowBib.setTextBoxText(((MainWindow)Application.Current.MainWindow).iPTBFileType, MainWindow.jSettings.fileType.ToString());

            //TODO error handling (enable buttons)
            ((MainWindow)Application.Current.MainWindow).btnStart.IsEnabled = true;
        }
    }
}
