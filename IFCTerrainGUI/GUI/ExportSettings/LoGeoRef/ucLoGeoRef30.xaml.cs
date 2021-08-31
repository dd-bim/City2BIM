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

//shortcut to set json settings
using init = GuiHandler.InitClass;

//shortcut to set logging messages
using guiLog = GuiHandler.GuiSupport;

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
            init.config.customOrigin = false;

            //set task (file opening) to true
            GuiHandler.GuiSupport.selectGeoRef = true;

            //enable apply button
            btnLoGeoRef30Apply.IsEnabled = true;
        }

        /// <summary>
        /// task to enable input fields
        /// </summary>
        private void rbLoGeoRef30User_Checked(object sender, RoutedEventArgs e)
        {
            //set json settings (use of custom origin)
            init.config.customOrigin = true;

            //check if input all input fields are filled 
            readyCheck();
        }

        private void rbLoGeoRef30Default_Unchecked(object sender, RoutedEventArgs e)
        {
            //set task (file opening) to false: user have to apply settings
            GuiHandler.GuiSupport.selectGeoRef = false;
        }

        /// <summary>
        /// task to apply custom origin
        /// </summary>
        private void btnLoGeoRef30Apply_Click(object sender, RoutedEventArgs e)
        {
            //set json settings (use of level of georef 30)
            init.config.logeoref = BIMGISInteropLibs.IFC.LoGeoRef.LoGeoRef30;

            //if custom origin: set values of input fields to json settings
            if (init.config.customOrigin.GetValueOrDefault())
            {
                //set to json settings
                init.config.xOrigin = Double.Parse(tbLoGeoRef30ValueX.Text, CultureInfo.CurrentCulture);
                init.config.yOrigin = Double.Parse(tbLoGeoRef30ValueY.Text, CultureInfo.CurrentCulture);
                init.config.zOrigin = Double.Parse(tbLoGeoRef30ValueZ.Text, CultureInfo.CurrentCulture);
            }

            //
            if (valueRotation)
            {
                init.config.trueNorth = Double.Parse(tbRotationLevel30.Text, CultureInfo.CurrentCulture);
            }

            //set task (file opening) to true (user applyed)
            GuiHandler.GuiSupport.selectGeoRef = true;

            //set gui log
            guiLog.setLog("LoGeoRef30 set.");

            //set task (file opening) to true
            GuiHandler.GuiSupport.selectGeoRef = true;

            //check if all task are allready done
            MainWindowBib.enableStart(GuiHandler.GuiSupport.readyState());
        }

        /// <summary>
        /// task to check if all required fields are set (and not empty) [Error Handling]
        /// </summary>
        private bool readyCheck()
        {
            //if all tasks are true
            if (valueXset && valueYset && valueZset && !valueRotation)
            {
                //enable apply button
                btnLoGeoRef30Apply.IsEnabled = true;

                //return
                return true;
            }
            else if (valueXset && valueYset && valueZset && valueRotation)
            {
                //enable apply button
                btnLoGeoRef30Apply.IsEnabled = true;
                return true;
            }else if(valueRotation && rbLoGeoRef30Default.IsChecked.GetValueOrDefault())
            {
                btnLoGeoRef30Apply.IsEnabled = true;
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
        /// 
        /// </summary>
        private bool valueRotation { get; set; }
        

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

            if (!string.IsNullOrEmpty(tbRotationLevel30.Text))
            {
                //set to true
                valueRotation = true;
            }
            else
            {
                //set to false
                valueRotation = false;
            }

            //check if all fields are not empty any more
            readyCheck();
        }

        /// <summary>
        /// check the textboxes (bounding box values) input if it corresponds to the regex
        /// </summary>
        private void tbLoGeoRef30Value_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            //regex only numbers (no comma or dot)
            Regex regex = new Regex("^[a-zA-Z]*$");

            //if not valid no input follows
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
