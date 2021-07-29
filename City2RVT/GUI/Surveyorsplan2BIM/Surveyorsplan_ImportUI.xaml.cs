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

using System.Collections.ObjectModel; //observable collection
using System.Windows.Data;

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
        /// mapping table
        /// </summary>
        private utils.mapping mapping { get; set; }

        /// <summary>
        /// 
        /// </summary>
        private string dxfLayer { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
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
            
            //init mapping
            mapping = utils.mapping.init();

            //init gui components
            InitializeComponent();

            //init background Worker
            utils.initBackgroundWorkerDxf();
        }

        /// <summary>
        /// load dxf file & list all layer
        /// </summary>
        private void btnLoadDxf_Click(object sender, RoutedEventArgs e)
        {
            //open dialog
            utils.readDxfFileDialog();

            //clear layout
            lbDxfLayer.Items.Clear();
           
            //check if file can be opend
            if(utils.openDxfFile(utils.dxfFileName, out dxfFile))
            {
                //go through all layer of selected dxf fiel
                foreach (var layer in dxfFile.Layers)
                {
                    //list up all layer
                    lbDxfLayer.Items.Add(layer.Name);
                }

                //set file name
                mapping.dxfFileName = utils.dxfFileName;
            }
            else
            {
                dxfFile = null;
                mapping.dxfFileName = null;
            }
            
            return;
        }
      

        /// <summary>
        /// dxf file to be processed
        /// </summary>
        private DxfFile dxfFile = null;
        

        //change selection
        private void btnApplyDxfLayer_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = lbDxfLayer.SelectedItem.ToString();

            //set current layer
            dxfLayer = selectedItem;


            DxfInsert dxfEntity;

            cbBlockAttributes.Items.Clear();

            //set number of attributes to 0
            int count = 0;

            //get current layer
            foreach (var entity in dxfFile.Entities)
            {
                if (entity.Layer == selectedItem)
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
                            cbBlockAttributes.Items.Add(attr.AttributeTag.ToString());
                        }
                    }                    
                }
            }

            //update layout
            cbBlockAttributes.UpdateLayout();
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

            //
            bool familyReading;

            //if string is not empty
            if(ofd.SelectedPath != string.Empty)
            {
                //set file path
                rvtFamilyFolder = ofd.SelectedPath;

                familyReading = readRvtFam(rvtFamilyFolder);
            }
            else
            {
                //set file to false
                rvtFamilyFolder = null;

                //set to false
                familyReading = false;
            }

            //error handling
            if (familyReading)
            {
                mapping.familyDir = rvtFamilyFolder;
            }
            else
            {
                mapping.familyDir = null;
            }

            //clear attributes list (new family folder is selected)
            cbFamilyAttributes.Items.Clear();

            return;
        }

        /// <summary>
        /// read revit familys in selected file folder
        /// </summary>
        private bool readRvtFam(string rvtFamilyFilePath)
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

                    return false;
                }
                
            }
            catch(Exception ex)
            {
                //show error message
                MessageBox.Show(ex.Message, "Revit family reading", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            //update layout - file names will be shown after that
            lbFamilies.UpdateLayout();

            return true;
        }

        /// <summary>
        /// apply revit family selection
        /// </summary>
        private void btnApplyFamilySelection_Click(object sender, RoutedEventArgs e)
        {
            //clear attributes
            cbFamilyAttributes.Items.Clear();

            //get selected family
            string selectedFamily = lbFamilies.SelectedItem.ToString();

            //
            rfaFile = Path.GetFileNameWithoutExtension(selectedFamily);

            //set absoult file path to family
            string absPath = Path.Combine(rvtFamilyFolder, selectedFamily);

            //read out parameter
            List<utils.familyParameterData> famParameterDataSet = utils.readFamilyParameterInfo(rvtDoc.Application, absPath);

            //loop through all parameter
            foreach (var para in famParameterDataSet)
            {
                //if built in parameter is invalid list up (equals user defined parameter)
                if (para.BuiltinParameter.Equals("INVALID"))
                {
                    //list into list box for user selection
                    cbFamilyAttributes.Items.Add(para.ParameterName);
                }
            }
            //update lb will be listed up after that
            cbFamilyAttributes.UpdateLayout();
        }

        

       

        

        /// <summary>
        /// btn to create new mapping list
        /// </summary>
        private void btnCreateMapping_Click(object sender, RoutedEventArgs e)
        {
            //init mapping list
            utils.mappingEntry mapEntry = new utils.mappingEntry();

            //init enum            
            utils.survObjType survObjType;

            //parse enum
            Enum.TryParse(cbObjTypeEnum.SelectedValue.ToString(), out survObjType);

            //set type
            mapEntry.survObjType = survObjType;

            //set current layer selection
            mapEntry.dxfName = dxfLayer;

            //set current family name
            mapEntry.familyName = rfaFile;

            //create new guid
            //mapList.mappingId = Guid.NewGuid();

            //init empty parametermap 
            mapEntry.parameterMap = new Dictionary<string, string>();

            //add to mapping lists collection 
            mapping.mappingEntrys.Add(mapEntry);

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
            var currentMapEntry = mapping.mappingEntrys.Last();

            string mapTitel = currentMapEntry.dxfName + "-" + currentMapEntry.familyName;

            //[REWORK]lbMappingConfigs.Items.Add(mapTitel);

            //remove attributes
            lbDxfLayer.Items.Remove(currentMapEntry.dxfName);
            lbFamilies.Items.Remove(currentMapEntry.familyName + ".rfa");

            //update layout
            lbDxfLayer.UpdateLayout();

            //[REWORK]lbFamilies.UpdateLayout();

            //clear reaming attributes
            cbFamilyAttributes.Items.Clear();
            cbBlockAttributes.Items.Clear();
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
                //serialize mappingList to file
                string jsonText = JsonConvert.SerializeObject(mapping, Formatting.Indented);

                //write json to "wanted" file path
                File.WriteAllText(sfd.FileName, jsonText);

                //clear items & update
                //[REWORK]lbMappingConfigs.Items.Clear();
                //[REWORK]lbMappingConfigs.UpdateLayout();
            }
        }

        /// <summary>
        /// config loading
        /// </summary>
        private void btnLoadConfig_Click(object sender, RoutedEventArgs e)
        {
            //set file dialog
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Json config| *.json";

            //open dialog
            if(ofd.ShowDialog() == true)
            {
                //file stream to read json file
                using (StreamReader file = File.OpenText(ofd.FileName))
                {
                    //init serializer
                    JsonSerializer serializer = new JsonSerializer();
                    
                    //try to deserialize json file to mapping List
                    try
                    {
                        //get mapping from json import
                        utils.mapping map = (utils.mapping)serializer.Deserialize(file, typeof(utils.mapping));

                        //überschreiben des mapping durch import json
                        mapping = map;

                    }
                    //return exeption (TODO: logging)
                    catch(Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Imported JSON - deserializing", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    //if deserialize was successful ... list all entrys
                    //[REWORK]lbMappingConfigs.Items.Clear();

                    //loop through every entry and list up
                    foreach (utils.mappingEntry entry in mapping.mappingEntrys)
                    {
                        //[REWORK]lbMappingConfigs.Items.Add(entry.dxfName + " - " + entry.familyName);
                    }

                    //update entry list
                    //[REWORK]lbMappingConfigs.UpdateLayout();
                }
            }

            //error handling
            //open dxf file path and check that file is still there
            
            //clear listbox
            lbDxfLayer.Items.Clear();

            if (File.Exists(mapping.dxfFileName))
            {
                //read dxf file from output
                utils.openDxfFile(mapping.dxfFileName, out dxfFile);

                foreach(var layer in dxfFile.Layers)
                {
                    lbDxfLayer.Items.Add(layer.Name);
                }
            }
            else
            {
                //
                lbDxfLayer.Items.Add("DXF file missing!");

                //return error message
                MessageBox.Show("The DXF file under the path: " + 
                    Environment.NewLine + mapping.dxfFileName + " can not be readed.", "DXF file reading", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            //open family folder and check each used family if still there
            lbFamilies.Items.Clear();

            if (Directory.Exists(mapping.familyDir))
            {
                readRvtFam(mapping.familyDir);
            }
            else
            {
                lbFamilies.Items.Add("Family folder invalid!");

                MessageBox.Show("The family file folder under the path: " +
                    Environment.NewLine + mapping.familyDir + " can not be readed.", "Family folder",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void btnStartImport_Click(object sender, RoutedEventArgs e)
        {



        }

        /// <summary>
        /// 
        /// </summary>
        private void lvMappingSort(object sender, RoutedEventArgs e)
        {
            //get column which should be sorted
            GridViewColumnHeader column = sender as GridViewColumnHeader;

            //
            ICollectionView view = CollectionViewSource.GetDefaultView(lvMapping.ItemsSource);

            //
            view.SortDescriptions.Clear();

            //
            view.SortDescriptions.Add(
                new SortDescription(column.Content.ToString(), ListSortDirection.Ascending));

            //
            view.Refresh();
        }

        /// <summary>
        /// button to fill mapping list
        /// </summary>
        private void btnSetSelection_Click(object sender, RoutedEventArgs e)
        {
            //get last mapping list
            var mapEntry = mapping.mappingEntrys.Last();

            //get selected dxf layer
            var dxfLayer = cbBlockAttributes.SelectedItem.ToString();

            //get selected family attribute layer
            var familyName = cbFamilyAttributes.SelectedItem.ToString();

            //add to parameter map
            mapEntry.parameterMap.Add(dxfLayer, familyName);

            //remove from "list selection"
            cbBlockAttributes.Items.Remove(dxfLayer);
            cbFamilyAttributes.Items.Remove(familyName);

            //update layout (otherwise will not be shown)
            cbBlockAttributes.UpdateLayout();
            cbFamilyAttributes.UpdateLayout();

            if (!btnSetLayerMapping.IsEnabled)
            {
                btnSetLayerMapping.IsEnabled = true;
            }

            //-----------------
            mapOverview.Add(new mappingOverview()
            { Name = rfaFile, ObjType = mapEntry.survObjType, DxfLayer = dxfLayer, DxfUnit = "TESTUNIT", RfaLayer = familyName });

            DataContext = mapOverview;

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lvMapping.DataContext);
            PropertyGroupDescription groupDescription = new PropertyGroupDescription("Name");

            if (view.GroupDescriptions.Count == 0)
            {
                view.GroupDescriptions.Add(groupDescription);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ObservableCollection<mappingOverview> mapOverview { get; set; } = new ObservableCollection<mappingOverview>();

        /// <summary>
        /// 
        /// </summary>
        public ObservableCollection<utils.mapping> map { get; set; } = new ObservableCollection<utils.mapping>();

        /// <summary>
        /// 
        /// </summary>
        public class mappingOverview
        {
            public string Name { get; set; }

            public utils.survObjType ObjType { get; set; }

            public string DxfLayer { get; set; }

            public string DxfUnit { get; set; }

            public string RfaLayer { get; set; }
        }

        private void btnRemoveMappingEntry_Click(object sender, RoutedEventArgs e)
        {
            //init selected item
            dynamic item = null;

            try
            {
                //get selected value (index) from grid view
                item = mapOverview.ElementAt(lvMapping.SelectedIndex);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                lbLogging.Items.Add("Selected item cannot be deleted.");
                return;
            }

            //
            var remove = mapOverview.ElementAt(lvMapping.SelectedIndex).DxfLayer;

            //get map entry which shoul be removed
            foreach (var entry in mapping.mappingEntrys)
            {
                //
                if (entry.parameterMap.ContainsKey(remove))
                {
                    entry.parameterMap.Remove(remove);
                }
            }
            //remove item
            mapOverview.Remove(item);

            //restore in parameter pull down menus


        }
    }
}

