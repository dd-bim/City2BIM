using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Windows;
using Microsoft.Win32;

using Autodesk.Revit.UI;

using City2BIM.XPlanung;
using City2RVT.GUI.Modify;

namespace City2RVT.GUI.XPlan2BIM
{
    /// <summary>
    /// Interaction logic for ImportXPlanDialog.xaml
    /// </summary>
    public partial class ImportXPlanDialog : Window
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

        public ImportXPlanDialog(bool terrainAvailable)
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
            if (openFileDialog.ShowDialog() == true)
            {
                filePathBox.Text = openFileDialog.FileName;
            }
        }

        private void LoadLayerBtn_click(object sender, RoutedEventArgs e)
        {
            string path = filePathBox.Text;
            if (path == null)
            {
                TaskDialog.Show("Warning!", "Please specify a path to your data!");
                return;
            }

            XDocument xmlDoc = XDocument.Load(path);

            XPlanungReader xPlanReader = new XPlanungReader(xmlDoc);
            xPlanReader.readData();

            var xPlanObjs = xPlanReader.XPlanungObjects;

            var avialableLayers = from xObj in xPlanObjs
                                  group xObj by xObj.UsageType into usageGroup
                                  select usageGroup;

            List<LayerStatus> layerStatusList = new List<LayerStatus>();

            foreach (var usageGroup in avialableLayers)
            {
                layerStatusList.Add(new LayerStatus { LayerName = usageGroup.Key, Visibility = true });
            }

            this.layerStatusList = layerStatusList;
            LayerTable.ItemsSource = this.layerStatusList;
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
                if (drapeCheckBox.IsEnabled)
                {
                    drape = true;
                }
                else
                {
                    drape = false;
                }

                this.filePath = filePathBox.Text;
                this.Close();
            }
            else
            {
                TaskDialog.Show("Error", "No layers to import!");
            }
        }
    }
}
