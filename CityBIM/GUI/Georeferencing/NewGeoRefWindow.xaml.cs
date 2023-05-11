using System;
using System.Windows;

using Autodesk.Revit.UI;


namespace CityBIM.GUI.Georeferencing
{
    /// <summary>
    /// Interaction logic for NewGeoRefWindow.xaml
    /// </summary>
    public partial class NewGeoRefWindow : Window
    {
        public NewGeoRefWindow()
        {
            InitializeComponent();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var vm = (GeoRefViewModel)DataContext;
            vm.SaveSettings();

            TaskDialog.Show("Settings applied!", "Settings have been stored successfully!");
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
