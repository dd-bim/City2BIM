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

using Microsoft.Win32; //file dialog handling
using System.ComponentModel;//interface property changed
using System.Collections.ObjectModel; //observable collection
using SysPath = System.IO; // file path

using IxMilia.Dxf; //dxf processing
using Newtonsoft.Json;

namespace City2RVT.GUI.Surveyorsplan2BIM
{
    public enum logType
    {
        verbose,
        debug,
        info,
        warning,
        error
    }

    public enum geomType
    {
        Point,
        Line,
        MultiPoint,
        Polyline,
        Surface
    }

    public class enumType : INotifyPropertyChanged
    {
        /// <summary>
        /// do not rename (otherwise whole 'store' interface is not valid)
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private geomType _selectedMyEnumType;

        public geomType selectedMyEnumType
        {
            get { return _selectedMyEnumType; }
            set
            {
                _selectedMyEnumType = value;
                NotifyPropertyChanged(nameof(selectedMyEnumType));
            }
        }

        public IEnumerable<geomType> MyEnumTypeValues
        {
            get
            {
                return Enum.GetValues(typeof(geomType))
                    .Cast<geomType>();
            }
        }
    }

    public class logEntry : INotifyPropertyChanged
    {
        /// <summary>
        /// do not rename (otherwise whole 'store' interface is not valid)
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private logType _logType { get; set; }

        public logType logType
        {
            get { return _logType; }
            set
            {
                _logType = value;
                NotifyPropertyChanged(nameof(logType));
            }
        }

        private string _logMessage { get; set; }

        public string logMessage
        {
            get { return _logMessage; }
            set
            {
                _logMessage = value;
                NotifyPropertyChanged(nameof(logMessage));
            }
        }

    }

    public class store : INotifyPropertyChanged
    {
        /// <summary>
        /// do not rename (otherwise whole 'store' interface is not valid)
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #region attributes
        private string _dxfFilePath { get; set; }

        public string dxfFilePath
        {
            get { return _dxfFilePath; }
            set
            {
                _dxfFilePath = value;
                NotifyPropertyChanged(nameof(dxfFilePath));
            }
        }

        private string _dxfFileName { get; set; }

        public string dxfFileName
        {
            get { return _dxfFileName; }
            set
            {
                _dxfFileName = value;
                NotifyPropertyChanged(nameof(dxfFileName));
            }
        }


        private DxfFile _dxfFile { get; set; }

        [JsonIgnore] //this will be used to ignore this value for json parsing
        public DxfFile dxfFile
        {
            get { return _dxfFile; }
            set
            {
                _dxfFile = value;
                NotifyPropertyChanged(nameof(dxfFile));
            }
        }

        private string _rfaDir { get; set; }

        public string rfaDir
        {
            get { return _rfaDir; }
            set
            {
                _rfaDir = value;
                NotifyPropertyChanged(nameof(rfaDir));
            }
        }

        private string _rfaDirName { get; set; }

        public string rfaDirName
        {
            get { return _rfaDirName; }
            set
            {
                _rfaDirName = value;
                NotifyPropertyChanged(nameof(rfaDirName));
            }
        }

        private List<SysPath.FileInfo> _fileInfos { get; set; }

        public List<SysPath.FileInfo> fileInfos
        {
            get { return _fileInfos; }
            set
            {
                _fileInfos = value;
                NotifyPropertyChanged(nameof(fileInfos));
            }
        }
        #endregion attributes
    }

    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class Surveyorsplan_ImportUI : Window
    {
        #region UI properties
        /// <summary>
        /// [Databinding] store all information will be exported to json
        /// </summary>
        public ObservableCollection<store> store { get; set; }

        /// <summary>
        /// [Databinding] logging entries
        /// </summary>
        public ObservableCollection<logEntry> logEntries { get; set; }

        /// <summary>
        /// view to manage data
        /// </summary>
        public ICollectionView view { get; set; }
        #endregion UI properties

        public Surveyorsplan_ImportUI()
        {
            //init UI
            InitializeComponent();

            //init databinding
            initDatabindung();

            //init backgroundworker dxf - need to be started later (just "prepare" them)
            initBackgroundWorkerDxf();

            //init backgroundworker rfa
            initBackgroundWorkerRfa();
        }
        #region databinding

        /// <summary>
        /// auxiliary function to set the databinding to the different elements
        /// </summary>
        private void initDatabindung()
        {
            //init observable collection store
            store = new ObservableCollection<store>();

            //set data context for store (otherwise will be empty)
            survMain.DataContext = store;

            //add empty store
            store.Add(new store());

            //init obs coll for logging
            logEntries = new ObservableCollection<logEntry>();

            //set data context to observable collection
            gbLogging.DataContext = logEntries;

            //init class
            var enumType = new enumType();

            //set item source via request of class
            cbGeomType.ItemsSource = enumType.MyEnumTypeValues;
        }
        #endregion

        #region DXF import
        private void btnLoadDxf_Click(object sender, RoutedEventArgs e)
        {
            //file dialog
            var ofd = new OpenFileDialog();

            //set file filter
            ofd.Filter = "DXF Files *.dxf, *.dxb|*.dxf;*.dxb";

            //open dialog window
            if (ofd.ShowDialog() == true)
            {
                readDxfFile(ofd.FileName);
            }
        }

        private void LoadDxf_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files != null && files.Length != 0)
                {
                    readDxfFile(files[0]);
                    return;
                }
                else
                {
                    addLog(logType.error, "File reading failed.");
                }
            }
        }

        /// <summary>
        /// read dxf file and bind to store item
        /// </summary>
        /// <param name="file">file path</param>
        private bool readDxfFile(string file)
        {
            //get current store
            var item = store.LastOrDefault();

            if (file.Length != 0)
            {
                //set file path
                item.dxfFilePath = file;

                //set file name
                item.dxfFileName = SysPath.Path.GetFileNameWithoutExtension(item.dxfFilePath);

                //gui logging
                addLog(logType.info, "DXF file imported.");

                //kick off background worker to read file & list all layer
                backgroundWorkerDxf.RunWorkerAsync(item.dxfFilePath);

                return true;
            }
            else
            {
                addLog(logType.error, "DXF file is not valid.");
                return false;
            }
        }

        /// <summary>
        /// list all block layer and bind them
        /// </summary>
        private void lbDxfLayer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //get "view" of layer
            view = CollectionViewSource.GetDefaultView(lbDxfLayer.ItemsSource);

            //get current layer from selection
            DxfLayer dxfLayer = view.CurrentItem as DxfLayer;

            //set view to data context
            view = CollectionViewSource.GetDefaultView(survMain.DataContext);

            //get entites from selected layer 
            List<IxMilia.Dxf.Entities.DxfEntity> dxfEntites = (view.CurrentItem as store).dxfFile.Entities.Where(l => l.Layer == dxfLayer.Name).ToList();

            //


        }
        #endregion DXF import

        #region rfa
        private void btnRfaDir_Click(object sender, RoutedEventArgs e)
        {
            //add new FileDialog handler
            var ofd = new System.Windows.Forms.FolderBrowserDialog();

            //open dialog
            ofd.ShowDialog();

            //if string is not empty
            if (ofd.SelectedPath != string.Empty)
            {
                readRfaDir(ofd.SelectedPath);


            }
        }



        /// <summary>
        /// 
        /// </summary>
        private void RfaDir_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files != null && files.Length != 0)
                {
                    readRfaDir(files[0]);
                    return;
                }
                else
                {
                    addLog(logType.error, "File reading failed.");
                }
            }
        }

        private void readRfaDir(string dir)
        {
            view = CollectionViewSource.GetDefaultView(survMain.DataContext);

            //get current store
            if (dir.Length != 0)
            {
                //set rfa dir
                (view.CurrentItem as store).rfaDir = dir;

                //set rfa directory name
                (view.CurrentItem as store).rfaDirName = SysPath.Path.GetFileName(dir);

                //logging
                addLog(logType.info, "Revit family directory set.");

                //kick off background worker to list all rfa files
                backgroundWorkerRfa.RunWorkerAsync((view.CurrentItem as store).rfaDir);
                return;
            }
            else
            {
                addLog(logType.error, "Revit family directory is not valid.");
            }
        }
        #endregion rfa

        #region logging
        /// <summary>
        /// add log message to GUI
        /// </summary>
        /// <param name="logType"></param>
        /// <param name="message"></param>
        private void addLog(logType logType, string message)
        {
            logEntries.Add(new logEntry()
            {
                logType = logType,
                logMessage = message
            });

            Console.WriteLine(message);
        }
        #endregion logging

        #region background worker dxf
        private static readonly BackgroundWorker backgroundWorkerDxf = new BackgroundWorker();

        /// <summary>
        /// init backgroundworker with "do" task 
        /// <para>also add "run" task as soon as "do" is done</para>
        /// </summary>
        private void initBackgroundWorkerDxf()
        {
            backgroundWorkerDxf.DoWork += backgroundWorkerDxf_DoWork;
            backgroundWorkerDxf.RunWorkerCompleted += backgroundWorkerDxf_RunWorkerCompleted;
        }

        /// <summary>
        /// open dxf file 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundWorkerDxf_DoWork(object sender, DoWorkEventArgs e)
        {
            //get current store item
            var item = store.LastOrDefault();

            //if file exsists get file name if not: set to null
            e.Result = SysPath.File.Exists((string)e.Argument) ? (string)e.Argument : null;

            //if file exsists
            if (e.Result != null)
            {
                //
                try
                {
                    //use filestream to open dxf file
                    using (var fileStream = new SysPath.FileStream(item.dxfFilePath, SysPath.FileMode.Open))
                    {
                        //add dxf fuke to store item
                        item.dxfFile = DxfFile.Load(fileStream);
                    }
                }
                catch (Exception ex)
                {
                    addLog(logType.error, "[DXF]: " + ex.Message);
                    return;
                }
            }
        }

        /// <summary>
        /// will be executed as soon as file reading is completed 
        /// </summary>
        private void backgroundWorkerDxf_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //list dxf layer
            listDxfLayer();

            //list dxf blocks
            listDxfBlocks();
        }

        /// <summary>
        /// auxilary function to list all layer in listbox
        /// </summary>
        /// <param name="dxfFile">dxf file</param>
        private void listDxfLayer()
        {
            //get current view
            view = CollectionViewSource.GetDefaultView(survMain.DataContext);

            //add dxf layer as source
            lbDxfLayer.ItemsSource = (view.CurrentItem as store).dxfFile.Layers;

            //refresh view
            view.Refresh();

            //add gui logging message
            addLog(logType.debug, "[DXF] file readed - layers: " + lbDxfLayer.Items.Count);
        }

        private void listDxfBlocks()
        {
            //get "view" of layer
            view = CollectionViewSource.GetDefaultView(survMain.DataContext);

            //get blocks
            var blocks = (view.CurrentItem as store).dxfFile.Blocks.Where(l => l.HasAttributeDefinitions == true);

            //add blocks as source
            lbDxf.ItemsSource = blocks;

            //refresh view to apply 
            view.Refresh();

            //add gui logging message
            addLog(logType.debug, "[DXF] file readed - blocks: " + lbDxf.Items.Count);
        }
        #endregion background worker

        #region background worker rfa
        private static readonly BackgroundWorker backgroundWorkerRfa = new BackgroundWorker();

        private void initBackgroundWorkerRfa()
        {
            backgroundWorkerRfa.DoWork += backgroundWorkerRfa_DoWork;
            backgroundWorkerRfa.RunWorkerCompleted += backgroundWorkerRfa_RunWorkerCompleted;
        }


        private void backgroundWorkerRfa_DoWork(object sender, DoWorkEventArgs e)
        {
            var rfaFilePath = e.Argument.ToString();
            //(view.CurrentItem as store).rfaDir;

            //get method to get directory
            var directoryInfo = new SysPath.DirectoryInfo(rfaFilePath);

            //read rfa files
            SysPath.FileInfo[] rvtFiles = directoryInfo.GetFiles("*.rfa", SysPath.SearchOption.AllDirectories);

            //list result
            e.Result = rvtFiles;
        }

        private void backgroundWorkerRfa_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            dynamic results = e.Result;

            List<SysPath.FileInfo> files = new List<SysPath.FileInfo>();

            foreach (SysPath.FileInfo result in results)
            {
                files.Add(result);
            }

            (view.CurrentItem as store).fileInfos = files;

            lbRfaFiles.ItemsSource = (view.CurrentItem as store).fileInfos;
        }
        #endregion

        #region mapping
        private void btnRemoveSelection_Click(object sender, RoutedEventArgs e)
        {
            if (lbDxf.SelectedIndex != -1)
            {
                lbDxf.UnselectAll();
            }
        }

        private void btnAddMapping_Click(object sender, RoutedEventArgs e)
        {
            view = CollectionViewSource.GetDefaultView(lbDxf.DataContext);

            var block = (view.CurrentItem as store).dxfFile.Blocks.Where(b => b.Name == (lbDxf.SelectedItem as IxMilia.Dxf.Blocks.DxfBlock).Name);

        }




        #endregion
    }
}