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
        /// 
        /// </summary>
        private Document rvtDoc { get; set; }
        #endregion config

        /// <summary>
        /// init import gui
        /// </summary>
        /// <param name="doc">current revit document</param>
        public Surveyorsplan_ImportUI(Document doc)
        {
            //
            rvtDoc = doc;

            //
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
        /// 
        /// </summary>
        private void lbDxfLayer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = lbDxfLayer.SelectedItems;

            //check if at least one item (layer) is selected
            if (selectedItem.Count != 0)
            {
                //enable btn for apply
                btnApplyDxfLayer.IsEnabled = true;
            }
            else
            {
                //disable btn
                btnApplyDxfLayer.IsEnabled = false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void btnSetFamilyFolder_Click(object sender, RoutedEventArgs e)
        {
            //add new FileDialog handler
            var ofd = new System.Windows.Forms.FolderBrowserDialog();

            //open dialog
            ofd.ShowDialog();

            //if string is not empty
            if(ofd.ToString() != string.Empty)
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
            //get selected family
            string selectedFamily = lbFamilies.SelectedItem.ToString();

            //kick off listing method
            Family family = readFamily(rvtFamilyFolder, selectedFamily);


            //
            ElementClassFilter FamilyInstanceFilter = new ElementClassFilter(typeof(FamilyInstance));

            //
            FilteredElementCollector FamilyInstanceCollector = new FilteredElementCollector(rvtDoc);

            //
            ICollection<Element> AllFamilyInstances = FamilyInstanceCollector.WherePasses(FamilyInstanceFilter).ToElements();

            //
            List<string> paramList = new List<string>();

            //
            FamilySymbol familySymbol;

            //
            Family fam;

            //
            foreach(FamilyInstance famIns in AllFamilyInstances)
            {
                familySymbol = famIns.Symbol;
                fam = familySymbol.Family;

                //
                foreach(Parameter para in famIns.Parameters)
                {
                    paramList.Add(para.Definition.Name);
                }

            }


            var parameter = family.GetParameters("Pfeiler_Material");

            if(family != null)
            {
                MessageBox.Show(parameter.ToString());
            }
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
                //enable btn for apply
                btnApplyFamilySelection.IsEnabled = true;
            }
            else
            {
                //disable btn
                btnApplyFamilySelection.IsEnabled = false;
            }
        }

        /// <summary>
        /// import family to current document
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="famliyFile"></param>
        /// <returns></returns>
        private Family readFamily(string filePath, string famliyFile)
        {
            //init family
            Family family = null;

            //get absoult path
            var absPath = Path.Combine(filePath, famliyFile);

            //
            FamilySymbol familySymbol = GetSymbol(rvtDoc, Path.GetFileNameWithoutExtension(famliyFile));

            if (familySymbol == null)
            {
                //read family as transaction (otherwise: famliy = null)
                using (Transaction t = new Transaction(rvtDoc))
                {
                    t.Start("Load family");
                    try
                    {
                        rvtDoc.LoadFamily(absPath, out family);
                    }
                    catch (Exception ex)
                    {
                        t.RollBack();
                        //return error message
                        MessageBox.Show(ex.Message, "Family loading", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return null;
                    }
                    t.Commit();
                }
            }
            else
            {
                family = familySymbol.Family;
            }
            

           
            return family;
        }


        public FamilySymbol GetSymbol(Document document, string familyName)
        {
            return new FilteredElementCollector(document).OfClass(typeof(Family)).OfType<Family>().FirstOrDefault(f => f.Name.Equals(familyName))?.GetFamilySymbolIds().Select(id => document.GetElement(id)).OfType<FamilySymbol>().FirstOrDefault();
        }

    }
}

