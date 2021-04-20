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

using IFCTerrainGUI.GUI.MainWindowLogic; //used to outsource auxiliary functions

//integrate logic from BIMGISInteropsLibs 
using BIMGISInteropLibs.IfcTerrain; //used for JsonSettings, ...
using System.IO; //used for file handling (e.g. open directory)
using System.Diagnostics; //used for file explorer opening
using Newtonsoft.Json; //used for serialize the json file
using Newtonsoft.Json.Linq;
using Microsoft.Win32;  //file handing (storage location)
using System.ComponentModel; //used for background worker

//logging
using BIMGISInteropLibs.Logging; //access to log writer
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

namespace IFCTerrainGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// initialize main gui
        /// </summary>
        public MainWindow()
        {
            //all components of the GUI are created (never remove)
            InitializeComponent();

            //add tasks for background worker (konversion)
            backgroundWorkerIfc.DoWork += BackgroundWorkerIfc_DoWork;
            backgroundWorkerIfc.RunWorkerCompleted += BackgroundWorkerIfc_RunWorkerCompleted;

            //add gui logging
            tbGuiLogging.Items.Add("Welcome to IFCTerrain!");

            LogWriter.Entries.Add(new LogPair(LogType.verbose, "GUI initialized."));
        }
        /// <summary>
        /// create an instance for Json Settings (getter + setter) <para/>
        /// mainly used to create interaction between command / GUI and readers & writers <para/>
        /// </summary>
        public static JsonSettings jSettings = new JsonSettings();

        /// <summary>
        /// create an instance for JSON Settings (getter + setter) <para/>
        /// mainly used to export metadata acording to DIN SPEC 91391-2 <para/>
        /// </summary>
        public static JsonSettings_DIN_SPEC_91391_2 jSettings91391 { get; set; } = new JsonSettings_DIN_SPEC_91391_2();

        /// <summary>
        /// create an instance for JSON Settings (getter + setter) <para/>
        /// mainly used to export metadata acording to DIN SPEC 18740-6 <para/>
        /// </summary>
        public static JsonSettings_DIN_18740_6 jSettings18740 { get; set; } = new JsonSettings_DIN_18740_6();

        /// <summary>
        /// 
        /// </summary>
        public Result result { get; set; }
        
        /// <summary>
        /// opens documentation
        /// </summary>
        private void tbDocumentation_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //direct Link to GITHUB - Repro so it should be accessable for "all"
            string docuPath = "https://github.com/dd-bim/City2BIM/wiki/IFC-Terrain";
            //opens link
            System.Diagnostics.Process.Start(docuPath);
        }

        /// <summary>
        /// Sets the location of the IFC file (via JSON settings)
        /// </summary>
        private void btnChooseStorageLocation_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Industry Fundation Classes| *.ifc|IFCXML File | *.ifcXML|IFCZIP | *.ifcZIP";
            //open file handler
            if (sfd.ShowDialog() == true)
            {
                //use for selected IFC (STEP) or ifxXML here --> thus case differentiation becomes possible
                switch (sfd.FilterIndex)
                {
                    //jump to this case if STEP was selected
                    case 1:
                        //json settings                        
                        jSettings.outFileType = BIMGISInteropLibs.IFC.IfcFileType.Step;

                        //set settings for DIN SPEC 91391
                        jSettings91391.mimeType = "application/x-step";
                        break;
                    //jump to this case if ifcXML was selected
                    case 2:
                        //json setting file format
                        jSettings.outFileType = BIMGISInteropLibs.IFC.IfcFileType.ifcXML;

                        //set settings for DIN SPEC 91391
                        jSettings91391.mimeType = "application/xml";
                        break;
                    //jump to this case if ifcXML was selected
                    case 3:
                        //json setting file format
                        jSettings.outFileType = BIMGISInteropLibs.IFC.IfcFileType.ifcZip;

                        //set settings for DIN SPEC 91391
                        jSettings91391.mimeType = "application/zip";
                        break;
                }
                //below settings regardless of format

                //gui information
                MainWindowBib.setTextBoxText(tbIfcDir, sfd.FileName);
                MainWindowBib.setGuiLog("Storage location set.");

                //set filepath to jSettings
                jSettings.destFileName = sfd.FileName;

                //set task (file opening) to true
                MainWindowBib.selectStoreLocation = true;

                //check if all task are allready done
                MainWindowBib.readyState();

                //set logging path
                string logPath = System.IO.Path.GetDirectoryName(jSettings.destFileName);
                jSettings.logFilePath = logPath;

                //logging
                LogWriter.Entries.Add(new LogPair(LogType.verbose, "[GUI] Storage location set to: " + jSettings.logFilePath));
            }
            else
            {
                //set task (file opening) to true
                MainWindowBib.selectStoreLocation = false;

                //check to deactivate start button
                MainWindowBib.readyState();

                //logging
                LogWriter.Entries.Add(new LogPair(LogType.warning, "[GUI] Storage location was not set."));
            }
            return;
        }
        /// <summary>
        /// Start the conversion (based on the settings the user made)
        /// </summary>
        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            //json settings set 3D to true [TODO]
            jSettings.is3D = true;
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[GUI][JsonSetting] set 'is3D'-value to default (true)"));

            //json settings set minDist to 1.0 (default value) [TODO]
            jSettings.minDist = 1.0;
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[GUI][JsonSetting] set 'minDist'-value to default (1.0 m)"));

            //read export specific settings
            //get filepath
            string dirPath = System.IO.Path.GetDirectoryName(jSettings.destFileName);
            string dirName = System.IO.Path.GetFileNameWithoutExtension(jSettings.destFileName);

            string fileType = jSettings.fileType.ToString();
            string ifcVersion = jSettings.outIFCType.ToString();
            string shape = jSettings.surfaceType.ToString();

            #region metadata
            //will be executed if user select export of meta data
            if (jSettings.exportMetadataFile)
            {
                try
                {
                    LogWriter.Entries.Add(new LogPair(LogType.verbose, "[GUI][Metadata] export started."));
                    //init vars for export settings
                    var export913912 = new JProperty("DIN SPEC 91391-2", "NOT EXPORTED");
                    var export187406 = new JProperty("DIN 18740-6", "NOT EXPORTED");

                    //check if metadata should be exported according to DIN 91391-2
                    if (jSettings.exportMetadataDin91391)
                    {
                        LogWriter.Entries.Add(new LogPair(LogType.verbose, "[GUI][Metadata]***DIN SPEC 91391-2***"));
                        //Assignment all obligatory variables
                        //set file name
                        jSettings91391.name = System.IO.Path.GetFileName(jSettings.destFileName);

                        //set mime type
                        jSettings91391.mimeType = "application/x-step";

                        //set export string
                        export913912 = new JProperty("DIN SEPC 91391-2", JObject.FromObject(jSettings91391));

                        LogWriter.Entries.Add(new LogPair(LogType.verbose, "[GUI][Metadata] set all meta data to JsonProperty."));

                    }

                    //check if metadata should be exported according to DIN 18740-6
                    if (jSettings.exportMetadataDin18740)
                    {
                        //set export string
                        export187406 = new JProperty("DIN 18740-6", JObject.FromObject(jSettings18740));
                        LogWriter.Entries.Add(new LogPair(LogType.verbose, "[GUI][Metadata] ***DIN 18740-6***"));
                        LogWriter.Entries.Add(new LogPair(LogType.verbose, "[GUI][Metadata] set all meta data to JsonProperty."));
                    }

                    //build objects (here you can add, if needed more objects)
                    JObject meta = new JObject(export913912, export187406);

                    //write it to json file (TODO: add path)
                    //File.WriteAllText(@"D:\test.json", meta.ToString());
                    File.WriteAllText(dirPath + @"\" + dirName + "_metadata.json", meta.ToString());

                    LogWriter.Entries.Add(new LogPair(LogType.info, "[GUI][Metadata] exported metadata to following path: " + dirPath));

                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Write(ex.Message.ToString() + Environment.NewLine);
                    LogWriter.Entries.Add(new LogPair(LogType.error, "Metadata - processing: " + ex.Message.ToString()));
                }
            }

            #endregion metadata

            //serialize json file
            try
            {
                LogWriter.Entries.Add(new LogPair(LogType.info, "[GUI][JsonSettings] start serializing json"));

                //convert to json object
                string jExportText = JsonConvert.SerializeObject(jSettings, Formatting.Indented);

                //export json settings
                File.WriteAllText(dirPath + @"\config_" + fileType + "_" + ifcVersion + "_" + shape + ".json", jExportText);

                LogWriter.Entries.Add(new LogPair(LogType.info, "[GUI][JsonSettings] exported to following path: " + dirPath));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex.Message.ToString() + Environment.NewLine);

                LogWriter.Entries.Add(new LogPair(LogType.error, "Json Config - processing: " + ex.Message.ToString()));
            }

            //lock MainWindow 
            this.IsEnabled = false;

            //set mouse cursor to wait
            Mouse.OverrideCursor = Cursors.Wait;

            //kick off background worker ifc
            backgroundWorkerIfc.RunWorkerAsync();
        }

        #region background worker
        /// <summary>
        /// BackgroundWorker (IFC): used to start the conversion to IFC file
        /// </summary>
        private readonly BackgroundWorker backgroundWorkerIfc = new BackgroundWorker();

        /// <summary>
        /// start conversion (using the JSON settings)
        /// </summary>
        private void BackgroundWorkerIfc_DoWork(object sender, DoWorkEventArgs e)
        {
            //Interface between GUI, reader and writer
            ConnectionInterface conInt = new ConnectionInterface();

            //logging
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[BackgroundWorker][IFC] started."));

            //start mapping process which currently begins with the selection of the file reader
            result = conInt.mapProcess(jSettings, jSettings91391);
        }

        /// <summary>
        /// Executed after the conversion is done
        /// TODO: catching errors & output to user
        /// TODO: LOGGING
        /// </summary>
        private void BackgroundWorkerIfc_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //release the MainWindow (conversion is completed)
            this.IsEnabled = true;

            //enable open storage button
            this.btnOpenStore.IsEnabled = true;

            //set mouse cursor to default
            Mouse.OverrideCursor = null;

            //logging stat
            double numPoints = (double)result.wPoints / (double)result.rPoints;
            double numFaces = (double)result.wFaces / (double)result.rFaces;
            MainWindowBib.setGuiLog("Conversion completed!");
            MainWindowBib.setGuiLog("Results: " + result.wPoints + " points (" + Math.Round(numPoints * 100, 2) + " % )");
            MainWindowBib.setGuiLog("and "+ result.wFaces + " Triangles(" + Math.Round(numFaces * 100, 2) + " %) processed.");
        }
        #endregion background worker

        /// <summary>
        /// swtiching verbosity levels
        /// </summary>
        private void selectVerbosityLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //check and set the choosen verbo level into json settings
            if (minVerbose.IsSelected)
            {
                jSettings.verbosityLevel = LogType.verbose;
            }
            else if (minDebug.IsSelected)
            {
                jSettings.verbosityLevel = LogType.debug;
            }
            else if (minInformation.IsSelected)
            {
                jSettings.verbosityLevel = LogType.info;
            }
            else if (minWarning.IsSelected)
            {
                jSettings.verbosityLevel = LogType.warning;
            }

            //logging
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "Verbosity level changed to: " + jSettings.verbosityLevel.ToString()));
        }

        /// <summary>
        /// function to open storage location
        /// </summary>
        private void btnOpenStore_Click(object sender, RoutedEventArgs e)
        {
            //get file path
            string path = System.IO.Path.GetDirectoryName(jSettings.destFileName);

            //check if 'path' exsists
            if (Directory.Exists(path))
            {
                //open explorer
                Process.Start("explorer.exe", path);
            }
            else
            {
                //gui logging (user information)
                tbGuiLogging.Items.Add("File path could not be opened!");
            }
        }


        //source: https://stackoverflow.com/questions/2337822/wpf-listbox-scroll-to-end-automatically
        /// <summary>
        /// update list box (scroll at the end)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            var listBox = (ListBox)sender;

            var scrollViewer = FindScrollViewer(listBox);

            if (scrollViewer != null)
            {
                scrollViewer.ScrollChanged += (o, args) =>
                {
                    if (args.ExtentHeightChange > 0)
                        scrollViewer.ScrollToBottom();
                };
            }
        }

        /// <summary>
        /// search for scroll viewer
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        private static ScrollViewer FindScrollViewer(DependencyObject root)
        {
            var queue = new Queue<DependencyObject>(new[] { root });

            do
            {
                var item = queue.Dequeue();

                if (item is ScrollViewer)
                    return (ScrollViewer)item;

                for (var i = 0; i < VisualTreeHelper.GetChildrenCount(item); i++)
                    queue.Enqueue(VisualTreeHelper.GetChild(item, i));
            } while (queue.Count > 0);

            return null;
        }
    }
}
