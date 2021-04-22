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

using IFCTerrainGUI.GUI.MainWindowLogic;
using System.Text.RegularExpressions; //include to be able to restrict textbox entries

namespace IFCTerrainGUI.GUI.ExportSettings
{
    /// <summary>
    /// Interaktionslogik für LoGeoRef50_CRS_Metadata.xaml
    /// </summary>
    public partial class LoGeoRef50_CRS_Metadata : Window
    {
        public LoGeoRef50_CRS_Metadata()
        {
            InitializeComponent();
        }

        /// <summary>
        /// function 
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //unlock MainWindow
            ((MainWindow)Application.Current.MainWindow).IsEnabled = true;

            //check state
            MainWindowBib.readyState();
        }

        /// <summary>
        /// function to set meta data and close window
        /// </summary>
        private void btnApplyCrsMetadata_Click(object sender, RoutedEventArgs e)
        {
            //crs name
            if (!string.IsNullOrEmpty(tbCrsName.Text))
            {
                //parse to int and pass to json settings
                MainWindow.jSettings.crsName = int.Parse(tbCrsName.Text);
            }
            else
            {
                //default value
                MainWindow.jSettings.crsName = int.Parse("000000");
            }

            //crs discription
            if (!string.IsNullOrEmpty(tbDescription.Text))
            {
                //
                MainWindow.jSettings.crsDescription = tbDescription.Text;
            }
            else
            {
                //
                MainWindow.jSettings.crsDescription = "[Placeholder - Discription]";
            }

            //crs geodetic datum
            if (!string.IsNullOrEmpty(tbGeodeticDatum.Text))
            {
                MainWindow.jSettings.geodeticDatum = tbGeodeticDatum.Text;
            }
            else
            {
                MainWindow.jSettings.geodeticDatum = "[Placeholder - geodetic datum]";
            }

            //crs vertical datum
            if (!string.IsNullOrEmpty(tbVerticalDatum.Text))
            {
                MainWindow.jSettings.verticalDatum = tbVerticalDatum.Text;
            }
            else
            {
                MainWindow.jSettings.verticalDatum = "[Placeholder - vertical datum]";
            }


            //crs projection name
            if (!string.IsNullOrEmpty(tbProjectionName.Text))
            {
                MainWindow.jSettings.projectionName = tbProjectionName.Text;
            }
            else
            {
                MainWindow.jSettings.projectionName = "[Placeholder - projection name]";
            }

            //crs projection zone
            if (!string.IsNullOrEmpty(tbProjectionZone.Text))
            {
                MainWindow.jSettings.projectionZone = tbProjectionZone.Text;
            }
            else
            {
                MainWindow.jSettings.projectionZone = "[Placeholder - projection zone]";
            }

            //gui logging (user information)
            ((MainWindow)Application.Current.MainWindow).tbGuiLogging.Items.Add("[LoGeoRef50] CRS Metadata adopted!");

            //Close window
            Close();
        }

        /// <summary>
        /// regex for input of crs (only epsg code)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbCrsName_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {

            //regex only numbers (no comma or dot)
            Regex regex = new Regex("^[0-9]*$");

            //if not valid no input follows
            e.Handled = !regex.IsMatch(e.Text);

        }
    }
}
