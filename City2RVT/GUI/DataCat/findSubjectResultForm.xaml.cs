using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Autodesk.Revit.UI;

using City2RVT.ExternalDataCatalog;

namespace City2RVT.GUI.DataCat
{
    /// <summary>
    /// Interaction logic for findSubjectResultForm.xaml
    /// </summary>
    public partial class findSubjectResultForm : Window
    {
        public findSubjectResultForm()
        {
            InitializeComponent();
            //trvFindResult.ItemsSource = response.data.findSubjects.nodes;
        }

        private void queryBtn_click(object sender, RoutedEventArgs e)
        {
            var searchText = SearchBox.Text;
            
            bool tokenStatus = ExternalDataUtils.testTokenValidity();

            if (tokenStatus == false)
            {
                TaskDialog.Show("Error!", "You are currently not logged into the external server!");
            }

            else
            {
                var response = Prop_Revit.DataClient.querySubjects(searchText);
                trvFindResult.ItemsSource = response.data.findSubjects.nodes;
            }
        }
    }
}
