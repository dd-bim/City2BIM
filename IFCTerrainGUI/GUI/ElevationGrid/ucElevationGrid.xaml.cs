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

using IFCTerrainGUI.GUI.MainWindowLogic; //included to provide error handling
using System.Text.RegularExpressions; //include to be able to restrict textbox entries
using Microsoft.Win32; //used for file handling

namespace IFCTerrainGUI.GUI.ElevationGrid
{
    /// <summary>
    /// Interaction logic for ucElevationGrid.xaml
    /// </summary>
    public partial class ucElevationGrid : UserControl
    {
        public ucElevationGrid()
        {
            InitializeComponent();
        }

        /// <summary>
        /// opens file handling - elevation grid
        /// </summary>
        private void btnReadGrid_Click(object sender, RoutedEventArgs e)
        {
            //add new FileDialog handler 
            OpenFileDialog ofd = new OpenFileDialog();
            //set filtering so that the following selection is possible (these also represent only the selected files)
            ofd.Filter = "Textfile *.txt|*.txt|XYZ *.xyz|*.xyz";

            //opens the dialog window (if a file is selected, everything inside the loop is executed)
            if (ofd.ShowDialog() == true)
            {
                #region JSON settings
                //set JSON settings of file format 
                //(Referencing to the BIMGISInteropsLibs, for which fileTypes an enumeration is used).
                MainWindow.jSettings.fileType = BIMGISInteropLibs.IfcTerrain.IfcTerrainFileType.Grid;

                //set JSON settings of file path
                MainWindow.jSettings.filePath = ofd.FileName;
                #endregion JSON settings

                #region logging [TODO]
                //TODO: add logging
                #endregion logging [TODO]

                #region gui feedback
                //here a feedback is given to the gui for the user (info panel)
                MainWindowBib.setTextBoxText(((MainWindow)Application.Current.MainWindow).tbFileName, MainWindow.jSettings.filePath);

                //conversion to string, because stored as enumeration
                ((MainWindow)Application.Current.MainWindow).tbFileType.Text = MainWindow.jSettings.fileType.ToString();
                #endregion gui feedback

                //enable grid size field
                stpGridSize.IsEnabled = true;

                //set default value
                tbGridSize.Text = "1";

                //TODO logging that default value ("1" m) has been set

                //enable stack panel for bounding box 
                stpGridBB.IsEnabled = true;
                chkGridBB.IsEnabled = true;
            }
        }

        /// <summary>
        /// include regular expressions so that only numbers can be entered
        /// Note: blanks can also be entered (these must still be removed)
        /// </summary>
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            //regular expression
            Regex regex = new Regex("[^0-9][,.]+");

            //check if input corresponds to a regular expression
            e.Handled = regex.IsMatch(e.Text);
        }

        /// <summary>
        /// will be executed as soon as the checkbox is selected
        /// </summary>
        private void chkGridBB_Checked(object sender, RoutedEventArgs e)
        {
            //enable for input in textboxes
            stpGridBB.IsEnabled = true;

            //diable process button grid --> user need to input in bounding box
            btnProcessGrid.IsEnabled = false;

            //check if all textboxes may allready are filled 
            readyState();
        }

        /// <summary>
        /// will be executed as soon as the checkbox is not selected anymore
        /// </summary>
        private void chkGridBB_Unchecked(object sender, RoutedEventArgs e)
        {
            //disable input fields (not needed anymore)
            stpGridBB.IsEnabled = false;

            //check all textboxes
            readyState();
        }

        /// <summary>
        /// will be executed as soon the user klicked the process button
        /// </summary>
        private void btnProcessGrid_Click(object sender, RoutedEventArgs e)
        {
            #region set json settings
            if (chkGridBB.IsChecked == true)
            {
                //bounding box will be used
                MainWindow.jSettings.bBox = true;

                //set text to info panel
                ((MainWindow)Application.Current.MainWindow).tbFileSpecific2.Text = "Bounding Box will be used.";

                //set bb north to json
                MainWindow.jSettings.bbNorth = Convert.ToDouble(tbBBNorth.Text);

                //set bb north to json
                MainWindow.jSettings.bbEast = Convert.ToDouble(tbBBEast.Text);

                //set bb north to json
                MainWindow.jSettings.bbSouth = Convert.ToDouble(tbBBSouth.Text);
                
                //set bb north to json
                MainWindow.jSettings.bbWest = Convert.ToDouble(tbBBWest.Text);
            }
            else
            {
                //set text to info panel
                ((MainWindow)Application.Current.MainWindow).tbFileSpecific2.Text = "Bounding Box will not be used.";

                //bounding box will not be used
                MainWindow.jSettings.bBox = false;
            }
            #endregion set json settings

            //set text to info panel
            ((MainWindow)Application.Current.MainWindow).ipFileSpecific.Text = "Grid size [m]";

            //set json settings grid size
            MainWindow.jSettings.gridSize = Convert.ToInt32(tbGridSize.Text);

            //input in info panel
            ((MainWindow)Application.Current.MainWindow).tbFileSpecific.Text = MainWindow.jSettings.gridSize.ToString();

            //set text in info panel
            ((MainWindow)Application.Current.MainWindow).ipFileSpecific2.Text = "Bounding Box";

            //error handling
            //set task (file opening) to true
            MainWindowBib.taskfileOpening = true;

            //check if all task are allready done
            MainWindowBib.readyState();

        }
       
        /// <summary>
        /// Check that all required tb fields are not empty (via booleans)
        /// </summary>
        private bool readyState()
        {
            //if all tbs checker set to true
            if (bbNorth && bbEast && bbSouth && bbWest && gridSize)
            {
                //enable process grid button
                btnProcessGrid.IsEnabled = true;
                //return
                return true;
            }
            else if (gridSize && chkGridBB.IsChecked == false)
            {
                //enable process grid button
                btnProcessGrid.IsEnabled = true;
                //return
                return true;
            }
            else
            {
                //disable process grid button
                btnProcessGrid.IsEnabled = false;
                return false;
            }
        }

        /// <summary>
        /// boolean value - to check if tb NORTH is not empty
        /// </summary>
        private bool bbNorth { get; set; }

        /// <summary>
        /// boolean value - to check if tb EAST is not empty
        /// </summary>
        private bool bbEast { get; set; }

        /// <summary>
        /// boolean value - to check if tb SOUTH is not empty
        /// </summary>
        private bool bbSouth { get; set; }

        /// <summary>
        /// boolean value - to check if tb WEST is not empty
        /// </summary>
        private bool bbWest { get; set; }

        /// <summary>
        /// boolean value - to check if tb GRID SIZE is not empty
        /// </summary>
        private bool gridSize { get; set; }

        #region textbox changes to check if all four values are set
        /// <summary>
        /// as soon as a text field is changed, this function is called up
        /// </summary>
        private void tbBB_TextChanged(object sender, TextChangedEventArgs e)
        {
            //check if tb north is not empty
            if (!string.IsNullOrEmpty(tbBBNorth.Text))
            {
                //set to true
                bbNorth = true;
            }
            //if tb north is empty
            else
            {
                //set to FALSE
                bbNorth = false;
            }

            //check if tb east is not empty
            if (!string.IsNullOrEmpty(tbBBEast.Text))
            {
                //set to true
                bbEast = true;
            }
            //if tb east is empty
            else
            {
                //set to FALSE
                bbEast = false;
            }

            //check if tb south is not empty
            if (!string.IsNullOrEmpty(tbBBSouth.Text))
            {
                //set to true
                bbSouth = true;
            }
            //if tb south is empty
            else
            {
                //set to FALSE
                bbSouth = false;
            }


            //check if tb west is not empty
            if (!string.IsNullOrEmpty(tbBBWest.Text))
            {
                //set to true
                bbWest = true;
            }
            //if tb west is empty
            else
            {
                //set to FALSE
                bbWest = false;
            }

            //check if GRID SIZE is not empty
            if (!string.IsNullOrEmpty(tbGridSize.Text))
            {
                //set to true
                gridSize = true;
            }
            //if tb west is empty
            else
            {
                //set to FALSE
                gridSize = false;
            }

            //check if all fields are not empty
            readyState();
        }
        #endregion textbox changes to check if all four values are set
    }
}
