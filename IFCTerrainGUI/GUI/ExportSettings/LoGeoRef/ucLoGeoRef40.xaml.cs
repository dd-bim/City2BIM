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

using IFCTerrainGUI.GUI.MainWindowLogic; //included to provide error handling

using System.Globalization; //included to use culture info (parsing double values)

namespace IFCTerrainGUI.GUI.ExportSettings
{
    /// <summary>
    /// Interaction logic for ucLoGeoRef40.xaml
    /// </summary>
    public partial class ucLoGeoRef40 : UserControl
    {
        public ucLoGeoRef40()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Changing the selection off (is initalized by default)
        /// </summary>
        private void rbLoGeoRef40Default_Checked(object sender, RoutedEventArgs e)
        {
            //in this case set to default (background: the project center will be used)
            MainWindow.jSettings.customOrigin = false;

            //set task (file opening) to true
            MainWindowBib.selectGeoRef = true;

            //set tasks to true
            valueXset = valueYset = valueZset = true;

            //check if input all input fields are filled 
            readyCheck();
        }

        private void rbLoGeoRef40Default_Unchecked(object sender, RoutedEventArgs e)
        {
            //set task (file opening) to false: user have to apply settings
            MainWindowBib.selectGeoRef = false;

            //set tasks to false
            valueXset = valueYset = valueZset = false;

            //ready check again
            readyCheck();
        }

        private void rbLoGeoRef40User_Unchecked(object sender, RoutedEventArgs e)
        {
            //disable input fields
            inputGrid.IsEnabled = false;
        }

        private void rbLoGeoRef40User_Checked(object sender, RoutedEventArgs e)
        {
            //set json settings (use of custom origin)
            MainWindow.jSettings.customOrigin = true;

            //enabling input fields
            inputGrid.IsEnabled = true;

            //check if input all input fields are filled 
            readyCheck();
        }


        /// <summary>
        /// task to check if all required fields are set (and not empty) [Error Handling]
        /// </summary>
        private bool readyCheck()
        {
            //if all tasks are true
            if (valueXset && valueYset && valueZset && !tbRotationLevel40.IsEnabled)
            {
                //enable apply button
                btnLoGeoRef40Apply.IsEnabled = true;
                return true;
            }
            else if (valueXset && valueYset && valueZset)
            {
                //enable apply button
                btnLoGeoRef40Apply.IsEnabled = true;

                //return
                return true;
            }
            else if (valueRotationset && rbLoGeoRef40Default.IsChecked == true)
            {
                //enable apply button
                btnLoGeoRef40Apply.IsEnabled = true;

                //return
                return true;
            }
            else
            {
                //disable apply button
                btnLoGeoRef40Apply.IsEnabled = false;
                return false;
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
        private bool valueRotationset { get; set; }

        /// <summary>
        /// function for error handling to set bool settings (for ready checker)
        /// </summary>
        private void tbInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            //check if tb x is not empty
            if (!string.IsNullOrEmpty(tbLoGeoRef40ValueX.Text))
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
            if (!string.IsNullOrEmpty(tbLoGeoRef40ValueY.Text))
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
            if (!string.IsNullOrEmpty(tbLoGeoRef40ValueZ.Text))
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

            //check if tb z is not empty
            if (!string.IsNullOrEmpty(tbRotationLevel40.Text))
            {
                //set to true
                valueRotationset = true;
            }
            //if tb rotation is empty
            else
            {
                //set to FALSE
                valueRotationset = false;
            }

            //check if all fields are not empty any more
            readyCheck();
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

        /// <summary>
        /// task to apply logeoref 40
        /// </summary>
        private void btnLoGeoRef40Apply_Click(object sender, RoutedEventArgs e)
        {
            //set json settings (use of level of georef 40)
            MainWindow.jSettings.logeoref = BIMGISInteropLibs.IFC.LoGeoRef.LoGeoRef40;

            //if custom origin: set values of input fields to json settings
            if (MainWindow.jSettings.customOrigin)
            {
                //set to json settings
                MainWindow.jSettings.xOrigin = Double.Parse(tbLoGeoRef40ValueX.Text, CultureInfo.CurrentCulture);
                MainWindow.jSettings.yOrigin = Double.Parse(tbLoGeoRef40ValueY.Text, CultureInfo.CurrentCulture);
                MainWindow.jSettings.zOrigin = Double.Parse(tbLoGeoRef40ValueZ.Text, CultureInfo.CurrentCulture);
            }

            //check if user input rotation
            if(this.chkRotationLevel40.IsChecked == true)
            {
                //set rotation to json settings
                MainWindow.jSettings.trueNorth = Double.Parse(tbRotationLevel40.Text, CultureInfo.CurrentCulture);
            }
            else
            {
                //set to defaul value
                MainWindow.jSettings.trueNorth = 0;
            }

            //set task (file opening) to true (user applyed)
            MainWindowBib.selectGeoRef = true;

            //set gui log
            MainWindowBib.setGuiLog("LoGeoRef40 set.");

            //set task (file opening) to true
            MainWindowBib.selectGeoRef = true;

            //check if all task are allready done
            MainWindowBib.readyState();
        }

        /// <summary>
        /// enable input fields for rotation <para/>
        /// include error handling tasks
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkRotationLevel40_Checked(object sender, RoutedEventArgs e)
        {
            //enable textbox
            tbRotationLevel40.IsEnabled = true;

            //check if all fields are not empty any more
            readyCheck();
        }

        /// <summary>
        /// enable input fields for rotation <para/>
        /// include error handling tasks
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkRotationLevel40_Unchecked(object sender, RoutedEventArgs e)
        {
            //diable textbox
            tbRotationLevel40.IsEnabled = false;

            //check if all fields are not empty any more
            readyCheck();
        }
    }
}
