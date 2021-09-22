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

//logging
using BIMGISInteropLibs.Logging; //access to log writer
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

//shortcut to set logging messages
using guiLog = GuiHandler.GuiSupport;

namespace IFCTerrainGUI.GUI.ExportSettings.Metadata
{
    /// <summary>
    /// Interaktionslogik für ucMetaJson.xaml
    /// </summary>
    public partial class ucMetaIfcJson : UserControl
    {
        public ucMetaIfcJson()
        {
            InitializeComponent();
        }

        /// <summary>
        /// open window for input metadata DIN 18740-6
        /// </summary>
        private void btnOpenWindowMetadata187406_Click(object sender, RoutedEventArgs e)
        {
            //lock current MainWindow (because Background Worker is triggered)
            //so the user can not change any settings during the time the background worker is running
            ((MainWindow)Application.Current.MainWindow).IsEnabled = false;

            //get config via key
            var config = TryFindResource("configDin187406");

            //create new instance of window
            metadata_din_18740_6 window = new metadata_din_18740_6();

            //set data context
            window.DataContext = config;

            //open window - for input metadata
            window.ShowDialog();

            //enable gui
            ((MainWindow)Application.Current.MainWindow).IsEnabled = true;
        }

        /// <summary>
        /// open input window for metadata DIN SPEC 91391-2
        /// </summary>
        private void btnOpenWindowMetadata913912_Click(object sender, RoutedEventArgs e)
        {
            //lock current MainWindow (because Background Worker is triggered)
            //so the user can not change any settings during the time the background worker is running
            ((MainWindow)Application.Current.MainWindow).IsEnabled = false;

            //get config via key
            var config = TryFindResource("configDin913912");

            //open window for user input
            metadata_din_spec_91391_2 window = new metadata_din_spec_91391_2();

            //set data context
            window.DataContext = config;
            
            //open window - for input metadata
            window.ShowDialog();

            //enable gui
            ((MainWindow)Application.Current.MainWindow).IsEnabled = true;
        }
    }
}
