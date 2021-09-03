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

                        //bind to data contexts
                        init.config.fileName = ofd.FileName;

                        //enable breakline
                        chkBreakline.IsEnabled = true;

                        rbGeodeticCRS.IsEnabled = true;
                        rbMathematicCRS.IsEnabled = true;

                        //json settings set the file type (via enumeration from logic)
                        init.config.fileType = BIMGISInteropLibs.IfcTerrain.IfcTerrainFileType.LandXML;
                        
                        break;

                    //jump to this case if CityGML was selected
                    case 2:

                        //enable breakline
                        chkBreakline.IsEnabled = false;

                        //enable breakline
                        chkBreakline.IsChecked = false;

                        rbGeodeticCRS.IsEnabled = true;
                        rbGeodeticCRS.IsChecked = true;
                        rbMathematicCRS.IsEnabled = false;
                        rbMathematicCRS.IsChecked = false;

                        //bind to data context
                        init.config.fileName = ofd.FileName;

                        //json settings set the file type (via enumeration from logic)
                        init.config.fileType = BIMGISInteropLibs.IfcTerrain.IfcTerrainFileType.CityGML;

                        break;
                }

                //
                btnProcessXml.IsEnabled = true;

                //set the save path of the file to be converted
                init.config.filePath = ofd.FileName;

                //set JSON settings of file name
                init.config.fileName = System.IO.Path.GetFileName(ofd.FileName);

                //TODO logging
                return;
            }
        }

        /// <summary>
        /// passes to the current GUI (the readed JSON settings)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnProcessXml_Click(object sender, RoutedEventArgs e)
        {
            //
            if(init.config.fileType.Equals(BIMGISInteropLibs.IfcTerrain.IfcTerrainFileType.LandXML)
                && chkBreakline.IsChecked.GetValueOrDefault())
            {
                init.config.breakline = true;
            }
            else { init.config.breakline = false; }

            //
            if (rbGeodeticCRS.IsChecked.GetValueOrDefault())
            {
                init.config.mathematicCRS = false;
            }
            if (rbMathematicCRS.IsChecked.GetValueOrDefault())
            {
                init.config.mathematicCRS = true;
            }


            //set task (file opening) to true
            GuiSupport.taskfileOpening = true;

            //[IfcTerrain] check if all task are allready done
            GuiSupport.readyState();

            //[DTM2BIM] check if all task are allready done
            GuiSupport.rdyDTM2BIM();

            //display short information about imported file to user
            guiLog.fileReaded();

            //send gui logging
            guiLog.setLog("XML settings applyed.");
        }
    }
}
