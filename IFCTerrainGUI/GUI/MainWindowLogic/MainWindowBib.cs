using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows; //used to communicate with main window
using System.Windows.Controls; //TextBox

namespace IFCTerrainGUI.GUI.MainWindowLogic
{
    /// <summary>
    /// Collection of small code snips
    /// </summary>
    public class MainWindowBib
    {
        /// <summary>
        /// Function to assign text to a textbox;
        /// Note: this scrolls to the end of the text box so that the end of the file path is displayed
        /// </summary>
        /// <param name="tbName">Text box identifier</param>
        /// <param name="tbText">Text which should be used</param>
        public static void setTextBoxText(TextBox tbName, string tbText)
        {
            tbName.Text = tbText;
            tbName.CaretIndex = tbText.Length;
            var rect = tbName.GetRectFromCharacterIndex(tbName.CaretIndex);
            tbName.ScrollToHorizontalOffset(rect.Right);
        }

        /// <summary>
        /// Check that all required tasks have been performed
        /// </summary>
        public static bool readyState()
        {
            //if all tasks are true
            if (taskfileOpening && selectIfcShape && selectIfcVersion && selectMetadata && selectStoreLocation && selectGeoRef)
            {
                //enable start button
                ((MainWindow)Application.Current.MainWindow).btnStart.IsEnabled = true;

                //return
                return true;
            }
            else
            {
                //disable start button
                ((MainWindow)Application.Current.MainWindow).btnStart.IsEnabled = false;
                return false;
            }
        }

        /// <summary>
        /// task to check if a file has been opened
        /// </summary>
        public static bool taskfileOpening { get; set; }

        /// <summary>
        /// task to check if an Ifc version has been selected
        /// </summary>
        public static bool selectIfcVersion { get; set; }

        /// <summary>
        /// task to check if an Ifc Shape Repres has been selected
        /// </summary>
        public static bool selectIfcShape { get; set; }

        /// <summary>
        /// task to check if metadata are selected (or not needed)
        /// </summary>
        public static bool selectMetadata { get; set; }

        /// <summary>
        /// task to check if storage location have been set
        /// </summary>
        public static bool selectStoreLocation { get; set; }

        /// <summary>
        /// task to check if georef have been set
        /// </summary>
        public static bool selectGeoRef { get; set; }

    }

    /*
     * Add gui logging here!
     * PLACEHOLDER
     * 
     */
}
