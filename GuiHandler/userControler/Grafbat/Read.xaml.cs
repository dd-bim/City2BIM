using System.Windows;
using System.Windows.Controls;

using Microsoft.Win32; //used for file handling

//shortcut to set json settings
using init = GuiHandler.InitClass;

//shortcut to set logging messages
using guiLog = GuiHandler.GuiSupport;

namespace GuiHandler.userControler.Grafbat
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
        
        private void btnReadGrafbat_Click(object sender, RoutedEventArgs e)
        {
            //get config
            var config = DataContext as BIMGISInteropLibs.IfcTerrain.Config;

            //add new FileDialog handler 
            OpenFileDialog ofd = new OpenFileDialog();

            //set filter to grafbat (out files)
            ofd.Filter = "Grafbat files *.out|*.out";

            //is performed when a file is selected
            if (ofd.ShowDialog() == true)
            {
                //set the save path of the file to be converted
                config.filePath = ofd.FileName;

                //set JSON settings of file name
                config.fileName = System.IO.Path.GetFileName(ofd.FileName);
            }
        }
    }
}