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

using IFCTerrainGUI.GUI.MainWindowLogic; //include for error handling

//logging
using BIMGISInteropLibs.Logging; //access to log writer
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

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

            //create new instance of window
            metadata_din_18740_6 metadata_Din_18740_6 = new metadata_din_18740_6();
            
            //open window
            metadata_Din_18740_6.ShowDialog();
        }

        
        /// <summary>
        /// enable button to input all metadata
        /// </summary>
        private void chkDin18740Export_Checked(object sender, RoutedEventArgs e)
        {
            btnOpenWindowMetadata187406.IsEnabled = true;
        }

        /// <summary>
        /// disable button (input window not needed)
        /// </summary>
        private void chkDin18740Export_Unchecked(object sender, RoutedEventArgs e)
        {
            btnOpenWindowMetadata187406.IsEnabled = false;
        }


        /// <summary>
        /// set all json settings (most import button in this controler)
        /// </summary>
        private void btnAplly_Click(object sender, RoutedEventArgs e)
        {
            //export metadata json
            if (chkJsonExport.IsChecked == true)
            {
                //set to true
                MainWindow.jSettings.exportMetadataFile = true;
                LogWriter.Entries.Add(new LogPair(LogType.debug, "[GUI] Metadata export 'JSON' set."));
                MainWindowBib.setGuiLog("Metadata export 'JSON' set.");

            }

            //export as property set
            if (chkIfcPropertySet.IsChecked == true)
            {
                //set output of IfcPropertySet to true
                MainWindow.jSettings.outIfcPropertySet = true;
                LogWriter.Entries.Add(new LogPair(LogType.debug, "[GUI] Metadata export 'IfcPropertySet' set."));
                MainWindowBib.setGuiLog("Metadata export 'IfcPropertySet' set.");
            }

            //check if export of DIN 91391-2
            if (chkDin91391Export.IsChecked == true)
            {
                MainWindow.jSettings.exportMetadataDin91391 = true;
            }

            //check if export of DIN 18740-6
            if (chkDin18740Export.IsChecked == true)
            {
                MainWindow.jSettings.exportMetadataDin18740 = true;
            }

            //if non of those should not be exported set json settings (too)
            else
            {
                //set json setting to false
                MainWindow.jSettings.exportMetadataFile = false;
                MainWindow.jSettings.exportMetadataDin91391 = false;
                MainWindow.jSettings.exportMetadataDin18740 = false;
            }

            //set task (metadata) to true
            MainWindowBib.selectMetadata = true;
            
            //gui logging
            MainWindowBib.setGuiLog("Metadata 'export' set.");
            


            //check if all task are allready done
            MainWindowBib.readyState();
        }

        /// <summary>
        /// error handling - enable button
        /// </summary>
        private void chkDin91391Export_Checked(object sender, RoutedEventArgs e)
        {
            btnOpenWindowMetadata913912.IsEnabled = true;
        }

        /// <summary>
        /// error handling - disable button
        /// </summary>
        private void chkDin91391Export_Unchecked(object sender, RoutedEventArgs e)
        {
            btnOpenWindowMetadata913912.IsEnabled = false;
        }

        /// <summary>
        /// open input window for metadata DIN SPEC 91391-2
        /// </summary>
        private void btnOpenWindowMetadata913912_Click(object sender, RoutedEventArgs e)
        {
            //lock current MainWindow (because Background Worker is triggered)
            //so the user can not change any settings during the time the background worker is running
            ((MainWindow)Application.Current.MainWindow).IsEnabled = false;

            //open window for user input
            metadata_din_spec_91391_2 metadata_Din_Spec_91391_2 = new metadata_din_spec_91391_2();
            metadata_Din_Spec_91391_2.ShowDialog();
        }
    }
}
