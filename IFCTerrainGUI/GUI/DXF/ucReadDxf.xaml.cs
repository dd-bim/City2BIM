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
                //create "do" task and refernz to function
                backgroundWorkerDxf.DoWork += BackgroundWorkerDxf_DoWork;

                //create the task when the "do task" is completed
                backgroundWorkerDxf.RunWorkerCompleted += BackgroundWorkerDxf_RunWorkerCompleted;

                //kick off BackgroundWorker
                backgroundWorkerDxf.RunWorkerAsync(ofd.FileName);
                #endregion backgroundWorker

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
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorkerDxf_DoWork(object sender, DoWorkEventArgs e)
        {
            //background task: file reading
            e.Result = ReaderTerrain.ReadFile((string)e.Argument, out this.dxfFile) ? (string)e.Argument : "";

        }

        /// <summary>
        /// will be executed as soon as the (DoWorker) has read the dxf file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        /// TODO
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnProcessDxf_Click(object sender, RoutedEventArgs e)
        {
            MainWindowBib.setTextBoxText(((MainWindow)Application.Current.MainWindow).iPTBFileType, "DXF");
        }
    }
}
