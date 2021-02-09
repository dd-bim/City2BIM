using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMGISInteropLibs.GEOgraf
{
    /// <summary>
    /// processing points of GRAFBAT format
    /// </summary>
    public class OPoint
    {
        //Dokumentation
        #region Documentation
        //Link: https://vps2.hhk.de/geograf/Hilfe/GEOgraf/Anhang/Formatdokumentation/GRAFBAT_Format/GRAFBAT_Format.htm#Punkte
        #endregion

        #region Constructor
        /// <summary>
        /// Constructing a point based on GRAFBAT
        /// </summary>
        /// <param name="Pnr">Point number (corresponds to the "ID)</param>
        /// <param name="PointType">Point type (can be used for filtering)</param>
        /// <param name="X">Rechtswert</param>
        /// <param name="Y">Hochwert</param>
        /// <param name="Z">Höhe</param>
        /// <param name="StatusPos">Enumeration of the point status (location) - can be used for filtering</param>
        /// <param name="StatusHeight">Enumeration of the point status (height) - can be used for filtering</param>
        public OPoint(int Pnr, int PointType, double X, double Y, double Z, int StatusPos, int StatusHeight)
        {
            this.Pnr = Pnr;
            this.PointType = PointType;
            this.X = X;
            this.Y = Y;
            this.Z = Z;
            this.StatusPos = (Status)StatusPos;
            this.StatusHeight = (Status)StatusHeight;
        }
        #endregion

        //Eigenschaften
        #region Properties
        //Punktnummer
        public int Pnr { get; set; }
        //Punktart
        public int PointType { get; set; }
        //Rechtswert
        public double X { get; set; }
        //Hochwert
        public double Y { get; set; }
        //Höhe
        public double Z { get; set; }
        //Status - Lage
        public Status StatusPos { get; set; }
        //Status - Höhe
        public Status StatusHeight { get; set; }

                /// <summary>
        /// Assignment of an enumeration "Status" - possible statuses of a point are thus defined (translated in English)
        /// </summary>
        public enum Status : int
        {
            /// <summary>
            /// ungültig
            /// </summary>
            invalid = 0, 
            /// <summary>
            /// digitalisiert
            /// </summary>
            digitised = 1,
            /// <summary>
            /// gemessen
            /// </summary>
            measured = 2,   
            /// <summary>
            /// fest
            /// </summary>
            fix = 3      
        }
        #endregion

        //Methoden
        #region Method

        /// <summary>
        /// Method to query if a point with the same point number exists
        /// </summary>
        /// <param name="other">Point number of the point to be compared</param>
        /// <returns></returns>
        public bool Equals(OPoint other)
        {
            if (this.Pnr == other.Pnr)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion
    };
}
