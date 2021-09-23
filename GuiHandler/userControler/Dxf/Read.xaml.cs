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
using System.IO;

//Include user-specific libraries from here onwards

using Microsoft.Win32; //used for file handling
using System.ComponentModel; //used for background worker
using IxMilia.Dxf; //need to handle dxf files

//embed for file logging
using BIMGISInteropLibs.Logging;                                    //acess to logger
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain;    //to set log messages

//shortcut to set logging messages
using support = GuiHandler.GuiSupport;

using System.Collections.ObjectModel; //observable collection

namespace GuiHandler.userControler.Dxf
{
    /// <summary>
    /// class to store dxf file
    /// </summary>
    public class store : INotifyPropertyChanged
    {
        /// <summary>
        /// event handler (!) do not rename
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// check if property is "really" changed
        /// </summary>
        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        /// <summary>
        /// local internal storage to store dxf file
        /// </summary>
        private DxfFile _dxfFile { get; set; }

        /// <summary>
        /// set dxf file
        /// </summary>
        public DxfFile dxfFile
        {
            get { return _dxfFile; }
            set
            {
                _dxfFile = value;
                NotifyPropertyChanged(nameof(dxfFile));
            }
        }

        /// <summary>
        /// local storage for file path
        /// </summary>
        private string _dxfFilePath { get; set; }

        /// <summary>
        /// store file path
        /// </summary>
        public string dxfFilePath
        {
            get { return _dxfFilePath; }
            set
            {
                _dxfFilePath = value;
                NotifyPropertyChanged(nameof(dxfFilePath));
            }
        }
    }

    /// <summary>
    /// Interaction logic for ucReadDxf.xaml
    /// </summary>
    public partial class Read : UserControl
    {

        /// <summary>
        ///init collection (needed for data binding)
        /// </summary>
        public ObservableCollection<store> store { get; set; }

        /// <summary>
        /// data manage
        /// </summary>
        private ICollectionView view { get; set; }

        /// <summary>
        /// create instance of the gui
        /// </summary>
        public Read()
        {
            //init store
            store = new ObservableCollection<store>();

            //set data context to grid
            store.Add(new store());

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
        private void btnReadDxf_Click(object sender, RoutedEventArgs e)
        {
            //add new FileDialog handler 
            OpenFileDialog ofd = new OpenFileDialog();
            //set filtering so that the following selection is possible (these also represent only the selected files)
            ofd.Filter = "DXF Files *.dxf, *.dxb|*.dxf;*.dxb";

            //opens the dialog window (if a file is selected, everything inside the loop is executed)
            if (ofd.ShowDialog() == true)
            {
                //get current store
                var item = store.LastOrDefault(); //do not need 'as' it is implizit inculded

                //bind file path
                item.dxfFilePath = ofd.FileName;

                //kick off BackgroundWorker
                backgroundWorkerDxf.RunWorkerAsync(ofd.FileName);

                //lock current MainWindow (because Background Worker is triggered)
                //so the user can not change any settings during the time the background worker is running
                IsEnabled = false;

                //change cursor to wait animation (for user feedback)
                Mouse.OverrideCursor = Cursors.Wait;

                #region logging
                //logging
                LogWriter.Entries.Add(new LogPair(LogType.debug, "[GUI] File (" + ofd.FileName + ") selected!"));
                #endregion logging
                return; //do not add anything after this
            }
            return; //do not add anything after this
        }

        /// <summary>
        /// BackgroundWorker (DXF): used to read dxf file and list up all layers
        /// </summary>
        private readonly BackgroundWorker backgroundWorkerDxf = new BackgroundWorker();

        /// <summary>
        /// reading dxf file
        /// </summary>
        private void BackgroundWorkerDxf_DoWork(object sender, DoWorkEventArgs e)
        {
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[GUI] Background Worker DXF - started!"));

            //get current store
            var item = store.LastOrDefault();

            //background task: file reading
            e.Result = File.Exists((string)e.Argument) ? (string)e.Argument : null;

            if(e.Result != null)
            {
                try
                {
                    using (var fileStream = new FileStream(item.dxfFilePath, FileMode.Open))
                    {
                        //open dxf file
                        item.dxfFile = DxfFile.Load(fileStream);

                        LogWriter.Entries.Add(new LogPair(LogType.debug, "[GUI] DXF file readed!"));
                    }
                }
                catch(Exception ex)
                {
                    e.Cancel = true;
                    LogWriter.Entries.Add(new LogPair(LogType.error, "[GUI] DXF file reading: "+ ex.Message));
                    return;
                }
            }
        }

        /// <summary>
        /// will be executed as soon as the (DoWorker) has read the dxf file
        /// </summary>
        private void BackgroundWorkerDxf_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //enable UI
            IsEnabled = true;

            //change cursor to wait animation (for user feedback)
            Mouse.OverrideCursor = null;

            if (e.Cancelled)
            {
                support.setLog(LogType.error, "File reading failed!");

                //ERROR --> do not try to list layers
                return;
            }

            //define view
            view = CollectionViewSource.GetDefaultView(store);
           
            //get source
            var dxfLayers = (view.CurrentItem as store).dxfFile.Layers;

            //init list store store layer
            List<string> layerTitle = new List<string>();

            foreach(var dxfLayer in dxfLayers)
            {
                layerTitle.Add(dxfLayer.Name);
            }

            //bind list to item source
            lbDxfDtmLayer.ItemsSource = layerTitle;
            lbDxfBreaklineLayer.ItemsSource = layerTitle;

            //refresh view (otherwise will not be shown in GUI)
            view.Refresh();

            //get config
            var config = DataContext as BIMGISInteropLibs.IfcTerrain.Config;

            //set file path and get file name
            config.filePath = store.LastOrDefault().dxfFilePath;
            config.fileName = Path.GetFileName(config.filePath);

            //logging
            LogWriter.Entries.Add(new LogPair(LogType.debug, "[GUI] Background Worker DXF - completed!"));
            LogWriter.Entries.Add(new LogPair(LogType.debug, "[GUI] Background Worker DXF - readed layers: " + lbDxfDtmLayer.Items.Count));

            //gui logging (user information)
            support.setLog(LogType.info, "Readed dxf layers: " + lbDxfDtmLayer.Items.Count);
        }
    }
}