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

//using IFCTerrainGUI.GUI.MainWindowLogic; //used to outsource auxiliary functions

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

//shortcut to set json settings
using init = GuiHandler.InitClass;

//shortcut to set logging messages
using guiLog = GuiHandler.GuiSupport;

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

            //gui logging
            guiLog.setLog(LogType.info, "Welcome to IFCTerrain");

            //file logging
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "GUI initialized."));
        }

        /// <summary>
        /// opens documentation
        /// </summary>
        private void tbDocumentation_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //direct Link to GITHUB - Repro so it should be accessable for "all"
            string docuPath = "https://github.com/dd-bim/City2BIM/wiki/IFC-Terrain";
            //opens link
            Process.Start(docuPath);
        }

        /// <summary>
        /// Sets the location of the IFC file (via JSON settings)
        /// </summary>
        private void btnChooseStorageLocation_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Industry Fundation Classes| *.ifc|IFCXML File | *.ifcXML|IFCZIP | *.ifcZIP";

            var config = DataContext as Config;

            //open file handler
            if (sfd.ShowDialog() == true)
            {
                //use for selected IFC (STEP) or ifxXML here --> thus case differentiation becomes possible
                switch (sfd.FilterIndex)
                {
                    //jump to this case if STEP was selected
                    case 1:
                        //json settings                        
                        config.outFileType = BIMGISInteropLibs.IFC.IfcFileType.Step;

                        //set settings for DIN SPEC 91391
                        init.config91391.mimeType = "application/x-step";
                        break;
                    //jump to this case if ifcXML was selected
                    case 2:
                        //json setting file format
                        config.outFileType = BIMGISInteropLibs.IFC.IfcFileType.ifcXML;

                        //set settings for DIN SPEC 91391
                        init.config91391.mimeType = "application/xml";
                        break;
                    //jump to this case if ifcXML was selected
                    case 3:
                        //json setting file format
                        config.outFileType = BIMGISInteropLibs.IFC.IfcFileType.ifcZip;

                        //set settings for DIN SPEC 91391
                        init.config91391.mimeType = "application/zip";
                        break;
                }
                
                guiLog.setLog(LogType.info, "Storage location set.");

                //set filepath to jSettings
                config.destFileName = sfd.FileName;

                //set logging path
                string logPath = System.IO.Path.GetDirectoryName(config.destFileName);
                config.logFilePath = logPath;

                //logging
                LogWriter.Entries.Add(new LogPair(LogType.verbose, "[GUI] Storage location set to: " + config.logFilePath));
            }
            return;
        }
        /// <summary>
        /// Start the conversion (based on the settings the user made)
        /// </summary>
        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            //get config
            var config = DataContext as Config;

            //init logger
            LogWriter.initLogger(config);

            //read export specific settings
            //get filepath
            string dirPath = System.IO.Path.GetDirectoryName(config.destFileName);
            string dirName = System.IO.Path.GetFileNameWithoutExtension(config.destFileName);

            string fileType = config.fileType.ToString();
            string ifcVersion = config.outIFCType.ToString();
            string shape = config.outSurfaceType.ToString();

            #region metadata
            //will be executed if user select export of meta data
            if (config.exportMetadataFile.GetValueOrDefault())
            {
                try
                {
                    LogWriter.Entries.Add(new LogPair(LogType.verbose, "[GUI][Metadata] export started."));
                    //init vars for export settings
                    var export913912 = new JProperty("DIN SPEC 91391-2", "NOT EXPORTED");
                    var export187406 = new JProperty("DIN 18740-6", "NOT EXPORTED");

                    //check if metadata should be exported according to DIN 91391-2
                    if (init.config.exportMetadataDin91391.GetValueOrDefault())
                    {
                        LogWriter.Entries.Add(new LogPair(LogType.verbose, "[GUI][Metadata]***DIN SPEC 91391-2***"));
                        //Assignment all obligatory variables
                        //set file name
                        init.config91391.name = System.IO.Path.GetFileName(init.config.destFileName);

                        //set mime type
                        init.config91391.mimeType = "application/x-step";

                        //set export string
                        export913912 = new JProperty("DIN SEPC 91391-2", JObject.FromObject(init.config91391));
                        
                        LogWriter.Entries.Add(new LogPair(LogType.verbose, "[GUI][Metadata] set all meta data to JsonProperty."));

                    }

                    //check if metadata should be exported according to DIN 18740-6
                    if (init.config.exportMetadataDin18740.GetValueOrDefault())
                    {
                        //set export string
                        export187406 = new JProperty("DIN 18740-6", JObject.FromObject(init.config18740));
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
                    Debug.Write(ex.Message.ToString() + Environment.NewLine);
                    LogWriter.Entries.Add(new LogPair(LogType.error, "Metadata - processing: " + ex.Message.ToString()));
                }
            }

            #endregion metadata

            //serialize json file
            try
            {
                LogWriter.Entries.Add(new LogPair(LogType.info, "[GUI][JsonSettings] start serializing json"));

                //convert to json object
                string jExportText = JsonConvert.SerializeObject(config, Formatting.Indented, new JsonSerializerSettings
                {
                    //ignore null values
                    NullValueHandling = NullValueHandling.Ignore
                });

                //export json settings
                File.WriteAllText(dirPath + @"\config_" + fileType + "_" + ifcVersion + "_" + shape + ".json", jExportText);

                LogWriter.Entries.Add(new LogPair(LogType.info, "[GUI][JsonSettings] exported to following path: " + dirPath));
            }
            catch (Exception ex)
            {
                Debug.Write(ex.Message.ToString() + Environment.NewLine);

                LogWriter.Entries.Add(new LogPair(LogType.error, "Json Config - processing: " + ex.Message.ToString()));
            }

            //lock MainWindow 
            this.IsEnabled = false;

            //set mouse cursor to wait
            Mouse.OverrideCursor = Cursors.Wait;

            //kick off background worker ifc
            backgroundWorkerIfc.RunWorkerAsync(config);
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
            //get config 
            var config = e.Argument as Config;

            //Interface between GUI, reader and writer
            ConnectionInterface conInt = new ConnectionInterface();

            //logging
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[BackgroundWorker][IFC] started."));

            //start mapping process which currently begins with the selection of the file reader
            bool processingResult = conInt.mapProcess(config, init.config91391, init.config18740);

            //set result from processing (needed to handle in 'RunWorkerCompleted')
            e.Result = processingResult;
        }

        /// <summary>
        /// Executed after the conversion is done
        /// </summary>
        private void BackgroundWorkerIfc_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //release the MainWindow (conversion is completed)
            this.IsEnabled = true;

            //set mouse cursor to default
            Mouse.OverrideCursor = null;

            //check if processing result is true / false
            if (e.Result.Equals(true))
            {
                guiLog.setLog(LogType.info, "Processing successful.");
            }
            else
            {
                guiLog.setLog(LogType.error, "Processing failed! -> Please check log file!");
            }
        }
        #endregion background worker

        /// <summary>
        /// function to open storage location
        /// </summary>
        private void btnOpenStore_Click(object sender, RoutedEventArgs e)
        {
            //get config from data context
            var config = DataContext as Config;

            //get file path
            string path = System.IO.Path.GetDirectoryName(config.destFileName);

            //check if 'path' exsists
            if (Directory.Exists(path))
            {
                //open explorer
                Process.Start("explorer.exe", path);
            }
            else
            {
                //gui logging (user information)
                guiLog.setLog(LogType.warning, "File path could not be opened!");
            }
        }

        /// <summary>
        /// reset file path (kind of error handling)
        /// </summary>
        private void tabControlImport_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.Source is TabControl)
            {
                //get config from data context
                var config = DataContext as Config;

                config.filePath = null;
                config.fileName = null;
            }
        }

        /// <summary>
        /// remove log entrys when loaded
        /// </summary>
        private void IFCTerrainGUI_Loaded(object sender, RoutedEventArgs e)
        {
            guiLog.clearLog();
        }
    }

    /// <summary>
    /// class to convert integer values of tab index to file type enumeration
    /// </summary>
    public class EnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
                          System.Globalization.CultureInfo culture)
        {
            return (int)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                                  System.Globalization.CultureInfo culture)
        {
            return (IfcTerrainFileType)value;
        }
    }
}
