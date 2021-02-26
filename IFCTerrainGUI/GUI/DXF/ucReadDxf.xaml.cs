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

//Include user-specific libraries from here onwards
using IFCTerrainGUI.GUI.MainWindowLogic; //included to provide filepath insert into tb

using Microsoft.Win32; //used for file handling


namespace IFCTerrainGUI.GUI.DXF
{
    /// <summary>
    /// Interaction logic for ucReadDxf.xaml
    /// </summary>
    public partial class ucReadDxf : UserControl
    {
        public ucReadDxf()
        {
            InitializeComponent();
        }

        /// <summary>
        /// a file dialog is opened, 
        /// as soon as a file (according to the corresponding filters) has been selected 
        /// further functions are triggered
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnReadDxf_Click(object sender, RoutedEventArgs e)
        {
            //add new FileDialog handler 
            OpenFileDialog ofd = new OpenFileDialog();
            //set filtering so that the following selection is possible (these also represent only the selected files)
            ofd.Filter = "DXF Files *.dxf, *.dxb|*.dxf;*.dxb";

            //opens the dialog window (if a file is selected, everything inside the loop is executed)
            if (ofd.ShowDialog() == true)
            {
                #region JSON settings
                //set JSON settings of file format 
                //(Referencing to the BIMGISInteropsLibs, for which fileTypes an enumeration is used).
                MainWindow.jSettings.fileType = BIMGISInteropLibs.IfcTerrain.IfcTerrainFileType.DXF;

                //set JSON settings of file path
                MainWindow.jSettings.filePath = ofd.FileName;
                #endregion JSON settings

                //lock current MainWindow (because Background Worker is triggered)
                //so the user can not change any settings during the time the background worker is running
                ((MainWindow)Application.Current.MainWindow).IsEnabled = false;

                //kick off the background worker
                //TODO ADD

                #region error handling
                //TODO: buttons to be released here otherwise the user can't go on
                #endregion error handling

                #region logging
                //TODO: add logging
                #endregion logging

                #region gui feedback
                //here a feedback is given to the gui for the user (info panel)
                MainWindowBib.setTextBoxText(((MainWindow)Application.Current.MainWindow).iPTBFileName, MainWindow.jSettings.filePath);

                //conversion to string, because stored as enumeration
                ((MainWindow)Application.Current.MainWindow).iPTBFileType.Text = MainWindow.jSettings.fileType.ToString();

                #endregion gui feedback
                return; //do not add anything after this
            }



        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnProcessDxf_Click(object sender, RoutedEventArgs e)
        {
            MainWindowBib.setTextBoxText(((MainWindow)Application.Current.MainWindow).iPTBFileType, "DXF");
        }
    }
}
