using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMGISInteropLibs.GEOgraf
{
    /// <summary>
    /// processing lines in GRAFBAT format
    /// </summary>
    public class OLine
    {
        #region Documentation
        //Link: https://vps2.hhk.de/geograf/Hilfe/GEOgraf/Anhang/Formatdokumentation/GRAFBAT_Format/GRAFBAT_Format.htm#Linien
        #endregion

        #region Constructors
        /// <summary>
        /// Constructed structure for a line according to GRAFBAT - format in processing of a TIN (if necessary also break edges)
        /// </summary>
        /// <param name="PNr1">Starting point of a line - described by the point number</param>
        /// <param name="PNr2">End point of a line - described by the point number</param>
        /// <param name="LineType">line type - if necessary to use for filtering</param>
        public OLine(OPoint PNr1, OPoint PNr2, int LineType)
        {
            this.PNr1 = PNr1;
            this.PNr2 = PNr2;
            this.LineType = LineType;
        }
        #endregion

        #region Properties
        public OPoint PNr1 { get; set; }
        public OPoint PNr2 { get; set; }
        public int LineType { get; set; }
        #endregion
    }
}
