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

//shortcut to set json settings
using init = GuiHandler.InitClass;



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
            MainWindowBib.enableStart(GuiHandler.GuiSupport.readyState());
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
                init.config.crsName = int.Parse(tbCrsName.Text);
            }
            else
            {
                //default value
                init.config.crsName = int.Parse("000000");
            }

            //crs discription
            if (!string.IsNullOrEmpty(tbDescription.Text))
            {
                init.config.crsDescription = tbDescription.Text;
            }
            else
            {
                init.config.crsDescription = "[Placeholder - Discription]";
            }

            //crs geodetic datum
            if (!string.IsNullOrEmpty(tbGeodeticDatum.Text))
            {
                init.config.geodeticDatum = tbGeodeticDatum.Text;
            }
            else
            {
                init.config.geodeticDatum = "[Placeholder - geodetic datum]";
            }

            //crs vertical datum
            if (!string.IsNullOrEmpty(tbVerticalDatum.Text))
            {
                init.config.verticalDatum = tbVerticalDatum.Text;
            }
            else
            {
                init.config.verticalDatum = "[Placeholder - vertical datum]";
            }


            //crs projection name
            if (!string.IsNullOrEmpty(tbProjectionName.Text))
            {
                init.config.projectionName = tbProjectionName.Text;
            }
            else
            {
                init.config.projectionName = "[Placeholder - projection name]";
            }

            //crs projection zone
            if (!string.IsNullOrEmpty(tbProjectionZone.Text))
            {
                init.config.projectionZone = tbProjectionZone.Text;
            }
            else
            {
                init.config.projectionZone = "[Placeholder - projection zone]";
            }

            //gui logging (user information)
            
            //((MainWindow)Application.Current.MainWindow).ucUILog.tbGuiLogging.Items.Add("[LoGeoRef50] CRS Metadata adopted!");
                
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

        /// <summary>
        /// insert the readed epsg codes [TODO: add error handler for, that epsg code is requested before]
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                //set crs name (epsg code)
                tbCrsName.Text = init.config.crsName.ToString();

                //set description
                tbDescription.Text = init.config.crsDescription.ToString();

                //set geodetic datum
                tbGeodeticDatum.Text = init.config.geodeticDatum;

                //set vertical datum
                tbVerticalDatum.Text = init.config.verticalDatum;

                //set projection name
                tbProjectionName.Text = init.config.projectionName;

                //set projection zone
                tbProjectionZone.Text = init.config.projectionZone;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                //TODO: log exception
            }
            
        }
    }
}
