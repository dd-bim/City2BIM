using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Win32;

using Autodesk.Revit.UI;
using City2RVT.GUI.Modify;
using BIMGISInteropLibs.OGR;
using Serilog;

namespace City2RVT.GUI.NAS2BIM
{
    /// <summary>
    /// Interaction logic for ImportDialogAlkis.xaml
    /// </summary>
    public partial class ImportDialogAlkis : Window
    {
        private Autodesk.Revit.DB.Document doc { get; set; }
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

        public bool UseFilter { get => useFilter; }
        private bool useFilter { get; set; }

        public OGRSpatialFilter SpatialFilter { get => spatialFilter; }

        private OGRSpatialFilter spatialFilter { get; set; }

        public ImportDialogAlkis(bool terrainAvailable, Autodesk.Revit.DB.Document doc)
        {
            this.doc = doc;
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

                    OGRALKISReader alkisReader = new OGRALKISReader(openFileDialog.FileName);

                    var availableLayers = alkisReader.getLayerList();

                    List<LayerStatus> layerStatusList = new List<LayerStatus>();

                    foreach (var layerName in availableLayers)
                    {
                        layerStatusList.Add(new LayerStatus { LayerName = layerName, Visibility = true });
                    }
                    
                    this.layerStatusList = layerStatusList;
                    
                    LayerTable.ItemsSource = this.layerStatusList;

                    alkisReader.destroy();
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

                if (this.filterCheckBox.IsChecked == true)
                {
                    var pbp = utils.getProjectBasePointMeter(doc);
                    double distance;
                    
                    try
                    {
                        distance = double.Parse(distanceTextBox.Text);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Cannot parse value for spatial filter \n" + ex.ToString());
                        TaskDialog.Show("Error", "Is value for distance/radius a number?");
                        return;
                    }

                    if (filterTypeBox.SelectedValue.ToString() == "Circle")
                    {
                        this.spatialFilter = new OGRCircularSpatialFilter(pbp.X, pbp.Y, distance);
                    }
                    else if (filterTypeBox.SelectedValue.ToString() == "Square")
                    {
                        this.spatialFilter = new OGRRectangularSpatialFilter(pbp.X, pbp.Y, distance, distance);
                    }
                }
                else
                {
                    this.spatialFilter = null;
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
