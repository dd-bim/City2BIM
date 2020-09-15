using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace City2RVT.GUI.Properties
{
    /// <summary>
    /// Interaktionslogik für Wpf_CreateMetaJSON.xaml
    /// </summary>
    public partial class Wpf_CreateMetaJSON : Window
    {
        ExternalCommandData commandData;
        public Wpf_CreateMetaJSON(ExternalCommandData cData)
        {
            InitializeComponent();
            commandData = cData;
            //Cb_Theme.Text = "Select a theme.";
            Cb_Theme.Items.Add("XPlanung");
            Cb_Theme.Items.Add("ALKIS");
            Cb_Theme.Items.Add("ZukunftBau");

        }

        private void Btn_Start_Click(object sender, RoutedEventArgs e)
        {
            if (Cb_Theme.SelectedItem.ToString() == "XPlanung")
            {
                Wpf_showLayer f1 = new Wpf_showLayer(commandData);
                //f1.Text = propertyListBox.SelectedItem.ToString();
                _ = f1.ShowDialog();
            }
            else
            {
                MessageBox.Show("Noch nicht implementiert. ");
            }
            
        }

        private void Cb_Theme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
