using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMGISInteropLibs.GEOgraf
{
    public class OHorizon
    {
        //Dokumentation zum GRAFBAT - Format
        #region Documenation
        //Link: https://vps2.hhk.de/geograf/Hilfe/GEOgraf/Anhang/Formatdokumentation/GRAFBAT_Format/GRAFBAT_Format.htm#Horizontdefinition
        #endregion

        //Konstruktor
        #region Constructor
        public OHorizon(int HorNr, string Designation, bool is3D)
        {
            this.HorNr = HorNr;
            this.Designation = Designation;
            this.is3D = is3D;
        }
        #endregion

        //Eigenschaften
        #region Properties
        //Horizonnummer
        public int HorNr { get; set; }
        //Beschreibung des Horizonts
        public string Designation { get; set; }
        //Boolscher Wert - true = 3D-Horizont; false = 2D-Horizont
        public bool is3D { get; set; }
        #endregion
    }
}
