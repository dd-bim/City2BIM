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
using IFCTerrainGUI.GUI.MainWindowLogic; //error handling (btn start)

namespace IFCTerrainGUI.GUI.Grafbat
{
    /// <summary>
    /// Interaction logic for ucReadGraftbat.xaml
    /// </summary>
    public partial class ucReadGraftbat : UserControl
    {
        public ucReadGraftbat()
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
                MainWindow.jSettings.filePath = ofd.FileName;

                //set the save path of the file to be converted
                MainWindow.jSettings.fileType = BIMGISInteropLibs.IfcTerrain.IfcTerrainFileType.Grafbat;

                //enable process button [TODO]: check if need to be relocated 
                btnProcessGraftbat.IsEnabled = true;

                //gui logging (user information)
                MainWindowBib.setGuiLog("File selected! --> Please make settings and confirm.");

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
            MainWindowBib.setTextBoxText(((MainWindow)Application.Current.MainWindow).tbFileName, MainWindow.jSettings.filePath);

            //file tpye
            MainWindowBib.setTextBoxText(((MainWindow)Application.Current.MainWindow).tbFileType, MainWindow.jSettings.fileType.ToString());
            #endregion gui info panel

            #region selection [include TODO!]
            //check selection and set json settings
            //Selection "Faces"
            if (rbGraftbatReadFaces.IsChecked == true)
            {
                MainWindow.jSettings.isTin = true;
            }
            //Selection "Points / Lines"
            else if (rbGraftbatReadPointsLines.IsChecked == true)
            {
                MainWindow.jSettings.isTin = false;
            }
            //Selection "Points"
            //[TODO]: must be revised ... Reader + ConnectionInterface!
            #endregion selection [include TODO!]

            #region error handling
            //set task (file opening) to true
            MainWindowBib.taskfileOpening = true;

            //gui logging (user information)
            MainWindowBib.setGuiLog("Grafbat settings applyed.");

            //check if all task are allready done
            MainWindowBib.readyState();
            #endregion error handling
        }
    }
}
