using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Win32;

using Autodesk.Revit.UI;
using City2RVT.GUI.Modify;
using City2BIM.Alkis;

namespace City2RVT.GUI.NAS2BIM
{
    /// <summary>
    /// Interaction logic for ImportDialogAlkis.xaml
    /// </summary>
    public partial class ImportDialogAlkis : Window
    {
        private List<LayerStatus> layerStatusList { get; set; }
        private bool startImport { set; get; }
        public bool StartImport
        {
            get
            {
                return startImport;
            }
        }

        public List<string> LayerNamesToImport
        {
            get
            {
                return layerNamesToImport;
            }
        }
        private List<string> layerNamesToImport { get; set; }
        private bool drape { get; set; }
        public bool Drape
        {
            get
            {
                return drape;
            }
        }
        private string filePath { get; set; }
        public string FilePath
        {
            get
            {
                return filePath;
            }
        }

        public ImportDialogAlkis(bool terrainAvailable)
        {
            this.layerStatusList = new List<LayerStatus>();
            InitializeComponent();

            if (terrainAvailable)
            {
                drapeCheckBox.IsEnabled = true;
                drapeCheckBox.IsChecked = true;
            }
        }

        private void browseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XML/GML files (*.xml, *.gml)|*.xml; *.gml|All files (*.*)|*.*";


            if (openFileDialog.ShowDialog() == true)
            {
                try
                {

                    filePathBox.Text = openFileDialog.FileName;

                    AlkisReader alkisReader = new AlkisReader(openFileDialog.FileName);

                    var alkisObjs = alkisReader.AlkisObjects;

                    var avialableLayers = from alkisObj in alkisObjs
                                          group alkisObj by alkisObj.UsageType into usageGroup
                                          select usageGroup;

                    List<LayerStatus> layerStatusList = new List<LayerStatus>();

                    foreach (var usageGroup in avialableLayers)
                    {
                        layerStatusList.Add(new LayerStatus { LayerName = usageGroup.Key, Visibility = true });
                    }

                    this.layerStatusList = layerStatusList;
                    LayerTable.ItemsSource = this.layerStatusList;
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Error!", "An error occured. Did you specify a valid Alkis-File?" + "\n\n" + ex.ToString());
                }
            }
        }

        private void selectAllBtn_click(object sender, RoutedEventArgs e)
        {
            foreach (var layerStatus in this.layerStatusList)
            {
                layerStatus.Visibility = true;
            }
        }

        private void clearSelectionBtn_click(object sender, RoutedEventArgs e)
        {
            foreach (var layerStatus in this.layerStatusList)
            {
                layerStatus.Visibility = false;
            }
        }

        private void importButton_click(object sender, RoutedEventArgs e)
        {
            var layersToImport = from layer in this.layerStatusList
                                 where layer.Visibility == true
                                 select layer.LayerName;

            this.layerNamesToImport = layersToImport.ToList();

            if (this.layerNamesToImport.Count > 0)
            {
                this.startImport = true;
                if (drapeCheckBox.IsEnabled & (bool)drapeCheckBox.IsChecked)
                {
                    this.drape = true;
                }
                else
                {
                    this.drape = false;
                }

                this.filePath = filePathBox.Text;
                this.Close();
            }
            else
            {
                TaskDialog.Show("Error", "No layers to import!");
            }
        }

        private void cancelButton_click(object sender, RoutedEventArgs e)
        {
            this.startImport = false;
            this.Close();
        }

    }
}
