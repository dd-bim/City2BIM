using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMGISInteropLibs.GEOgraf
{
    public class OPoint
    {
        //Dokumentation
        #region Documentation
        //Link: https://vps2.hhk.de/geograf/Hilfe/GEOgraf/Anhang/Formatdokumentation/GRAFBAT_Format/GRAFBAT_Format.htm#Punkte
        #endregion

        //Konstruktor
        #region Constructor
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

        //Vergabe einer Enumeration "Status" - mögliche Status eines Punktes sind somit definiert (übersetzt auf Englisch)
        public enum Status : int
        {
            invalid = 0,    //ungültig
            digitised = 1,  //digialisiert
            measured = 2,   //gemessen
            fix = 3         //fest
        }
        #endregion

        //Methoden
        #region Method

        //Methode zum Abfragen, ob ein Punkt mit der gleichen Punktnummer exsistiert
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
