﻿using System;
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
using IFCTerrainGUI.GUI.MainWindowLogic; //included to provide filepath insert into tb & for error handling (GUI)

namespace IFCTerrainGUI.GUI.REB
{
    /// <summary>
    /// Interaction logic for ucReadReb.xaml
    /// </summary>
    public partial class ucReadReb : UserControl
    {
        public ucReadReb()
        {
            InitializeComponent();

            //create "do" task and refernz to function
            backgroundWorkerReb.DoWork += BackgroundWorkerReb_DoWork; ;

            //create the task when the "do task" is completed
            backgroundWorkerReb.RunWorkerCompleted += BackgroundWorkerReb_RunWorkerCompleted; ;
        }

        /// <summary>
        /// string to process reb files
        /// </summary>
        private string[] fileName = new string[1];

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
                MainWindow.jSettings.fileType = BIMGISInteropLibs.IfcTerrain.IfcTerrainFileType.REB;

                //set JSON settings of file path
                MainWindow.jSettings.filePath = ofd.FileName;
                #endregion JSON settings

                //lock current MainWindow (because Background Worker is triggered)
                //so the user can not change any settings during the time the background worker is running
                ((MainWindow)Application.Current.MainWindow).IsEnabled = false;

                #region backgroundWorker

                fileName[0] = ofd.FileName;
                //kick off BackgroundWorker
                backgroundWorkerReb.RunWorkerAsync(ofd.FileName);
                #endregion backgroundWorker

                #region error handling [TODO]
                //TODO: buttons to be released here otherwise the user can't go on
                #endregion error handling

                #region logging [TODO]
                //TODO: add logging
                #endregion logging

                #region gui feedback
                //here a feedback is given to the gui for the user (info panel)
                MainWindowBib.setTextBoxText(((MainWindow)Application.Current.MainWindow).tbFileName, MainWindow.jSettings.filePath);

                //conversion to string, because stored as enumeration
                ((MainWindow)Application.Current.MainWindow).tbFileType.Text = MainWindow.jSettings.fileType.ToString();
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
            e.Result = ReaderTerrain.ReadReb(fileName);
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
                //TODO add reading log
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
            ((MainWindow)Application.Current.MainWindow).IsEnabled = true;

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
            MainWindow.jSettings.layer = null;

            //get selected horizon
            int horizon = (int)this.lbRebSelect.SelectedItem;

            //passed to json settings
            MainWindow.jSettings.horizon = horizon;

            //visual output on the GUI (layer selection) (need to convert to string!)
            ((MainWindow)Application.Current.MainWindow).tbLayerDtm.Text = horizon.ToString();

            //TODO gui logging

            //set task (file opening) to true
            MainWindowBib.taskfileOpening = true;

            //check if all task are allready done
            MainWindowBib.readyState();

            return;
        }
    }
}