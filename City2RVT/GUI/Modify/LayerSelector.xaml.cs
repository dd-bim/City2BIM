using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

using Autodesk.Revit.UI;

namespace City2RVT.GUI.Modify
{
    /// <summary>
    /// Interaction logic for LayerSelector.xaml
    /// </summary>
    public partial class LayerSelector : Window
    {

        public List<string> visibleLayers = new List<string>();
        public List<string> unvisibleLayers = new List<string>();
        private List<LayerStatus> layerStatusList { get; set; }

        public LayerSelector(List<LayerStatus> layerList)
        {
            InitializeComponent();
            this.layerStatusList = layerList;
            LayerTable.ItemsSource = this.layerStatusList;
        }

        private void apply_click(object sender, RoutedEventArgs e)
        {
            var layerStatusList = (List<LayerStatus>)LayerTable.ItemsSource;

            foreach (var entry in layerStatusList)
            {
                if (entry.Visibility && visibleLayers.Contains(entry.LayerName) == false)
                {
                    visibleLayers.Add(entry.LayerName);
                }
                else if (entry.Visibility == false && unvisibleLayers.Contains(entry.LayerName) == false)
                {
                    unvisibleLayers.Add(entry.LayerName);
                }
            }

            this.Close();
        }

        private void cancel_click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void selectAll_click(object sender, RoutedEventArgs e)
        {
            foreach (var layerStatus in this.layerStatusList)
            {
                layerStatus.Visibility = true;
            }
        }

        private void unSelectAll_click(object sender, RoutedEventArgs e)
        {
            foreach (var layerStatus in this.layerStatusList)
            {
                layerStatus.Visibility = false;
            }
        }
    }
}
