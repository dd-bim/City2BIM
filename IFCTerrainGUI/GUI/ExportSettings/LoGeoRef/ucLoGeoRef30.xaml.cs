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
    /// Interaction logic for ucLoGeoRef30.xaml
    /// </summary>
    public partial class ucLoGeoRef30 : UserControl
    {
        public ucLoGeoRef30()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Changing the selection off (is initalized by default)
        /// </summary>
        private void rbLoGeoRef30Default_Checked(object sender, RoutedEventArgs e)
        {
            //in this case set to default (background: the project center will be used)
            MainWindow.jSettings.customOrigin = false;

            //set task (file opening) to true
            MainWindowBib.selectGeoRef = true;

            //enable apply button
            btnLoGeoRef30Apply.IsEnabled = true;
        }

        /// <summary>
        /// task to enable input fields
        /// </summary>
        private void rbLoGeoRef30User_Checked(object sender, RoutedEventArgs e)
        {
            //set json settings (use of custom origin)
            MainWindow.jSettings.customOrigin = true;

            //enabling input fields
            inputGrid.IsEnabled = true;
           
            //check if input all input fields are filled 
            readyCheck();
        }

        private void rbLoGeoRef30Default_Unchecked(object sender, RoutedEventArgs e)
        {
            //set task (file opening) to false: user have to apply settings
            MainWindowBib.selectGeoRef = false;
        }

        private void rbLoGeoRef30User_Unchecked(object sender, RoutedEventArgs e)
        {
            //disable input fields (not needed)
            
        }

        /// <summary>
        /// task to apply custom origin
        /// </summary>
        private void btnLoGeoRef30Apply_Click(object sender, RoutedEventArgs e)
        {
            //set json settings (use of level of georef 30)
            MainWindow.jSettings.logeoref = BIMGISInteropLibs.IfcTerrain.LoGeoRef.LoGeoRef30;

            //if custom origin: set values of input fields to json settings
            if (MainWindow.jSettings.customOrigin)
            {
                //set to json settings
                MainWindow.jSettings.xOrigin = Double.Parse(tbLoGeoRef30ValueX.Text, CultureInfo.CurrentCulture);
                MainWindow.jSettings.yOrigin = Double.Parse(tbLoGeoRef30ValueY.Text, CultureInfo.CurrentCulture);
                MainWindow.jSettings.zOrigin = Double.Parse(tbLoGeoRef30ValueZ.Text, CultureInfo.CurrentCulture);
            }
                        
            //set task (file opening) to true (user applyed)
            MainWindowBib.selectGeoRef = true;

            //check if all task are allready done
            MainWindowBib.readyState();
        }

        /// <summary>
        /// task to check if all required fields are set (and not empty) [Error Handling]
        /// </summary>
        private bool readyCheck()
        {
            //if all tasks are true
            if (valueXset && valueYset && valueZset && (validationError == 0))
            {
                //enable apply button
                btnLoGeoRef30Apply.IsEnabled = true;

                //return
                return true;
            }
            else
            {
                //disable apply button
                btnLoGeoRef30Apply.IsEnabled = false;
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
        /// counter of validation errors
        /// </summary>
        private int validationError { get; set; }

        /// <summary>
        /// function for error handling to set bool settings (for ready checker)
        /// </summary>
        private void tbInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            //check if tb x is not empty
            if (!string.IsNullOrEmpty(tbLoGeoRef30ValueX.Text))
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
            if (!string.IsNullOrEmpty(tbLoGeoRef30ValueY.Text))
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
            if (!string.IsNullOrEmpty(tbLoGeoRef30ValueZ.Text))
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

            //check if all fields are not empty any more
            readyCheck();
        }


        /// <summary>
        /// will be executed as soon as a validation error occurs
        /// Application: deactivate the apply button if necessary
        /// </summary>
        private void tbNum_Error(object sender, ValidationErrorEventArgs e)
        {
            //if event is null
            if (e == null)
            {
                //output error
                throw new Exception("Unexpected event args");
            }
            //loop through whether error is present or not
            switch (e.Action)
            {
                //if there is an error --> count up
                case ValidationErrorEventAction.Added:
                    {
                        validationError++;
                        break;
                    }
                //for each removed error --> count down
                case ValidationErrorEventAction.Removed:
                    {
                        validationError--;
                        break;
                    }
                //error output
                default:
                    {
                        throw new Exception("Unknown action");
                    }
            }
        }
    }
}
