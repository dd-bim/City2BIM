using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows; //used to communicate with main window
using System.Windows.Controls; //TextBox
using System.Windows.Data;
using System.Windows.Input;

using GuiHandler.userControler;
//shortcut to set json settings
using init = GuiHandler.InitClass;
namespace GuiHandler
{
    public class GuiSupport
    {

        /// <summary>
        /// Check that all required tasks have been performed
        /// </summary>
        public static bool readyState()
        {
            //if all tasks are true
            if (taskfileOpening && selectIfcShape && selectIfcVersion && selectMetadata && selectStoreLocation && selectGeoRef)
            {
                //return
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// DTM2BIM task checker
        /// </summary>
        public static bool rdyDTM2BIM()
        {
            if (taskfileOpening && selectProcessing)
            {
                //TODO ENABLE START BUTTON FUNCTION!!!!!!!!!!!
                return true;
            }
            else
            {
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

        /// <summary>
        ///[DTM2BIM only] check if processing has been set
        /// </summary>
        public static bool selectProcessing { get; set; }

        /// <summary>
        /// support function to reset tasks
        /// </summary>
        public static void resetTasks()
        {
            selectProcessing = false;
            selectGeoRef = false;
            selectStoreLocation = false;
            selectMetadata = false;
            selectIfcVersion = false;
            selectIfcShape = false;
            taskfileOpening = false;
        }


        /// <summary>
        /// method to set gui messages
        /// </summary>
        /// <param name="message"></param>
        public static void setLog(string message)
        {
            UILog.LogMessages.Add(message);
        }

        /// <summary>
        /// method to clear log (needed in DTM2BIM)
        /// </summary>
        public static void clearLog()
        {
            UILog.LogMessages.Clear();
        }


        /// <summary>
        /// unterstützende FUnktion um das info panel zu füllen
        /// [TODO]: clean up code (this solution is a very bad databinding example)
        /// </summary>
        public static void fileReaded()
        {
            InformationPanel.info.LastOrDefault().fileName = init.config.fileName;
            InformationPanel.info.LastOrDefault().fileType = init.config.fileType.ToString();
            InformationPanel.info.LastOrDefault().breakline = init.config.breakline.GetValueOrDefault();
            if (init.config.horizon.HasValue)
            {
                InformationPanel.info.LastOrDefault().breaklineLayer = init.config.horizon.ToString();
            }
            else if (!string.IsNullOrEmpty(init.config.breakline_layer))
            {
                InformationPanel.info.LastOrDefault().breaklineLayer = init.config.breakline_layer;
            }
            else
            {
                InformationPanel.info.LastOrDefault().breaklineLayer = null;
            }

            if (init.config.readPoints.GetValueOrDefault())
            {
                InformationPanel.info.LastOrDefault().points = init.config.readPoints.GetValueOrDefault();
                InformationPanel.info.LastOrDefault().faces = false;
            }
            else
            {
                InformationPanel.info.LastOrDefault().points = false;
                InformationPanel.info.LastOrDefault().faces = true;
            }
        }
    }
}
