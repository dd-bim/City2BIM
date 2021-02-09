using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMGISInteropLibs.GEOgraf
{
    public class OTriangle
    {
        #region Documentation
        //Link: https://vps2.hhk.de/geograf/Hilfe/GEOgraf/Anhang/Formatdokumentation/GRAFBAT_Format/GRAFBAT_Format.htm#DGM
        #endregion

        #region Constructor
        public OTriangle(int ElNum, OHorizon Hor, OPoint PNr1, OPoint PNr2, OPoint PNr3, int? ElNumN1, int? ElNumN2, int? ElNumN3, bool B1, bool B2, bool B3)
        {
            this.ElNum = ElNum;
            this.Hor = Hor;
            this.PNr1 = PNr1;
            this.PNr2 = PNr2;
            this.PNr3 = PNr3;
            this.ElNumN1 = ElNumN1;
            this.ElNumN2 = ElNumN2;
            this.ElNumN3 = ElNumN3;
            this.B1 = B1;
            this.B2 = B2;
            this.B3 = B3;
        }
        #endregion

        #region Properties
        //Elementnummer des Dreiecks
        public int ElNum { get; set; }
        //Horizont - Zuweisung eines Horizonts
        public OHorizon Hor { get; set; }
        //Punktnummer des ersten Punkt im Dreieck
        public OPoint PNr1 { get; set; }
        //Punktnummer des zweiten Punktes im Dreieck
        public OPoint PNr2 { get; set; }
        //Punktnummer des dritten Punktes im Dreieck
        public OPoint PNr3 { get; set; }
        //Elementnummer des 1. Nachbardreiecks
        public int? ElNumN1 { get; set; }
        //Elementnummer des 2. Nachbardreiecks
        public int? ElNumN2 { get; set; }
        //Elementnummer des 3. Nachbardreiecks
        public int? ElNumN3 { get; set; }
        //Bruchkante zwischen PNr1 & PNr2
        public bool B1 { get; set; }
        //Bruchkante zwischen PNr2 & PNr3
        public bool B2 { get; set; }
        //Bruchkante zwischen PNr3 & PNr1
        public bool B3 { get; set; }
        #endregion
    }
}
