using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32; //used for file handling

//shortcut to set json settings
using init = GuiHandler.InitClass;

//shortcut to set logging messages
using guiLog = GuiHandler.GuiSupport;

namespace GuiHandler.userControler.XML
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
                        init.config.fileType = BIMGISInteropLibs.IfcTerrain.IfcTerrainFileType.LandXML;

                        //Stack panel visible as user can decide whether to process break edges
                        stpXmlSelectBreakline.Visibility = Visibility;
                        break;

                    //jump to this case if CityGML was selected
                    case 2:
                        //json settings set the file type (via enumeration from logic)
                        init.config.fileType = BIMGISInteropLibs.IfcTerrain.IfcTerrainFileType.CityGML;

                        //activate button process xml (otherwise the processing can't go on)
                        btnProcessXml.IsEnabled = true;
                        break;
                }

                //set the save path of the file to be converted
                init.config.filePath = ofd.FileName;

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
            init.config.breakline = true;

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
            init.config.breakline = false;

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
            //MainWindowBib.setTextBoxText(((MainWindow)Application.Current.MainWindow).tbFileName, init.config.filePath);

            //file tpye
            //MainWindowBib.setTextBoxText(((MainWindow)Application.Current.MainWindow).tbFileType, init.config.fileType.ToString());

            //set task (file opening) to true
            GuiSupport.taskfileOpening = true;

            //[IfcTerrain] check if all task are allready done
            GuiSupport.readyState();

            //[DTM2BIM] check if all task are allready done
            GuiSupport.rdyDTM2BIM();

            //set json settings isTin to true
            init.config.isTin = true;

            //send gui logging
            guiLog.setLog("XML settings applyed.");
        }
    }
}
