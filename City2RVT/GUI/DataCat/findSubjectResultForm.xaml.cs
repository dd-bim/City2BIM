using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Newtonsoft.Json;

using City2RVT.ExternalDataCatalog;

namespace City2RVT.GUI.DataCat
{
    /// <summary>
    /// Interaction logic for findSubjectResultForm.xaml
    /// </summary>
    public partial class findSubjectResultForm : Window
    {
        private UIDocument uiDoc { get; set; }
        public findSubjectResultForm(UIDocument uiDoc)
        {
            this.uiDoc = uiDoc;
            InitializeComponent();
            
        }

        private void queryBtn_click(object sender, RoutedEventArgs e)
        {
            var searchText = SearchBox.Text;
            
            bool tokenStatus = ExternalDataUtils.testTokenValidity();

            if (tokenStatus == false)
            {
                TaskDialog.Show("Error!", "You are currently not logged into the external server!");
            }

            else if (searchText != null && searchText != "")
            {
                var responseWithHierarchy = Prop_Revit.DataClient.querySubjectsWithHierarchy(searchText);
                trvFindResult.ItemsSource = responseWithHierarchy.data.findSubjects.nodes;
            }
        }

        private void findSubjectWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double actualHeight = findSubjectWindow.ActualHeight;
            treeViewGrid.Height = actualHeight * .7;
        }

        private void setObjectBtn_Click(object sender, RoutedEventArgs e)
        {
            Selection sel = this.uiDoc.Selection;
            ICollection<ElementId> selectedIds = sel.GetElementIds();

            //more or less than one element is selected
            if (selectedIds.Count != 1)
            {
                TaskDialog.Show("Hint!", "Please only select ONE element!");
                return;
            }

            var selectedItem = trvFindResult.SelectedItem;

            if (selectedItem == null)
            {
                TaskDialog.Show("Info", "Please select an object type from the tree view");
            }


            else if (selectedItem.GetType() == typeof(City2RVT.ExternalDataCatalog.Property))
            {
                TaskDialog.Show("Warning", "Please do not select a property but an object type");
            }

            else if (selectedItem.GetType() == typeof(City2RVT.ExternalDataCatalog.Node))
            {
                var node = selectedItem as City2RVT.ExternalDataCatalog.Node;

                var attrDict = new Dictionary<string, string>();

                foreach (var prop in node.properties)
                {
                    attrDict.Add(prop.name, "");
                }

                var schemaObj = new ExternalDataSchemaObject(node.name, attrDict, node.ifcClassification);

                var editor = new DataCatalogEditorWindow(schemaObj.prepareForEditorWindow());
                editor.ShowDialog();

                if (editor.saveChanges)
                {
                    using (Transaction trans = new Transaction(this.uiDoc.Document, "add external data to element"))
                    {
                        trans.Start();

                        Schema externalSchema = ExternalDataUtils.createExternalDataCatalogSchema(uiDoc.Document);
                        var revitElement = uiDoc.Document.GetElement(selectedIds.FirstOrDefault());

                        var revitEntity = revitElement.GetEntity(externalSchema);

                        //if entity already exists data gets attached -> multiple property sets for one element possible
                        if (revitEntity.IsValid())
                        {
                            var data = revitEntity.Get <IDictionary<string, string>>("data");

                            if (data.ContainsKey(node.name))
                            {
                                data[node.name] = editor.getDataAsJsonString();
                            }

                            else
                            {
                                data.Add(node.name, editor.getDataAsJsonString());
                            }

                            revitEntity.Set<IDictionary<string, string>>("data", data);
                            var classificationAsJSONString = JsonConvert.SerializeObject(node.ifcClassification);
                            revitEntity.Set<string>("ifcClassification", classificationAsJSONString);
                            revitElement.SetEntity(revitEntity);

                        }

                        else
                        {
                            var data = new Dictionary<string, string>();
                            data.Add(node.name, editor.getDataAsJsonString());
                            revitEntity = new Entity(externalSchema);
                            revitEntity.Set<IDictionary<string, string>>("data", data);
                            var classificationAsJSONString = JsonConvert.SerializeObject(node.ifcClassification);
                            revitEntity.Set<string>("ifcClassification", classificationAsJSONString);
                            revitElement.SetEntity(revitEntity);
                        }

                        trans.Commit();
                    }
                }
            }
        }

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                queryBtn_click(sender, e);
            }
        }
    }
}
