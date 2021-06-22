using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//
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
        /// Function to assign text to a textbox<para/>
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
        /// function to set gui logging messages
        /// </summary>
        /// <param name="guiText"></param>
        public static void setGuiLog(string guiText)
        {
            //set list box
            var lstBox = ((MainWindow)Application.Current.MainWindow).tbGuiLogging;
            
            //set item to list box
            lstBox.Items.Add(guiText);
        }

        /// <summary>
        /// support function to enable start button
        /// </summary>
        public static void enableStart(bool boolen)
        {
            if (boolen)
            {
                //enable start button
                ((MainWindow)Application.Current.MainWindow).btnStart.IsEnabled = true;
            }
            else
            {
                //disable start button
                ((MainWindow)Application.Current.MainWindow).btnStart.IsEnabled = false;
            }
            return;
        }
    }
}
