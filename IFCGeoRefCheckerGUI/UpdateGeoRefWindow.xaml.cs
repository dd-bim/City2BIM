using IFCGeoRefCheckerGUI.ValueConverters;
using IFCGeoRefCheckerGUI.ViewModels;
using Microsoft.Win32;
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
using System.Windows.Shapes;

namespace IFCGeoRefCheckerGUI
{
    /// <summary>
    /// Interaction logic for UpdateGeoRefWindow.xaml
    /// </summary>
    public partial class UpdateGeoRefWindow : Window
    {
        public UpdateGeoRefWindow()
        {
            InitializeComponent();
        }

        private void setOutBtn_Click(object sender, RoutedEventArgs e)
        {
            var vm = (UpdateViewModel)DataContext;

            var outFileName = System.IO.Path.GetFileName(vm.OutIfcPath);
            var defaultDirectory = System.IO.Path.GetDirectoryName(vm.OutIfcPath);

            var dialog = new SaveFileDialog
            {
                Title = "Select location for exported Ifc file",
                FileName = outFileName,
                InitialDirectory = defaultDirectory,
                Filter = "IFC files (*.ifc)|*.ifc|All files (*.*)|*.*"
            };

            var result = dialog.ShowDialog();
            
            if (result == true)
            {
                vm.OutIfcPath = dialog.FileName;
            }
        }

        private void exportBtn_Click(object sender, RoutedEventArgs e)
        {
            var vm = (UpdateViewModel)DataContext;

            var selectedPath = vm.OutIfcPath;

            if (System.IO.File.Exists(selectedPath))
            {
                var mbr = MessageBox.Show("File already exists!\nDo you want to overwrite it?", "File already exists!", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                if (mbr == MessageBoxResult.OK)
                {
                    var inputIsValid = InputValidator.IsValid(this);
                    if (!inputIsValid)
                    {
                        MessageBox.Show("One or more input fields are invalid. Please correct them!", "Validation errors", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                    vm.StartExport!.Execute(null);
                    this.Close();
                }
            }
        }
    }
}
