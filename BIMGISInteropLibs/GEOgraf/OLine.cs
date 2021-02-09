using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMGISInteropLibs.GEOgraf
{
    public class OLine
    {
        #region Documentation
        //Link: https://vps2.hhk.de/geograf/Hilfe/GEOgraf/Anhang/Formatdokumentation/GRAFBAT_Format/GRAFBAT_Format.htm#Linien
        #endregion

        #region Constructors
        public OLine(OPoint PNr1, OPoint PNr2, int LineType)
        {
            this.PNr1 = PNr1;
            this.PNr2 = PNr2;
            this.LineType = LineType;
        }
        #endregion

        #region Properties
        //Startpoint
        public OPoint PNr1 { get; set; }
        //Endpoint
        public OPoint PNr2 { get; set; }
        //Linetype - Linienart - ggf. für die Nutzung zur Filterung
        public int LineType { get; set; }
        #endregion
    }
}
