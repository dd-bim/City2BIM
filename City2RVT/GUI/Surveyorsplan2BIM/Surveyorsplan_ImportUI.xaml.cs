using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using System.ComponentModel;    //BackgroundWorker
using System.IO;                //file stream
using System.Collections.Generic;

//revit api
using Autodesk.Revit.DB;

//dxf handling
using IxMilia.Dxf;              
using IxMilia.Dxf.Entities;

using Microsoft.Win32;          //file dialog handling

using Newtonsoft.Json;

using cmd = City2RVT.GUI.Cmd_Surveyorsplan;

namespace City2RVT.GUI.Surveyorsplan2BIM
{
    /// <summary>
    /// Interaktionslogik für Surveyorsplan_ImportUI.xaml
    /// </summary>
    public partial class Surveyorsplan_ImportUI : Window
    {
        #region config
        /// <summary>
        /// return to start surv import in cmd 
        /// </summary>
        public bool startSurvImport { get { return startImport; } }
        
        /// <summary>
        /// value to be specified that the import should be started
        /// </summary>
        private bool startImport { get; set; } = false;

        /// <summary>
        /// file path for revit families
        /// </summary>
        private string rvtFamilyFolder { get; set; }

        /// <summary>
        /// revit document
        /// </summary>
        private Document rvtDoc { get; set; }

        /// <summary>
        /// 
        /// </summary>
        private List<utils.mappingList> mappingLists { get; set; }

        private string dxfLayer { get; set; }
        private string rfaFile { get; set; }
        #endregion config

        /// <summary>
        /// init import gui
        /// </summary>
        /// <param name="doc">current revit document</param>
        public Surveyorsplan_ImportUI(Document doc)
        {
            //handle revit doc
            this.rvtDoc = doc;

            mappingLists = new List<utils.mappingList>();

            //init gui components
            InitializeComponent();

            //init "do" task
            backgroundWorker.DoWork += backgroundWorker_DoWork;

            //task when "do" is completed
            backgroundWorker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted;
        }

        /// <summary>
        /// load dxf file & list all layer
        /// </summary>
        private void btnLoadDxf_Click(object sender, RoutedEventArgs e)
        {
            //add new FileDialog handler
            var ofd = new OpenFileDialog();

            //set file filter
            ofd.Filter = "DXF Files *.dxf, *.dxb|*.dxf;*.dxb";

            //open dialog window
            if(ofd.ShowDialog() == true)
            {
                //disable window
                IsEnabled = false;

                //change mouse corsor (user feedback)
                Mouse.OverrideCursor = Cursors.Wait;

                //kick off backgorund worker (with current file name)
                backgroundWorker.RunWorkerAsync(ofd.FileName);
            }

            return;
        }

        /// <summary>
        /// background worker to list dxf elements for user selection
        /// </summary>
        private readonly BackgroundWorker backgroundWorker = new BackgroundWorker();

        /// <summary>
        /// dxf file to be processed
        /// </summary>
        private DxfFile dxfFile = null;

        /// <summary>
        /// read dxf file
        /// </summary>
        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = ReadDxfFile((string)e.Argument, out this.dxfFile) ? (string)e.Argument : "";
        }
        
        /// <summary>
        /// reader for file name
        /// </summary>
        /// <returns>dxf file (ixMilia conform)</returns>
        private static bool ReadDxfFile(string fileName, out DxfFile dxfFile)
        {
            //init dxf file
            dxfFile = null;

            //try to open fileName
            try 
            {
                using (var fileStream = new FileStream(fileName, FileMode.Open))
                {
                    //will be returned via "out"
                    dxfFile = DxfFile.Load(fileStream);
                    
                    //return true - file could be opend
                    return true;
                }
            }
            catch(Exception ex)
            {
                //return error message
                MessageBox.Show("DXF file could not be read:" + Environment.NewLine + ex.Message, "DXF file reader", MessageBoxButton.OK, MessageBoxImage.Error);
                
                //return false - file could not be opend
                return false;
            }
        }

        /// <summary>
        /// executed as soon "do"-Worker is done
        /// </summary>
        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //file name
            string fileName = (string)e.Result;

            //clear all items (cause another file will be read OR read again)
            lbDxfLayer.Items.Clear();

            //check if file could not be read
            if (string.IsNullOrEmpty(fileName))
            {
                //set dxf file to null
                dxfFile = null;
            }
            else
            {
                //go through all layer of selected dxf fiel
                foreach(var layer in dxfFile.Layers)
                {
                    //list up all layer
                    lbDxfLayer.Items.Add(layer.Name);
                }
            }

            //update gui
            lbDxfLayer.UpdateLayout();

            //enable mainwindow
            IsEnabled = true;

            //change mouse cursor to default
            Mouse.OverrideCursor = null;
        }

        //change selection
        private void btnApplyDxfLayer_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = lbDxfLayer.SelectedItem.ToString();

            //set current layer
            dxfLayer = selectedItem;

            DxfInsert dxfEntity;

            lbBlockAttributes.Items.Clear();

            //set number of attributes to 0
            int count = 0;

            //get current layer
            foreach (var entity in dxfFile.Entities)
            {
                if (entity.Layer == selectedItem)
                {
                    //
                    try
                    {
                        //get entity
                        dxfEntity = (IxMilia.Dxf.Entities.DxfInsert)entity;
                        
                        if(count != dxfEntity.Attributes.Count)
                        {
                            //update count
                            count = dxfEntity.Attributes.Count;

                            //loop through all attributes of one entity
                            foreach (var attr in dxfEntity.Attributes)
                            {
                                lbBlockAttributes.Items.Add(attr.AttributeTag.ToString());
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show(ex.Message, "List elemnts", MessageBoxButton.OK, MessageBoxImage.Question);
                    }
                }
            }

            //update layout
            lbBlockAttributes.UpdateLayout();
        }

        /// <summary>
        /// read folder
        /// </summary>
        private void btnSetFamilyFolder_Click(object sender, RoutedEventArgs e)
        {
            //add new FileDialog handler
            var ofd = new System.Windows.Forms.FolderBrowserDialog();

            //open dialog
            ofd.ShowDialog();

            //if string is not empty
            if(ofd.SelectedPath != string.Empty)
            {
                //set file path
                rvtFamilyFolder = ofd.SelectedPath;

                readRvtFam(rvtFamilyFolder);
            }
            else
            {
                //set file to false
                rvtFamilyFolder = null;
            }

            //clear attributes list (new family folder is selected)
            lbFamilyAttributes.Items.Clear();

            return;
        }

        /// <summary>
        /// read revit familys in selected file folder
        /// </summary>
        private void readRvtFam(string rvtFamilyFilePath)
        {
            //clear items
            lbFamilies.Items.Clear();

            try
            {
                //get method to get directory
                var directoryInfo = new DirectoryInfo(@rvtFamilyFilePath);

                //match all rvt files
                FileInfo[] rvtFiles = directoryInfo.GetFiles("*.rfa", SearchOption.AllDirectories);

                if(rvtFiles.Length != 0)
                {
                    //for each family in folder
                    foreach (FileInfo family in rvtFiles)
                    {
                        //add to menu
                        lbFamilies.Items.Add(family.Name);
                    }
                }
                else
                {
                    //user feedback
                    MessageBox.Show("The selected folder does not contain Revit families.", "Folder is empty!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                
            }
            catch(Exception ex)
            {
                //show error message
                MessageBox.Show(ex.Message, "Revit family reading", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            //update layout - file names will be shown after that
            lbFamilies.UpdateLayout();
        }

        /// <summary>
        /// apply revit family selection
        /// </summary>
        private void btnApplyFamilySelection_Click(object sender, RoutedEventArgs e)
        {
            //clear attributes
            lbFamilyAttributes.Items.Clear();

            //get selected family
            string selectedFamily = lbFamilies.SelectedItem.ToString();

            //
            rfaFile = Path.GetFileNameWithoutExtension(selectedFamily);

            //set absoult file path to family
            string absPath = Path.Combine(rvtFamilyFolder, selectedFamily);

            //read out parameter
            List<utils.FParameterData> famParameterDataSet = utils.readFamilyParameterInfo(rvtDoc.Application, absPath);

            //loop through all parameter
            foreach (var para in famParameterDataSet)
            {
                //if built in parameter is invalid list up (user defined parameter)
                if (para.BuiltinParameter.Equals("INVALID"))
                {
                    //list into list box for user selection
                    lbFamilyAttributes.Items.Add(para.ParameterName);
                }
            }
            //update lb will be listed up after that
            lbFamilyAttributes.UpdateLayout();

            return;

            /*
             * 
             * FamilySymbol familySymbol = new FilteredElementCollector(rvtDoc).
                OfClass(typeof(Family)).OfType<Family>().
                FirstOrDefault(f => f.Name.Equals(family.Name))?.
                GetFamilySymbolIds().Select(id => rvtDoc.GetElement(id)).OfType<FamilySymbol>().
                FirstOrDefault(sym => sym.Name.Equals(family.Name));


            FamilyInstance familyInstance = null;

            using (Transaction transaction = new Transaction(rvtDoc))
            {
                transaction.Start("Dummy");
                familyInstance = AdaptiveComponentInstanceUtils.CreateAdaptiveComponentInstance(rvtDoc, familySymbol);
                transaction.Commit();
            }

            

            foreach(Parameter para in familyInstance.Parameters)
            {   
                if(para.Definition.ParameterGroup != BuiltInParameterGroup.INVALID)
                {
                    lbFamilyAttributes.Items.Add(para.Definition.Name);
                }
            }
            lbFamilyAttributes.UpdateLayout();
            */
        }

        private void btnStartImport_Click(object sender, RoutedEventArgs e)
        {
            

            
        }

        /// <summary>
        /// button to fill mapping list
        /// </summary>
        private void btnSetSelection_Click(object sender, RoutedEventArgs e)
         {
            //get last mapping list
            utils.mappingList mapList = mappingLists.Last();

            //get selected dxf layer
            var dxfLayer = lbBlockAttributes.SelectedItem.ToString();

            //get selected family attribute layer
            var familyName = lbFamilyAttributes.SelectedItem.ToString();

            //add to parameter map
            mapList.parameterMap.Add(dxfLayer, familyName);

            //remove from "list selection"
            lbBlockAttributes.Items.Remove(dxfLayer);
            lbFamilyAttributes.Items.Remove(familyName);

            //update layout (otherwise will not be shown)
            lbBlockAttributes.UpdateLayout();
            lbFamilyAttributes.UpdateLayout();

            if (!btnSetLayerMapping.IsEnabled)
            {
                btnSetLayerMapping.IsEnabled = true;
            }
        }

        /// <summary>
        /// btn to create new mapping list
        /// </summary>
        private void btnCreateMapping_Click(object sender, RoutedEventArgs e)
        {
            //init mapping list
            utils.mappingList mapList = new utils.mappingList();

            //set current layer selection
            mapList.dxfName = dxfLayer;

            //set current family name
            mapList.familyName = rfaFile;

            //create new guid
            mapList.mappingId = Guid.NewGuid();

            //init empty parametermap 
            mapList.parameterMap = new Dictionary<string, string>();

            //add to mapping lists collection
            mappingLists.Add(mapList);

            //enable set selection
            btnSetSelection.IsEnabled = true;

            //
            btnCreateMapping.IsEnabled = false;

            //
            dxfLayerSelected = false;
            rfaSelected = false;
        }

        /// <summary>
        /// 
        /// </summary>
        private void lbDxfLayer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = lbDxfLayer.SelectedItems;

            //check if at least one item (layer) is selected
            if (selectedItem.Count != 0)
            {
                btnApplyDxfLayer.IsEnabled = true;
                dxfLayerSelected = true;
            }
            else
            {
                btnApplyDxfLayer.IsEnabled = false;
                dxfLayerSelected = false;
            }

            checkSelection();
        }


        /// <summary>
        /// 
        /// </summary>
        private void lbFamilies_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = lbFamilies.SelectedItems;

            //check if at least one item (layer) is selected
            if (selectedItem.Count != 0)
            {
                btnApplyFamilySelection.IsEnabled = true;
                rfaSelected = true;
            }
            else
            {
                btnApplyFamilySelection.IsEnabled = false;
                rfaSelected = false;
            }

            checkSelection();
        }

        private bool checkSelection()
        {
            if(dxfLayerSelected && rfaSelected)
            {
                btnCreateMapping.IsEnabled = true;
                return true;
            }
            return false;
        }

        private bool dxfLayerSelected { get; set; } = false;
        private bool rfaSelected { get; set; } = false;


        private void btnSetLayerMapping_Click(object sender, RoutedEventArgs e)
        {
            var currentMapEntry = mappingLists.Last();

            string mapTitel = currentMapEntry.dxfName + "-" + currentMapEntry.familyName;

            lbMappingConfigs.Items.Add(mapTitel);

            //remove attributes
            lbDxfLayer.Items.Remove(currentMapEntry.dxfName);
            lbFamilies.Items.Remove(currentMapEntry.familyName + ".rfa");

            //update layout
            lbDxfLayer.UpdateLayout();
            lbFamilies.UpdateLayout();

            //clear reaming attributes
            lbFamilyAttributes.Items.Clear();
            lbBlockAttributes.Items.Clear();
        }


        /// <summary>
        /// store current config to json file
        /// </summary>
        private void btnSaveConfig_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Json config| *.json";
            //open file handler
            if(sfd.ShowDialog() == true)
            {
                using (StreamWriter file = File.CreateText(sfd.FileName))
                {
                    //init serializer
                    JsonSerializer serializer = new JsonSerializer();

                    //set formatting
                    serializer.Formatting = Formatting.Indented;

                    //serialize mappingList to file
                    serializer.Serialize(file, mappingLists);
                }

                lbMappingConfigs.Items.Clear();
                lbMappingConfigs.UpdateLayout();
            }
        }

        private void btnLoadConfig_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Json config| *.json";

            if(ofd.ShowDialog() == true)
            {
                using (StreamReader file = File.OpenText(ofd.FileName))
                {
                    JsonSerializer serializer = new JsonSerializer();

                    List<utils.mappingList> mapEntry = (List<utils.mappingList>)serializer.Deserialize(file, typeof(List<utils.mappingList>));

                    mappingLists = mapEntry;

                    foreach(utils.mappingList entry in mappingLists)
                    {
                        lbMappingConfigs.Items.Add(entry.dxfName + " - " + entry.familyName);
                    }
                }
            }
        }
    }
}

