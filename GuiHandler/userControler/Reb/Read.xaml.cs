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

//shortcut to set json settings
using init = GuiHandler.InitClass;

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
        /// container for reb Data
        /// </summary>
        private RebDaData rebData = null;

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
                //set JSON settings of file format 
                //(Referencing to the BIMGISInteropsLibs, for which fileTypes an enumeration is used).
                init.config.fileType = BIMGISInteropLibs.IfcTerrain.IfcTerrainFileType.REB;

                //set JSON settings of file path
                init.config.filePath = ofd.FileName;
                #endregion JSON settings

                //lock current MainWindow (because Background Worker is triggered)
                //so the user can not change any settings during the time the background worker is running
                IsEnabled = false;

                //set mouse cursor to wait
                Mouse.OverrideCursor = Cursors.Wait;

                #region backgroundWorker
                //kick off BackgroundWorker
                backgroundWorkerReb.RunWorkerAsync(ofd.FileName);
                #endregion backgroundWorker

                #region error handling [TODO]
                //TODO: buttons to be released here otherwise the user can't go on
                #endregion error handling

                #region logging
                //logging
                LogWriter.Entries.Add(new LogPair(LogType.debug, "[GUI] File (" + ofd.FileName + ") selected!"));

                //gui logging (user information)
                guiLog.setLog("File selected! --> Please make settings and confirm.");
                #endregion logging

                #region gui feedback
                //here a feedback is given to the gui for the user (info panel)
                //MainWindowBib.setTextBoxText(((MainWindow)Application.Current.MainWindow).tbFileName, init.config.filePath);

                //conversion to string, because stored as enumeration
                //((MainWindow)Application.Current.MainWindow).tbFileType.Text = init.config.fileType.ToString();
                #endregion gui feedback
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
            //background task
            e.Result = ReaderTerrain.ReadReb(init.config.filePath);
        }

        /// <summary>
        /// will be executed as soon as the (DoWorker) has read the dxf file
        /// </summary>
        private void BackgroundWorkerReb_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //disable btn process reb 
            this.btnProcessReb.IsEnabled = false;

            //delete all items in list box 
            this.lbRebSelect.Items.Clear();

            //if file fits to rebdata
            if (e.Result is RebDaData rebData)
            {
                //Assignment
                this.rebData = rebData;

                //traversing all horizons
                foreach (var horizon in rebData.GetHorizons())
                {
                    //list the horizon
                    this.lbRebSelect.Items.Add(horizon);
                }
                //gui logging (user information)
                //MainWindowBib.setGuiLog("Readed reb layers: " + lbRebSelect.Items.Count);
            }
            //if the file could not be read
            else
            {
                //go through all layers (one by one) of selected reb file
                this.rebData = null;

                //TODO add throw error + log
            }
            //apply layout - all items will be listed
            this.lbRebSelect.UpdateLayout();

            //release complete gui again
            IsEnabled = true;

            //set to json settings
            init.config.isTin = true;

            //set mouse cursor to default
            Mouse.OverrideCursor = null;

            //release selection
            this.lbRebSelect.IsEnabled = true;
        }
        #endregion background worker reb


        /// <summary>
        /// is executed as soon as the selection has been changed in the List Box (horizon)
        /// </summary>
        private void lbRebSelect_SelectionChanged(object sender, RoutedEventArgs e)
        {
            //check if an item was selected
            if (this.lbRebSelect.SelectedIndex != -1)
            {
                //enable process button
                this.btnProcessReb.IsEnabled = true;
            }
            //disable process button (user have to select horizon first)
            else
            {
                //disable 
                this.btnProcessReb.IsEnabled = false;
            }
        }

        /// <summary>
        /// Logic that is executed as soon as the "process key" is pressed
        /// </summary>
        private void btnProcessReb_Click(object sender, RoutedEventArgs e)
        {
            //blank json settings 
            init.config.layer = null;

            //get selected horizon
            int horizon = (int)this.lbRebSelect.SelectedItem;

            //passed to json settings
            init.config.horizon = horizon;

            //visual output on the GUI (layer selection) (need to convert to string!)
            //((MainWindow)Application.Current.MainWindow).tbLayerDtm.Text = horizon.ToString();

            //set task (file opening) to true
            GuiSupport.taskfileOpening = true;

            //[IfcTerrain] check if all task are allready done
            GuiSupport.readyState();

            //[DTM2BIM] check if all task are allready done
            GuiSupport.rdyDTM2BIM();

            //check if all task are allready done
            //MainWindowBib.enableStart(GuiHandler.GuiSupport.readyState()); (TODO)

            //gui logging (user information)
            guiLog.setLog("REB settings applyed.");

            return;
        }
    }
}

