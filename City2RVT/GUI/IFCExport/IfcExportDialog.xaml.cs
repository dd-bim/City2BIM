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

namespace City2RVT.GUI.IFCExport
{
    /// <summary>
    /// Interaction logic for IfcExportDialog.xaml
    /// </summary>
    public partial class IfcExportDialog : Window
    {
        public IfcExportDialog()
        {
            InitializeComponent();
        }

        private void browseButton_Click(object sender, RoutedEventArgs e)
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

        }

        private void cancelBtn_click(object sender, RoutedEventArgs e)
        {

        }

        /*
        private void selectAllBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void clearSelectionBtn_Click(object sender, RoutedEventArgs e)
        {

        }
        */
    }
}
