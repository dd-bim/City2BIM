using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;

using DataCatPlugin.ExternalDataCatalog;
using CommonRevit.Semantics;


namespace DataCatPlugin.GUI
{
    /// <summary>
    /// Interaction logic for DataCatOverview.xaml
    /// </summary>
    public partial class DataCatOverview : Window
    {
        public ObservableCollection<DataGridEntry> dataRows { get; set; }
        public ExternalCommandData commandData { get; set; }

        private Document doc { get; }
        public DataCatOverview(ExternalCommandData commandData, ObservableCollection<DataGridEntry> dataRowsInput)
        {
            InitializeComponent();

            this.dataRows = dataRowsInput;
            this.commandData = commandData;
            this.doc = commandData.Application.ActiveUIDocument.Document;

            EntriesGrid.DataContext = this.dataRows;

            if (dataRowsInput.Count > 0)
            {
                EntriesGrid.SelectedIndex = 0;
            }
        }

        private void EntriesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 1)
            {
                return;
            }

            saveBtn.IsEnabled = true;
            removeClassBtn.IsEnabled = true;
            var entry = e.AddedItems[0] as DataGridEntry;
            var view = commandData.Application.ActiveUIDocument.ActiveView;

            commandData.Application.ActiveUIDocument.Selection.SetElementIds(new ElementId[] { new ElementId(int.Parse(entry.RevitID)) });

            var revitElementId = new ElementId(int.Parse(entry.RevitID));
            var revitElement = doc.GetElement(revitElementId);
            var revitEntity = revitElement.GetEntity(SchemaUtils.getSchemaByName("ExternalDataCatalogSchema"));

            var dict = revitEntity.Get<IDictionary<string, string>>("Objects");

            var myList = dict.Values.ToList().Select(obj => ExternalDataCatalog.ExternalDataSchemaObject.fromJSONString(obj)).ToList();
            var containerList = myList.Select(obj => ExternalDataCatalog.ExternalDataEditorContainer.fromExternalDataSchemaObject(obj)).ToList();

            tabControl.ItemsSource = containerList;
            tabControl.SelectedIndex = 0;

        }

        private void editButtonClick(object sender, RoutedEventArgs e)
        {
            var entry = ((FrameworkElement)sender).DataContext as DataGridEntry;

            var schema = SchemaUtils.getSchemaByName("ExternalDataCatalogSchema");

            Element revitElement = this.doc.GetElement(new ElementId(int.Parse(entry.RevitID)));
            var entity = revitElement.GetEntity(schema);

            var objectDictionary = entity.Get<IDictionary<string, string>>("Objects");

            List<ExternalDataCatalog.ExternalDataSchemaObject> objList = objectDictionary.Select(o => JsonConvert.DeserializeObject<ExternalDataCatalog.ExternalDataSchemaObject>(o.Value)).ToList();

        }

        private void save_Click(object sender, RoutedEventArgs e)
        {
            var dataEditorContainerList = tabControl.Items.OfType<ExternalDataCatalog.ExternalDataEditorContainer>().ToList();
            var selectedEntry = EntriesGrid.SelectedItem as DataGridEntry;

            var revitElement = this.doc.GetElement(new ElementId(int.Parse(selectedEntry.RevitID)));
            var revitEntity = revitElement.GetEntity(SchemaUtils.getSchemaByName("ExternalDataCatalogSchema"));

            if (revitEntity.IsValid())
            {
                var externalDataSchemObjects = dataEditorContainerList.Select(obj => obj.toExternalDataSchemaObject()).ToList();
                Dictionary<string, string> dict = externalDataSchemObjects.ToDictionary(obj => obj.ObjectType, obj => obj.toJSONString());

                using (Transaction trans = new Transaction(doc, "saveChanges"))
                {
                    trans.Start();
                    revitEntity.Set<IDictionary<string, string>>("Objects", dict);
                    revitElement.SetEntity(revitEntity);
                    trans.Commit();
                }
            }
        }

        private void removeClassBtn_Click(object sender, RoutedEventArgs e)
        {
            var messageBoxResult = System.Windows.MessageBox.Show("Do you really want to delete the classification for the selected object", "Delete confirmation", System.Windows.MessageBoxButton.YesNoCancel);

            if (messageBoxResult == MessageBoxResult.Yes)
            {
                IEditableCollectionView items = tabControl.Items;
                if (items.CanRemove)
                {
                    items.Remove(tabControl.SelectedItem);
                }
                if (tabControl.Items.Count < 1)
                {
                    
                }
            }
        }
    }
}
