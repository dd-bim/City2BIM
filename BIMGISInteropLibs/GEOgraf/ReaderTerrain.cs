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
using BimGisCad.Representation.Geometry.Elementary;

//Transfer class for the reader (IFCTerrain + Revit)
using BIMGISInteropLibs.IfcTerrain;

//Logging
using BIMGISInteropLibs.Logging;
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

namespace BIMGISInteropLibs.GEOgraf
{
    /// <summary>
    /// Processing GEOgraf OUT (based on meshing)
    /// </summary>
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
        public static Dictionary<int, Line3> breakLineList = new Dictionary<int, Line3>();
        #endregion

        /// <summary>
        /// Class for reading out the mesh (via points and triangles)
        /// </summary>
        /// <param name="filePath">Location of the GEOgraf OUT - file</param>
        /// <param name="pointIndex2NumberMap"></param>
        /// <param name="triangleIndex2NumberMap"></param>
        /// <returns>TIN - for processing in IFCTerrain or Revit</returns>
        public static Result ReadOutData(Config jSettings, out IReadOnlyDictionary<int, int> pointIndex2NumberMap, out IReadOnlyDictionary<int, int> triangleIndex2NumberMap)
        {
            //read from json settings
            string filePath = jSettings.filePath;

            //logging
            LogWriter.Add(LogType.verbose, "[Grafbat] start reading.");

            //Result define so that TIN can be passed
            Result result = new Result();

            //Create a new builder for TIN            
            var tinB = Tin.CreateBuilder(true);
            LogWriter.Add(LogType.verbose, "[Grafbat] Tin builder created.");

            //init transfer classes
            pointIndex2NumberMap = null;
            triangleIndex2NumberMap = null;
            /*
            //breakline
            bool breakline = jSettings.breakline;
            int bl_layer = int.Parse(jSettings.breakline_layer);

            //
            double scale = 1.0; //TODO dynamic*/

            //check if file exsists 
            if (File.Exists(filePath))
            {
                ReadDataViaTriangle(tinB, filePath);
            }
            //if file path isn't valid
            else
            {
                //error handling??? result.error???

                //error logging
                LogWriter.Add(LogType.error, "[Grafbat] File path (" + filePath + ") could not be read!");
            }

            //handover tin from tin builder
            LogWriter.Add(LogType.verbose, "[Grafbat] Create TIN via TIN builder.");
            Tin tin = tinB.ToTin(out pointIndex2NumberMap, out triangleIndex2NumberMap);

            //Result describe
            result.Tin = tin;

            //add to results (stats)
            //result.rPoints = tin.Points.Count;
            //result.rFaces = tin.NumTriangles;

            //logging
            LogWriter.Add(LogType.info, "Reading Grafbat data successful.");
            LogWriter.Add(LogType.debug, "Points: " + result.Tin.Points.Count + "; Triangles: " + result.Tin.NumTriangles + " processed");

            return result;
        }


        /// <summary>
        /// reads out data via points & triangles
        /// </summary>
        private static void ReadDataViaTriangle(Tin.Builder tinBuilder, string filePath)
        {
            //pass through each line of the file
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
                    //Add point to TIN builder, here only PNR + coordinates are needed
                    tinBuilder.AddPoint(pnr, x, y, z);

                    //logging
                    LogWriter.Add(LogType.verbose, "[Grafbat] Point (" + (pnr) + ") set (x= " + x + "; y= " + y + "; z= " + z + ")");

                    //Create point
                    OPoint point = new OPoint(pnr, pointtype, x, y, z, statPos, statHeight);
                    //Insert point (Value) via Key (Point number) into point list
                    pointList[pnr] = point;
                }

                //Read horizon
                
                if (line.StartsWith("HNR") && values.Length > 13
                    && int.TryParse(values[0].Substring(values[0].IndexOf(':') + 1, 2), out int hornr))
                {
                    //Query whether 2D (false) or 3D (true)
                    bool is3D = values[4].Equals("1") ? true : false;

                    //Form horizon
                    OHorizon horizon = new OHorizon(hornr, values[3].ToString(), is3D); //NOTE: Encoding of ANSI not considered!

                    //Add horizon to the horizon list
                    horList[hornr] = horizon;
                }

                //Read triangles
                if (line.StartsWith("DG") && values.Length > 9
                    && int.TryParse(values[0].Substring(2, values[0].IndexOf(':') - 2), out int tn)
                    && int.TryParse(values[0].Substring(values[0].IndexOf(':') + 1, 2), out int hnr)
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

                    //Add triangle to TIN builder
                    tinBuilder.AddTriangle(tn, va, vb, vc, true);
                    LogWriter.Add(LogType.verbose, "[Grafbat] Triangle (" + tn + ") set (P1= " + (va) + "; P2= " + (vb) + "; P3= " + (vc) + ")");

                    OTriangle triangle = new OTriangle(tn, horList[hnr], pointList[va], pointList[vb], pointList[vc], na, nb, nc, ea, eb, ec);
                    triList[tn] = triangle;
                }
            }
        }
    }
}
