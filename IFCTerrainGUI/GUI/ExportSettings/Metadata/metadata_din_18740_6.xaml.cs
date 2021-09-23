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

//shortcut to set logging messages
using guiLog = GuiHandler.GuiSupport;

namespace IFCTerrainGUI.GUI.ExportSettings.Metadata
{
    /// <summary>
    /// Interaktionslogik für metadata_din_18740_6.xaml
    /// </summary>
    public partial class metadata_din_18740_6 : Window
    {
        public metadata_din_18740_6()
        {
            InitializeComponent();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
            
            // gui logging
            guiLog.setLog(BIMGISInteropLibs.Logging.LogType.info, "Metadata 'DIN 18740-6' set.");
        }

        /// <summary>
        /// query epsg code
        /// </summary>
        private void epsgCode_Click(object sender, RoutedEventArgs e)
        {
            var config = DataContext as BIMGISInteropLibs.IfcTerrain.configDin18740;

            //error handling (disable UI & set mouse cursor to wait)
            IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;

            //send request
            var projCRS = BIMGISInteropLibs.ProjCRS.Request.get(config.epsgCode, out bool isValid);

            if (isValid)
            {
                //
                tbEpsg.BorderBrush = Brushes.Green;
                
                //get code
                int code = projCRS.BaseCoordRefSystem.Code;

                config.positionReferenceSystem = projCRS.Name;
                
                //send request for geodetic coord ref
                var geoCRS = BIMGISInteropLibs.GeodeticCRS.GeodeticCRS.get(code);

                //get geoCRS EPSG Code
                code = geoCRS.Datum.Code;

                //send datum request
                var datum = BIMGISInteropLibs.Datum.Datum.get(code);

                //vertical datum (need other request)
                config.projection = datum.Ellipsoid.Name;
            }
            else
            {
                tbEpsg.BorderBrush = Brushes.Red;

                MessageBox.Show("EPSG code '" + config.epsgCode.ToString() + "' invalid!", "EPSG code invalid", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            IsEnabled = true;
            Mouse.OverrideCursor = null;
        }
    }
}
