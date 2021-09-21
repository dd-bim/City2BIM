using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32; //used for file handling

//shortcut to set json settings
using init = GuiHandler.InitClass;

//shortcut to set logging messages
using guiLog = GuiHandler.GuiSupport;

using System.IO; //file path

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
            ofd.Filter = "LandXML *.xml|*.xml";
            if (ofd.ShowDialog() == true)
            {
                var config = DataContext as BIMGISInteropLibs.IfcTerrain.Config;

                //set file path and get file name
                config.filePath = ofd.FileName;
                config.fileName = Path.GetFileName(ofd.FileName);
            }
        }

        private void tgbtnCRS_Checked(object sender, RoutedEventArgs e)
        {
            var config = DataContext as BIMGISInteropLibs.IfcTerrain.Config;
            config.invertedCRS = true;
        }

        private void tgbtnCRS_Unchecked(object sender, RoutedEventArgs e)
        {
            var config = DataContext as BIMGISInteropLibs.IfcTerrain.Config;
            config.invertedCRS = false;
        }
    }
}
