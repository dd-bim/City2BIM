using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//NumberStyles + CultureInfo
using System.Globalization;

//File handling
using System.IO;

//BimGisCad - Bibliothek einbinden (TIN)
using BimGisCad.Representation.Geometry.Composed; //TIN

//Logging
//NLOG removed...

//Transfer class for the reader (IFCTerrain + Revit)
using BIMGISInteropLibs.IFCTerrain;

namespace BIMGISInteropLibs.GEOgraf
{
    public static class ReadOUT
    {
        #region DictionaryCollection - TIN
        //Speicherung aller Punkte im TIN
        public static Dictionary<int, OPoint> pointList = new Dictionary<int, OPoint>();

        //Speicherung aller Linien im TIN
        public static Dictionary<int, OLine> lineList = new Dictionary<int, OLine>();

        //Speicherung aller Horizonte im TIN
        public static Dictionary<int, OHorizon> horList = new Dictionary<int, OHorizon>();

        //Speicherung aller Dreiecke im TIN
        public static Dictionary<int, OTriangle> triList = new Dictionary<int, OTriangle>();
        #endregion

        #region DictionaryCollection - Bruchkanten
        //EMPTY
        #endregion

        //auslesen aller Punkte
        public static Result ReadOutData(string filePath, out IReadOnlyDictionary<int, int> pointIndex2NumberMap, out IReadOnlyDictionary<int, int> triangleIndex2NumberMap)
        {
            //Logger
            //Logger logger = LogManager.GetCurrentClassLogger();

            //Result definieren damit Tin übergeben werden kann
            Result result = new Result();

            //Anlegen eines neuen Builder für TIN            
            var tinB = Tin.CreateBuilder(true);

            //Übergabeklassen
            pointIndex2NumberMap = null;
            triangleIndex2NumberMap = null;

            if (File.Exists(filePath))
            {
                foreach (var line in File.ReadAllLines(filePath))
                {
                    var values = line.Split(new[] { ',' });
                    if (line.StartsWith("PK") && values.Length > 4
                        && int.TryParse(values[0].Substring(2, values[0].IndexOf(':') - 2), out int pnr)
                        && int.TryParse(values[1].Substring(0, values[1].IndexOf('.')), out int pointtype)
                        && double.TryParse(values[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double x)
                        && double.TryParse(values[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double y)
                        && double.TryParse(values[4], NumberStyles.Float, CultureInfo.InvariantCulture, out double z)
                        && int.TryParse(values[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out int statPos)
                        && int.TryParse(values[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out int statHeight))
                    {
                        //Punkt dem TIN hinzufügen, hier wird nur PNR + Koordinaten benötigt
                        tinB.AddPoint(pnr, x, y, z);
                        //Punkt erstellen
                        OPoint point = new OPoint(pnr, pointtype, x, y, z, statPos, statHeight);
                        //Punkt (Value) über Key (Punktnummer) in Punktliste einfügen
                        pointList[pnr] = point;
                    }

                    //Horizont auslesen
                    if (line.StartsWith("HNR") && values.Length > 13
                        && int.TryParse(values[0].Substring(values[0].IndexOf(':') + 1, 3), out int hornr))
                    {
                        //Abfragen, ob 2D (false) oder 3D (true)
                        bool is3D = values[4].Equals("1") ? true : false;
                        //Horizont bilden
                        OHorizon horizon = new OHorizon(hornr, values[3].ToString(), is3D); //BEMERKUNG: Encoding von ANSI nicht berücksichtigt!
                        //Horizont der Horizontliste hinzufügen
                        horList[hornr] = horizon;
                    }
                    
                    //Dreiecke auslesen
                    if (line.StartsWith("DG") && values.Length > 9
                        && int.TryParse(values[0].Substring(2, values[0].IndexOf(':') - 2), out int tn)
                        && int.TryParse(values[0].Substring(values[0].IndexOf(':') + 1, 3), out int hnr)
                        && int.TryParse(values[1].Substring(3), out int va)
                        && int.TryParse(values[2].Substring(3), out int vb)
                        && int.TryParse(values[3].Substring(3), out int vc))
                    {
                        int? na = !string.IsNullOrEmpty(values[4]) && int.TryParse(values[4], out int n) ? n : (int?)null;
                        int? nb = !string.IsNullOrEmpty(values[5]) && int.TryParse(values[5], out n) ? n : (int?)null;
                        int? nc = !string.IsNullOrEmpty(values[6]) && int.TryParse(values[6], out n) ? n : (int?)null;
                        bool ea = !string.IsNullOrEmpty(values[7]);
                        bool eb = !string.IsNullOrEmpty(values[8]);
                        bool ec = !string.IsNullOrEmpty(values[9]);

                        //Dreieck dem TIN hinzufügen
                        tinB.AddTriangle(tn, va, vb, vc, na, nb, nc, ea, eb, ec, true);
                        
                        OTriangle triangle = new OTriangle(tn, horList[hnr], pointList[va], pointList[vb], pointList[vc], na, nb, nc, ea, eb, ec);
                        triList[tn] = triangle;
                    }
                }
            }
            result.Tin = tinB.ToTin(out pointIndex2NumberMap, out triangleIndex2NumberMap);
            //logger.Info("Reading GEOgraf OUT successful");
            //logger.Info(result.Tin.Points.Count() + " points; " + result.Tin.NumTriangles + " triangels processed");
            return result;
        }
    }
}
