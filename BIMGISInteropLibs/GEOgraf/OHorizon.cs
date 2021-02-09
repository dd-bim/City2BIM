using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMGISInteropLibs.GEOgraf
{
    /// <summary>
    /// Horizon - class for processing in GEOgraf OUT
    /// </summary>
    public class OHorizon
    {
        //Documentation for GRAFBAT - Format
        #region Documenation
        //Link: https://vps2.hhk.de/geograf/Hilfe/GEOgraf/Anhang/Formatdokumentation/GRAFBAT_Format/GRAFBAT_Format.htm#Horizontdefinition
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor for horizon
        /// </summary>
        /// <param name="HorNr">Horizon number (equals ID)</param>
        /// <param name="Designation">Description of the horizon, insofar as included</param>
        /// <param name="is3D">boolean value whether it is a 3D horizon (true) or 2D horizon (false)</param>
        public OHorizon(int HorNr, string Designation, bool is3D)
        {
            this.HorNr = HorNr;
            this.Designation = Designation;
            this.is3D = is3D;
        }
        #endregion

        #region Properties
        //Horizon Number
        public int HorNr { get; set; }
        //Description of the horizon
        public string Designation { get; set; }
        //Boolean value - true = 3D horizon; false = 2D horizon
        public bool is3D { get; set; }
        #endregion
    }
}
