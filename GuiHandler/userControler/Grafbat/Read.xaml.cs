using System.Windows;
using System.Windows.Controls;

using Microsoft.Win32; //used for file handling

//shortcut to set json settings
using init = GuiHandler.InitClass;

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
            //add new FileDialog handler 
            OpenFileDialog ofd = new OpenFileDialog();

            //set filter to grafbat (out files)
            ofd.Filter = "Grafbat files *.out|*.out";

            //is performed when a file is selected
            if (ofd.ShowDialog() == true)
            {
                //set the save path of the file to be converted
                init.config.filePath = ofd.FileName;

                //set the save path of the file to be converted
                init.config.fileType = BIMGISInteropLibs.IfcTerrain.IfcTerrainFileType.Grafbat;

                //enable process button [TODO]: check if need to be relocated 
                btnProcessGraftbat.IsEnabled = true;

                //gui logging (user information)
                //MainWindowBib.setGuiLog("File selected! --> Please make settings and confirm.");

                //TODO logging

                return;
            }
        }

        /// <summary>
        /// will be executed as soon as the process grafbat button is clicked
        /// mainly used to apply gui info and json setting
        /// </summary>
        private void btnProcessGraftbat_Click(object sender, RoutedEventArgs e)
        {
            #region gui info panel
            // storage location
            //MainWindowBib.setTextBoxText(((MainWindow)Application.Current.MainWindow).tbFileName, init.config.filePath);

            //file tpye
            //MainWindowBib.setTextBoxText(((MainWindow)Application.Current.MainWindow).tbFileType, init.config.fileType.ToString());
            #endregion gui info panel

            #region selection [include TODO!]
            //check selection and set json settings
            //Selection "Faces"
            if (rbGraftbatReadFaces.IsChecked == true)
            {
                init.config.isTin = true;
            }
            //Selection "Points / Lines"
            else if (rbGraftbatReadPointsLines.IsChecked == true)
            {
                init.config.isTin = false;
            }
            //Selection "Points"
            //[TODO]: must be revised ... Reader + ConnectionInterface!
            #endregion selection [include TODO!]

            #region error handling
            //set task (file opening) to true
            GuiSupport.taskfileOpening = true;

            //[IfcTerrain] check if all task are allready done
            GuiSupport.readyState();

            //[DTM2BIM] check if all task are allready done
            GuiSupport.rdyDTM2BIM();

            //gui logging (user information)
            //MainWindowBib.setGuiLog("Grafbat settings applyed.");

            //check if all task are allready done
            GuiSupport.readyState();
            #endregion error handling
        }
    }
}