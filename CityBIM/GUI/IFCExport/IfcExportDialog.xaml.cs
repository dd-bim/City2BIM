using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using CityBIM.IFCExport;

namespace CityBIM.GUI.IFCExport
{
    /// <summary>
    /// Interaction logic for IfcExportDialog.xaml
    /// </summary>
    public partial class IfcExportDialog : Window
    {
        public RevitGeoIfcExporter.ExportType ExportType;
        public bool startExport = false;
        public string ExportPath
        {
            get => exportPath;
        }

        private string exportPath { get; set; }

        public IfcExportDialog()
        {
            InitializeComponent();
        }

        private void saveAsButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "IfcExport";
            dlg.DefaultExt = ".ifc";
            dlg.Filter = "Ifc File (.ifc)|*.ifc";

            if (dlg.ShowDialog() == true)
            {
                filePathBox.Text = dlg.FileName;
            }
        }

        private void exportBtn_click(object sender, RoutedEventArgs e)
        {
            if (filePathBox.Text != null )
            {
                exportPath = filePathBox.Text;
                startExport = true;
                var checkedRadioBtn = ExportTypePanel.Children.OfType<RadioButton>().FirstOrDefault(r => r.IsChecked.HasValue && r.IsChecked.Value);
                Enum.TryParse(checkedRadioBtn.Content.ToString(), out this.ExportType);
                this.Close();
            }
            else
            {
                MessageBox.Show("Please specify a valid output path", "Warning", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void cancelBtn_click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /*private void radioButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton checkedButton = sender as RadioButton;
            if (checkedButton.IsChecked.Value)
            {
                ExportType = (RevitIfcExporter.ExportType) checkedButton.Content;
            }
        }*/

    }
}
