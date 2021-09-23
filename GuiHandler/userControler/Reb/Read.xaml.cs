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

using System.ComponentModel; //used for background worker
using Microsoft.Win32; //used for file handling
using BIMGISInteropLibs.REB; //include to read reb file

//embed for file logging
using BIMGISInteropLibs.Logging;                                    //acess to logger
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain;    //to set log messages

//shortcut to set logging messages
using guiLog = GuiHandler.GuiSupport;

namespace GuiHandler.userControler.Reb
{
    /// <summary>
    /// Interaktionslogik für Read.xaml
    /// </summary>
    public partial class Read : UserControl
    {
        public Read()
        {
            InitializeComponent();

            //create "do" task and refernz to function
            backgroundWorkerReb.DoWork += BackgroundWorkerReb_DoWork; ;

            //create the task when the "do task" is completed
            backgroundWorkerReb.RunWorkerCompleted += BackgroundWorkerReb_RunWorkerCompleted;
        }

        /// <summary>
        /// opens file dialgo as soon as a file has been selected further functions are triggerd
        /// </summary>
        private void btnReadReb_Click(object sender, RoutedEventArgs e)
        {
            //add new FileDialog handler 
            OpenFileDialog ofd = new OpenFileDialog();
            //set filtering so that the following selection is possible (these also represent only the selected files)
            ofd.Filter = "REB files *.REB, *.D45, *.D49, *.D58 | *.reb; *.d45; *.d49; *.d58s";

            if (ofd.ShowDialog() == true)
            {
                #region JSON settings
                //get config
                var config = DataContext as BIMGISInteropLibs.IfcTerrain.Config;
                
                //set JSON settings of file path
                config.filePath = ofd.FileName;

                //set JSON settings of file name
                config.fileName = System.IO.Path.GetFileName(ofd.FileName);
                #endregion JSON settings

                //lock current MainWindow (because Background Worker is triggered)
                //so the user can not change any settings during the time the background worker is running
                IsEnabled = false;

                //set mouse cursor to wait
                Mouse.OverrideCursor = Cursors.Wait;

                //kick off BackgroundWorker
                backgroundWorkerReb.RunWorkerAsync(ofd.FileName);
            }
        }

        #region background worker reb
        /// <summary>
        /// BackgroundWorker (REB): used to read reb files and list up all layers
        /// </summary>
        private readonly BackgroundWorker backgroundWorkerReb = new BackgroundWorker();

        /// <summary>
        /// reading reb files
        /// </summary>
        private void BackgroundWorkerReb_DoWork(object sender, DoWorkEventArgs e)
        {
            string filePath = e.Argument.ToString();

            //background task
            e.Result = ReaderTerrain.readHorizon(filePath);
        }

        /// <summary>
        /// will be executed as soon as the (DoWorker) has read the dxf file
        /// </summary>
        private void BackgroundWorkerReb_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //if file fits to rebdata
            if(e.Result != null)
            {
                List<int> horizons = new List<int>();
                foreach(var item in e.Result as HashSet<int>)
                {
                    horizons.Add(item);
                }
                lbRebSelect.ItemsSource = horizons;
            }
            
            //apply layout - all items will be listed
            this.lbRebSelect.UpdateLayout();

            //release complete gui again
            IsEnabled = true;

            //set mouse cursor to default
            Mouse.OverrideCursor = null;
        }
        #endregion background worker reb

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var config = DataContext as BIMGISInteropLibs.IfcTerrain.Config;

            if (config.fileType.Equals(BIMGISInteropLibs.IfcTerrain.IfcTerrainFileType.REB))
            {
                config.readPoints = true;
            }
        }
    }
}