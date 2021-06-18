﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows; //used to communicate with main window
using System.Windows.Controls; //TextBox

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
}