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

using IFCTerrainGUI.GUI.ExportSettings; //used for export handling (user controler)

using Newtonsoft.Json; //used for serialize the json file

using Microsoft.Win32;  //file handing (storage location)

using System.ComponentModel; //used for background worker

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
        }


        /// <summary>
        /// create an instance for Json Settings (getter + setter)
        /// mainly used to create interaction between command / GUI and readers & writers
        /// </summary>
        public static JsonSettings jSettings { get; set; } = new JsonSettings();

        /// <summary>
        /// opens documentation
        /// </summary>
        private void tbDocumentation_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //direct Link to GITHUB - Repro so it should be accessable for "all"
            string docuPath = "https://github.com/dd-bim/IfcTerrain/blob/master/README.md";
            //opens link
            System.Diagnostics.Process.Start(docuPath);
        }

        /// <summary>
        /// Sets the location of the IFC file (via JSON settings)
        /// </summary>
        private void btnChooseStorageLocation_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Industry Fundation Classes | *.ifc";
            if (sfd.ShowDialog() == true)
            {
                //gui information
                MainWindowBib.setTextBoxText(tbIfcDir, sfd.FileName);

                //set filepath to jSettings
                jSettings.destFileName = sfd.FileName;

                //TODO LOGGING
            }
            else
            {
                //TODO LOGGING (GUI LOGGING)
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
            //json settings set minDist to 1.0 (default value) [TODO]
            jSettings.minDist = 1.0;

            //serialize json file
            try
            {
                //get filepath
                string dirPath = System.IO.Path.GetDirectoryName(jSettings.destFileName);
                
                //convert to json object
                string jExportText = JsonConvert.SerializeObject(jSettings);

                
                string fileType = jSettings.fileType.ToString();
                string ifcVersion = jSettings.outIFCType.ToString();
                string shape = jSettings.surfaceType.ToString();
                File.WriteAllText(dirPath + @"\config_" + fileType + "_" + ifcVersion + "_" + shape + ".json", jExportText);
                
                //TODO Logging
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex.Message.ToString() + Environment.NewLine);

                //TODO LOGGING
            }
            
            //lock MainWindow 
            this.IsEnabled = false;

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

            //start mapping process which currently begins with the selection of the file reader
            conInt.mapProcess(jSettings);
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

            //TODO logging
        }



        #endregion background worker


    }
}
