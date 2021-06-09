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

using System.Text.RegularExpressions; //include to be able to restrict textbox entries

using IFCTerrainGUI.GUI.MainWindowLogic; //embed for error handling

using System.Globalization; //included to use culture info (parsing double values)

using BIMGISInteropLibs.ProjCRS;//included to request epsg codes

namespace IFCTerrainGUI.GUI.ExportSettings
{
    /// <summary>
    /// Interaction logic for ucLoGeoRef50.xaml
    /// </summary>
    public partial class ucLoGeoRef50 : UserControl
    {
        public ucLoGeoRef50()
        {
            InitializeComponent();
        }

        /// <summary>
        /// function to open input window for crs metadata
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOpenCrsMetadata_Click(object sender, RoutedEventArgs e)
        {
            //lock current MainWindow
            //so the user can not change any settings during this window is opend
            ((MainWindow)Application.Current.MainWindow).IsEnabled = false;

            //create new instance of window
            LoGeoRef50_CRS_Metadata windowCrsMeta = new LoGeoRef50_CRS_Metadata();

            //open window
            windowCrsMeta.ShowDialog();
        }

        /// <summary>
        /// check the textboxes (bounding box values) input if it corresponds to the regex
        /// </summary>
        private void tbLoGeoRefValue_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            //regex only numbers (no comma or dot)
            Regex regex = new Regex("^[a-zA-Z]*$");

            //if not valid no input follows
            e.Handled = regex.IsMatch(e.Text);
        }
        
        #region user origin
        /// <summary>
        ///  Changing the selection off (is initalized by default)
        /// </summary>
        private void rbLoGeoRef50Default_Checked(object sender, RoutedEventArgs e)
        {
            //in this case set to default (background: the project center will be used)
            MainWindow.jSettings.customOrigin = false;

            //set task (file opening) to true
            MainWindowBib.selectGeoRef = true;

            //set tasks to true
            valueXset = valueYset = valueZset = true;

            //check if all input fields are filled
            readyCheck();
        }

        /// <summary>
        /// solution otherwise the init of the gui won't work
        /// </summary>
        private void rbLoGeoRef50Default_Unchecked(object sender, RoutedEventArgs e)
        {
            //set task (file opening) to false: user have to apply settings
            MainWindowBib.selectGeoRef = false;

            //set tasks to false
            valueXset = valueYset = valueZset = false;

            //ready check again
            readyCheck();
        }
        #endregion user origin

        #region user specific

        private void rbLoGeoRef50User_Checked(object sender, RoutedEventArgs e)
        {
            //set json settings (use of custom origin)
            MainWindow.jSettings.customOrigin = true;

            //enabling input fields
            inputGrid.IsEnabled = true;

            //check if input all input fields are filled 
            readyCheck();
        }
        /// <summary>
        /// workaround to disable input fields
        /// </summary>
        private void rbLoGeoRef50User_Unchecked(object sender, RoutedEventArgs e)
        {
            //disable input fields
            inputGrid.IsEnabled = false;
        }
        #endregion user specific

        #region error handling (ready checker)
        /// <summary>
        /// task to check if all required fields are set (and not empty) [Error Handling]
        /// </summary>

        /// <summary>
        /// task to check if all required fields are set (and not empty) [TODO: Error Handling]
        /// </summary>
        private void readyCheck()
        {
            //if all tasks are true
            if (valueXset && valueYset && valueZset && !chkScaleLevel50.IsChecked == true)
            {
                //enable apply button
                btnApplyLoGeoRef50.IsEnabled = true;
            }
            else if (valueXset && valueYset && valueZset)
            {
                //enable apply button
                btnApplyLoGeoRef50.IsEnabled = true;
            }
            else if (valueScaleset && rbLoGeoRef50Default.IsChecked == true)
            {
                //enable apply button
                btnApplyLoGeoRef50.IsEnabled = true;
            }
            else
            {
                //disable apply button
                btnApplyLoGeoRef50.IsEnabled = false;
            }
        }
        /// <summary>
        /// check if value x is set
        /// </summary>
        private bool valueXset { get; set; }

        /// <summary>
        /// check if value y is set
        /// </summary>
        private bool valueYset { get; set; }

        /// <summary>
        /// check if value z is set
        /// </summary>
        private bool valueZset { get; set; }

        /// <summary>
        /// check if value rotation is set
        /// </summary>
        //private bool valueRotationset { get; set; }

        /// <summary>
        /// check if value scale is set
        /// </summary>
        private bool valueScaleset { get; set; }

        /// <summary>
        /// function for error handling to set bool settings (for ready checker)
        /// </summary>
        private void tbInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            //check if tb x is not empty
            if (!string.IsNullOrEmpty(tbLoGeoRef50ValueX.Text))
            {
                //set to true
                valueXset = true;
            }
            //if tb x is empty
            else
            {
                //set to FALSE
                valueXset = false;
            }

            //check if tb y is not empty
            if (!string.IsNullOrEmpty(tbLoGeoRef50ValueY.Text))
            {
                //set to true
                valueYset = true;
            }
            //if tb x is empty
            else
            {
                //set to FALSE
                valueYset = false;
            }

            //check if tb z is not empty
            if (!string.IsNullOrEmpty(tbLoGeoRef50ValueZ.Text))
            {
                //set to true
                valueZset = true;
            }
            //if tb x is empty
            else
            {
                //set to FALSE
                valueZset = false;
            }
            /*
            //check if tb roation is not empty
            if (!string.IsNullOrEmpty(tbRotationLevel50.Text))
            {
                //set to true
                valueRotationset = true;
            }
            //if tb rotation is empty
            else
            {
                //set to FALSE
                valueRotationset = false;
            }*/

            //check if tb sclae is not empty
            if (!string.IsNullOrEmpty(tbScaleLevel50.Text))
            {
                //set to true
                valueScaleset = true;
            }
            //if tb scale is empty
            else
            {
                //set to FALSE
                valueScaleset = false;
            }


            //check if all fields are not empty any more
            readyCheck();
        }

        #endregion error handling (ready checker)


        /// <summary>
        /// task to apply logeoref 50
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnApplyLoGeoRef50_Click(object sender, RoutedEventArgs e)
        {
            //set json settings (use of level of georef 50)
            MainWindow.jSettings.logeoref = BIMGISInteropLibs.IFC.LoGeoRef.LoGeoRef50;

            //if custom origin: set values of input fields to json settings
            if (MainWindow.jSettings.customOrigin)
            {
                //set to json settings
                MainWindow.jSettings.xOrigin = Double.Parse(tbLoGeoRef50ValueX.Text, CultureInfo.CurrentCulture);
                MainWindow.jSettings.yOrigin = Double.Parse(tbLoGeoRef50ValueY.Text, CultureInfo.CurrentCulture);
                MainWindow.jSettings.zOrigin = Double.Parse(tbLoGeoRef50ValueZ.Text, CultureInfo.CurrentCulture);
            }

            if(this.chkRotationLevel50.IsChecked == true)
            {
                //set rotation to json settings
                MainWindow.jSettings.trueNorth = Double.Parse(tbRotationLevel50.Text, CultureInfo.CurrentCulture);
            }
            else
            {
                //set to defaul value
                MainWindow.jSettings.trueNorth = 0;
            }

            if (this.chkScaleLevel50.IsChecked == true)
            {
                //set rotation to json settings
                MainWindow.jSettings.scale = Double.Parse(tbScaleLevel50.Text);
            }
            else
            {
                //set to defaul value
                MainWindow.jSettings.scale = 1.0;
            }

            //set gui log
            MainWindowBib.setGuiLog("LoGeoRef50 set.");

            //set task (logeoref) to true
            MainWindowBib.selectGeoRef = true;

            //check if all tasks are allready done
            MainWindowBib.readyState();
        }

        /// <summary>
        /// task if rotation (user input) is checked (error handling)
        /// </summary>
        private void chkRotationLevel50_Checked(object sender, RoutedEventArgs e)
        {
            //enable textbox
            tbRotationLevel50.IsEnabled = true;

            //set task to false
            //valueRotationset = false;

            //check if all fields are not empty any more
            readyCheck();
        }

        /// <summary>
        /// task if rotation (user input) is uncheck (error handling)
        /// </summary>
        private void chkRotationLevel50_Unchecked(object sender, RoutedEventArgs e)
        {
            //diable textbox
            tbRotationLevel50.IsEnabled = false;

            //set task to true
            //valueRotationset = true;

            //check if all fields are not empty any more
            readyCheck();
        }

        /// <summary>
        /// task if scaling (user input) is checked (error handling)
        /// </summary>
        private void chkScaleLevel50_Checked(object sender, RoutedEventArgs e)
        {
            //enable textbox
            tbScaleLevel50.IsEnabled = true;

            //set task to false;
            valueScaleset = false;

            //check if all fields are not empty any more
            readyCheck();
        }

        /// <summary>
        /// task if scaling (user input) is checked (error handling)
        /// </summary>
        private void chkScaleLevel50_Unchecked(object sender, RoutedEventArgs e)
        {
            //diable textbox
            tbScaleLevel50.IsEnabled = false;
            
            //set task to true;
            valueScaleset = true;

            //check if all fields are not empty any more
            readyCheck();
        }

        /// <summary>
        /// request button for epsg codes
        /// </summary>
        private void requestEPSG_Click(object sender, RoutedEventArgs e)
        {
            //TODO: error handler (integer and not more as 5 numbers)

            //change cursor to wait animation (for user feedback)
            Mouse.OverrideCursor = Cursors.Wait;

            //lock MainWindow a so the user can't make any entries
            ((MainWindow)Application.Current.MainWindow).IsEnabled = false;

            try
            {
                //get epsg code
                int epsgCode = int.Parse(tbEpsgCode.Text);

                //send request
                var projCRS = BIMGISInteropLibs.ProjCRS.Request.get(epsgCode, out bool isValid);

                //disable button
                btnOpenCrsMetadata.IsEnabled = false;

                if (isValid)
                {
                    //EPSG Code
                    MainWindow.jSettings.crsName = projCRS.Code;

                    //CRS Description (example only)
                    MainWindow.jSettings.crsDescription = projCRS.Name;

                    //split for getting name and zone
                    string[] projection = projCRS.Name.Split('/');

                    //projection name
                    MainWindow.jSettings.projectionName = projection[0];

                    //projection zone
                    MainWindow.jSettings.projectionZone = projection[1].Remove(0, 1);

                    int code = projCRS.BaseCoordRefSystem.Code;

                    //send request for geodetic coord ref
                    var geoCRS = BIMGISInteropLibs.GeodeticCRS.GeodeticCRS.get(code);

                    //geodetic datum
                    MainWindow.jSettings.geodeticDatum = geoCRS.Datum.Name;

                    //get geoCRS EPSG Code
                    code = geoCRS.Datum.Code;

                    //send datum request
                    var datum = BIMGISInteropLibs.Datum.Datum.get(code);

                    //vertical datum (need other request)
                    MainWindow.jSettings.verticalDatum = datum.Ellipsoid.Name;

                    //gui logging (user information)
                    MainWindowBib.setGuiLog("EPSG code readed.");

                    //disable button
                    btnOpenCrsMetadata.IsEnabled = true;
                }
            }
            catch
            {
                //disable button
                btnOpenCrsMetadata.IsEnabled = true;

                //gui logging (user information)
                MainWindowBib.setGuiLog("EPSG code invalid please try again!\nOr use manual input!");

            }
            //change cursor to default
            Mouse.OverrideCursor = null;

            //Release MainWindow again --> so the user can make entries again
            ((MainWindow)Application.Current.MainWindow).IsEnabled = true;
        }
    }
}
