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
using System.ComponentModel; //used for background worker
using BIMGISInteropLibs.DXF; //include to read dxf file
using IxMilia.Dxf; //need to handle dxf files

namespace IFCTerrainGUI.GUI.DXF
{
    /// <summary>
    /// Interaction logic for ucReadDxf.xaml
    /// </summary>
    public partial class ucReadDxf : UserControl
    {
        /// <summary>
        /// create instance of the gui
        /// </summary>
        public ucReadDxf()
        {
            //init gui panel
            InitializeComponent();

                            //create "do" task and refernz to function
                backgroundWorkerDxf.DoWork += BackgroundWorkerDxf_DoWork;

                //create the task when the "do task" is completed
                backgroundWorkerDxf.RunWorkerCompleted += BackgroundWorkerDxf_RunWorkerCompleted;
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

                #region backgroundWorker
                //kick off BackgroundWorker
                backgroundWorkerDxf.RunWorkerAsync(ofd.FileName);
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
                return; //do not add anything after this
            }
            return; //do not add anything after this
        }

        /// <summary>
        /// BackgroundWorker (DXF): used to read dxf file and list up all layers
        /// </summary>
        private readonly BackgroundWorker backgroundWorkerDxf = new BackgroundWorker();

        /// <summary>
        /// dxf file which is read
        /// </summary>
        private DxfFile dxfFile = null;


        /// <summary>
        /// reading dxf file
        /// </summary>
        private void BackgroundWorkerDxf_DoWork(object sender, DoWorkEventArgs e)
        {
            //background task: file reading
            e.Result = ReaderTerrain.ReadFile((string)e.Argument, out this.dxfFile) ? (string)e.Argument : "";
        }

        /// <summary>
        /// will be executed as soon as the (DoWorker) has read the dxf file
        /// </summary>
        private void BackgroundWorkerDxf_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //file name
            string name = (string)e.Result;

            //delete all items in list box (not in dxf file :) )
            this.lbDxfDtmLayer.Items.Clear();
            this.lbDxfBreaklineLayer.Items.Clear();

            //check if the file could not be read
            if (string.IsNullOrEmpty(name))
            {
                //set dxf file to "empty"
                this.dxfFile = null;

                //TODO add throw error + log
            }
            //will be executed if the file name is not empty
            else
            {
                //go through all layers (one by one) of select dxf file
                foreach (var l in this.dxfFile.Layers)
                {
                    //list layer name to list boxes (so the user can select a layer (or more))
                    this.lbDxfDtmLayer.Items.Add(l.Name);
                    this.lbDxfBreaklineLayer.Items.Add(l.Name);
                }
            }
            //so all items will be listed
            this.lbDxfDtmLayer.UpdateLayout();
            this.lbDxfBreaklineLayer.UpdateLayout();

            //TODO Logging idea ... count of layer readed (listbox.items.count())

            //Release MainWindow again --> so the user can make entries again
            ((MainWindow)Application.Current.MainWindow).IsEnabled = true;
        }

        /// <summary>
        /// Executed when break edge processing was enabled
        /// </summary>
        private void rbDxfBreaklinesTrue_Checked(object sender, RoutedEventArgs e)
        {
            //disable start button (so the user can't go on --> first of all a breakline layer have to be selected)
            ((MainWindow)Application.Current.MainWindow).btnStart.IsEnabled = false;

            //activate list box so the user can select a layer (where the breaklines are stored
            this.lbDxfBreaklineLayer.IsEnabled = true;

            //btn process dxf disable (Reason: the user has made a decision because of the breaking edges)
            this.btnProcessDxf.IsEnabled = false;
        }

        /// <summary>
        /// Executed when break edge processing was not enabled
        /// </summary>
        private void rbDxfBreaklinesFalse_Checked(object sender, RoutedEventArgs e)
        {
            //disable list box (not needed)
            this.lbDxfBreaklineLayer.IsEnabled = false;

            //btn process dxf enable (Reason: the user has made a decision because of the breaking edges)
            this.btnProcessDxf.IsEnabled = true;
        }

        
        /// <summary>
        /// is executed as soon as the selection has been changed in the ListBox lbDxfDtmLayer
        /// </summary>
        private void lbDxfDtmLayer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //save all selected items as variable
            var listSelectedItems = ((ListBox)sender).SelectedItems;

            //as soon as at least one "item" has been selected
            if (listSelectedItems.Count > 0)
            {
                //activate so that selection for break edges is possible
                gbDxfBreakline.IsEnabled = true;
            }

            //if no item is selected --> required for file handling
            else
            {
                //if no item is selected, the selection for break edges is disabled
                gbDxfBreakline.IsEnabled = false;

                //button dxf processing deactivate
                btnProcessDxf.IsEnabled = false;
            }
        }

        /// <summary>
        /// is executed as soon as the selection has been changed in the ListBox lbDxfBreaklineLayer
        /// </summary>
        private void lbDxfBreaklineLayer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //save selected item as variable
            var listSelectedItem = ((ListBox)sender).SelectedItems;

            //as one "item" has been selected
            if (listSelectedItem.Count != 0)
            {
                //activate btn process
                btnProcessDxf.IsEnabled = true;
            }
            else
            {
                //deactivate btn process
                btnProcessDxf.IsEnabled = true;
            }
        }

        /// <summary>
        /// Logic that is executed as soon as the "process key" is pressed
        /// </summary>
        private void btnProcessDxf_Click(object sender, RoutedEventArgs e)
        {
            //blank text boxes otherwise (all layers are added if the process button was clicked multiple times in one session)
            ((MainWindow)Application.Current.MainWindow).tbLayerDtm.Text = null;
            ((MainWindow)Application.Current.MainWindow).tbFileSpecific.Text = null;

            //blank json settings 
            MainWindow.jSettings.layer = null;

            //is executed as soon as a DXF layer is selected
            if (this.lbDxfDtmLayer.SelectedIndex >= 0)
            {
                //array of selected layers                
                string[] dxfSelectedItems = lbDxfDtmLayer.SelectedItems.OfType<string>().ToArray();

                //text box output dtm layer (only for user information) (loop through each selected item in the list)
                foreach (string item in lbDxfDtmLayer.SelectedItems)
                {
                    //passed to json settings
                    //MainWindow.jSettings.layer += item + ";";

                    MainWindow.jSettings.layer = item;

                    //visual output on the GUI (layer selection)
                    ((MainWindow)Application.Current.MainWindow).tbLayerDtm.Text += item + "; ";
                    
                    //TODO (gui logging / file logging)
                }

                //will be executed if "yes" was selected for break edge processing d
                if (rbDxfBreaklinesTrue.IsChecked == true)
                {
                    //(even if only one layer was selected), otherwise system variables are output
                    foreach (string item in lbDxfBreaklineLayer.SelectedItems)
                    {
                        #region json settings
                        //passed to json settings - breakline (bool)
                        MainWindow.jSettings.breakline = true;

                        //passed to json settings - breakline (layer)
                        MainWindow.jSettings.breakline_layer = item.ToString();
                        #endregion json settings

                        #region gui (user information)
                        //information panel 
                        ((MainWindow)Application.Current.MainWindow).ipFileSpecific.Text = "Breakline - Layer";
                        //information panel output break line layer
                        ((MainWindow)Application.Current.MainWindow).tbFileSpecific.Text += item.ToString();
                        #endregion gui (user information)
                    }
                }
                //execute, because in a session the break edge processing can also be deactivated again
                else
                {
                    //passed to json settings - breakline (bool = false); reason: user hasn't selected breakline processing
                    MainWindow.jSettings.breakline = false;
                }

                //Selection accordingly in isTin (set json settings) is needed at the ConnectionInterface to decide which dxf reader to use
                if (rbDxfFaces.IsChecked == true)
                {
                    //processing faces (true)
                    MainWindow.jSettings.isTin = true;
                }
                else
                {
                    //processing points / lines (false)
                    MainWindow.jSettings.isTin = false;
                }
                /*
                 * Should it be necessary to implement more distinctions, then the case distinctions should run via switch cases 
                 * and be converted in the JSON settings via an enumeration.
                 */

                //set task (file opening) to true
                MainWindowBib.taskfileOpening = true;

                //check if all task are allready done
                MainWindowBib.readyState();
            }
            return;
        }

    }
}
