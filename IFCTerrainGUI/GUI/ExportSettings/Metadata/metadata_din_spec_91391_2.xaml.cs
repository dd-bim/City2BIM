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

namespace IFCTerrainGUI.GUI.ExportSettings.Metadata
{
    /// <summary>
    /// Interaktionslogik für metadata_din_spec_91391_2.xaml
    /// </summary>
    public partial class metadata_din_spec_91391_2 : Window
    {
        public metadata_din_spec_91391_2()
        {
            InitializeComponent();

            //create guid
            Guid guid = Guid.NewGuid();
            //#1 set guid
            MainWindow.jSettings91391.id = guid;
            tbUUID.Text = guid.ToString();
        }

        private void btnApplyMetadata913912_Click(object sender, RoutedEventArgs e)
        {
            //#2 set type
            MainWindow.jSettings91391.type = "DTM";

            //#3 set description
            MainWindow.jSettings91391.description = this.tbDescription.Text;

            //#4 set creation date (set always the current day)
            MainWindow.jSettings91391.created = DateTime.Now.ToShortDateString();

            //#5 set creator
            MainWindow.jSettings91391.creator = this.tbCreator.Text;

            //#6 set revision
            MainWindow.jSettings91391.revision = this.tbRevision.Text;

            //#6 set version
            MainWindow.jSettings91391.version = this.tbVersion.Text;

            //#7 ser project id
            MainWindow.jSettings91391.projectId = this.tbProjectId.Text;

            //#8 meta scheme
            MainWindow.jSettings91391.metaScheme = this.tbMetaShema.Text;

            //unlock MainWindow
            ((MainWindow)Application.Current.MainWindow).IsEnabled = true;

            //close current window
            Close();
        }
    }
}
