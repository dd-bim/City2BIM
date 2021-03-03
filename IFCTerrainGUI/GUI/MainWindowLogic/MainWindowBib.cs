using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Controls; //TextBox

namespace IFCTerrainGUI.GUI.MainWindowLogic
{
    /// <summary>
    /// 
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
        
        public enum indispensableTasks
        {
            fileOpening,
            selectIfcVersion,
            selectIfcShhape,

        }

        /// <summary>
        /// Used to enable or disable conversion process! (especially start button)
        /// </summary>
        public static bool readyState(bool taskInput)
        {
            task = taskInput;


            if(task)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool task { get; set; }
    }
}
