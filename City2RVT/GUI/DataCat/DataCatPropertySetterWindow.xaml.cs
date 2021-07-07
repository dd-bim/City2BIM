using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;

using City2RVT.ExternalDataCatalog;


namespace City2RVT.GUI.DataCat
{
    /// <summary>
    /// Interaction logic for DataCatPropertySetterWindow.xaml
    /// </summary>
    public partial class DataCatPropertySetterWindow : Window
    {
        public List<ExternalDataEditorContainer> containerList { get; set; }
        public bool saveChanges { get; set; }

        public DataCatPropertySetterWindow(List<ExternalDataSchemaObject> objectList)
        {
            this.containerList = objectList.Select(obj => ExternalDataEditorContainer.fromExternalDataSchemaObject(obj)).ToList();

            InitializeComponent();

            tabs.ItemsSource = this.containerList;
        }

        private void save_Click(object sender, RoutedEventArgs e)
        {
            saveChanges = true;
            this.Close();
        }

        private void cancel_Click(object sender, RoutedEventArgs e)
        {
            saveChanges = false;
            this.Close();
        }
    }
}
