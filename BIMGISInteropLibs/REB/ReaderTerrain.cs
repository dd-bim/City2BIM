using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//embed for file handling
using System.IO;

//BimGisCad embed
using BimGisCad.Representation.Geometry.Elementary;     //Points, Lines, ... 
using BimGisCad.Representation.Geometry.Composed;       //TIN

//Transfer class (Result) for the reader (IFCTerrain + Revit)
using BIMGISInteropLibs.IfcTerrain;

//shortcut for tin building class
using terrain = BIMGISInteropLibs.Geometry.terrain;

//Logging
using BIMGISInteropLibs.Logging;
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages
using BIMGISInteropLibs.NTSApi;

namespace BIMGISInteropLibs.REB
{
    /// <summary>
    /// REB line consisting of starting point (P1) and end point (P2)
    /// </summary>
    public struct RLine
    {
        public long P1;
        public long P2;
    }

    /// <summary>
    /// REB triangle consisting of three points
    /// </summary>
    public struct RTri
    {
        public long P1;
        public long P2;
        public long P3;
    }
    /// <summary>
    /// Class to build the REB data
    /// </summary>
    public class RebDaData
    {
        public Dictionary<int, double[]> Points { get; }
        public Dictionary<int, List<RLine>> Lines { get; }
        public Dictionary<int, List<RTri>> Tris { get; }

        /// <summary>
        /// Container for caching points, lines and triangles
        /// </summary>
        public RebDaData()
        {
            this.Points = new Dictionary<int, double[]>(); //Basically a point list with an index and a double array containing the point coordinates
            this.Lines = new Dictionary<int, List<RLine>>(); //A list of lines. Each line has a horizon identifier and consists of two pointers (point indices) referencing the point list.
            this.Tris = new Dictionary<int, List<RTri>>(); //A list of triangles. Each triangle has a horizon identifier and consists of three pointers (point indices) referencing the point list.
        }

        /// <summary>
        /// Method to query the horizons
        /// </summary>
        /// <returns></returns>
        public int[] GetHorizons()
        {
            var hs = new SortedSet<int>();
            foreach (int h in this.Lines.Keys)
            {
                hs.Add(h);
            }
            foreach (int h in this.Tris.Keys)
            {
                hs.Add(h);
            }
            return hs.ToArray();
        }
    }

    /// <summary>
    /// Class to implement different readers if necessary
    /// </summary>
    public static class ReaderTerrain
    {
        /// <summary>
        /// Reading REB data via one specific horizon
        /// </summary>
        /// <param name="fileNames">Location of the REB data set</param>
        /// <returns>RebDaData - Container (must still be converted to a TIN)</returns>
        public static RebDaData ReadReb(string fileName)
        {
            //Create instance of the container
            var rebData = new RebDaData();
            //[TODO] add an error message if file cannot be processed
            try
            {
                if (File.Exists(fileName))
                {

                    using (var sr = new StreamReader(fileName))
                    {
                        //String to buffer a line
                        string line;

                        bool read = true; //[TODO]: check if this boolean value makes sense!

                        //Line by line reader
                        while (read && (line = sr.ReadLine()) != null)
                        {
                            //Coordinates work out via "identifier 45"
                            if (line.Length >= 40
                                && line[0] == '4'
                                && line[1] == '5')
                            {
                                if (int.TryParse(line.Substring(2, 7), out int nr)
                                   && !rebData.Points.ContainsKey(nr)
                                   && long.TryParse(line.Substring(10, 10), out long x)
                                   && long.TryParse(line.Substring(20, 10), out long y)
                                   && long.TryParse(line.Substring(30, 10), out long z)
                                   )
                                {
                                    double[] pointCoords = new double[] { x * 0.001, y * 0.001, z * 0.001 };
                                    rebData.Points.Add(nr, pointCoords);
                                }
                            }
                            //Break/edge lines work out via "identifier 49"
                            else if (line.Length >= 30
                                && line[0] == '4'
                                && line[1] == '9')
                            {
                                if (int.TryParse(line.Substring(7, 2), out int hz)
                                   && long.TryParse(line.Substring(10, 10), out long p1)
                                   && long.TryParse(line.Substring(20, 10), out long p2))
                                {
                                    if (rebData.Lines.TryGetValue(hz, out var ls))
                                    {
                                        ls = new List<RLine>();
                                        rebData.Lines.Add(hz, ls);
                                    }
                                    ls.Add(new RLine { P1 = p1, P2 = p2 });
                                }
                            }
                            //TIN work out via "identifier 58"
                            else if (line.Length >= 50
                                && line[0] == '5'
                                && line[1] == '8')
                            {
                                if (int.TryParse(line.Substring(7, 2), out int hz)
                                   && long.TryParse(line.Substring(20, 10), out long p1)
                                   && long.TryParse(line.Substring(30, 10), out long p2)
                                   && long.TryParse(line.Substring(40, 10), out long p3))
                                {
                                    if (!rebData.Tris.TryGetValue(hz, out var ts))
                                    {
                                        ts = new List<RTri>();
                                        rebData.Tris.Add(hz, ts);
                                    }
                                    ts.Add(new RTri { P1 = p1, P2 = p2, P3 = p3 });
                                }
                            }
                        }
                    }
                }

            }
            catch
            {
                return null;
            }
            //Return of a REB data set
            return rebData;
        }

        /// <summary>
        /// Conversion of a REB data set to a TIN
        /// </summary>
        /// <param name="rebData">REB data set</param>
        /// <param name="horizon">Horizon to be processed</param>
        /// <returns>TIN via Result for processing in IFCTerrain (and Revit)</returns>
        public static Result ConvertRebToTin(RebDaData rebData, JsonSettings jSettings)
        {
            //read horizon from json settings
            int horizon = jSettings.horizon;

            //var mesh = new Mesh(is3D, minDist); remove
            var tinB = Tin.CreateBuilder(true);
            LogWriter.Add(LogType.verbose, "[REB] Create TIN builder");

            //Container for tin
            var result = new Result();

            //create point map check whether revision makes sense
            var pmap = new Dictionary<long, int>();

            //create artificial point index
            int points = 0;

            //Add all points of the vine dataset to the TIN builder & point map
            foreach (var kv in rebData.Points)
            {
                Point3 p = Point3.Create(kv.Value[0], kv.Value[1], kv.Value[2]);
                pmap.Add(kv.Key, points); //point map
                tinB.AddPoint(points++, p); //add tin builder
                LogWriter.Add(LogType.verbose, "[REB] Point (" + (points - 1) + ") set (x= " + p.X + "; y= " + p.Y + "; z= " + p.Z + ")");
            }
            //reb triangle
            if (rebData.Tris.TryGetValue(horizon, out var tris))
            {
                //traverse each triangle in the REB data set
                foreach (var tri in tris)
                {
                    //Determine indices of the triangle
                    if (pmap.TryGetValue(tri.P1, out int v1)
                        && pmap.TryGetValue(tri.P2, out int v2)
                        && pmap.TryGetValue(tri.P3, out int v3))
                    {
                        //Create triangle over the references to the point indices
                        tinB.AddTriangle(v1, v2, v3, true);
                        LogWriter.Add(LogType.verbose, "[REB] Triangle set (P1= " + (v1) + "; P2= " + (v2) + "; P3= " + (v3) + ")");
                    }
                }
            }

            //Generate TIN from TIN Builder
            Tin tin = tinB.ToTin(out var pointIndex2NumberMap, out var triangleIndex2NumberMap);
            LogWriter.Add(LogType.verbose, "[REB] Create TIN via TIN builder.");

            //Add TIN to the Result
            result.Tin = tin;

            //logging
            LogWriter.Add(LogType.info, "Reading REB data successful.");
            LogWriter.Add(LogType.debug, "Points: " + result.Tin.Points.Count + "; Triangles: " + result.Tin.NumTriangles + " processed");

            //add to results (to gui logging)
            result.rPoints = tin.Points.Count;
            result.rFaces = tin.NumTriangles;

            return result;
        }

        public static Result CalculateTinFromReb(RebDaData rebData, JsonSettings jSettings)
        {
            //Initialize result
            Result result = new Result();

            //Initialize TIN builder
            var tinBuilder = Tin.CreateBuilder(true);

            //Log TIN builder initalization
            LogWriter.Add(LogType.verbose, "[REB] Initialize a TIN builder.");

            //Prepare DTM data REB file. If successful than process data and create TIN
            if (PrepareRebData(rebData, jSettings, out List<double[]> dtmPointData, out List<List<double[]>> dtmLineData))
            {
                //Get a list of triangles via NetTopologySuite class library using the interface object
                List<List<double[]>> dtmTriangleList = new NtsApi().MakeTriangleList(dtmPointData, dtmLineData);

                //init hash set (for unquie points)
                var uptList = new HashSet<Geometry.uPoint3>();

                foreach (List<double[]> dtmTriangle in dtmTriangleList)
                {
                    //Read out the three vertices of one triangle at each loop
                    Point3 p1 = Point3.Create(dtmTriangle[0][0], dtmTriangle[0][1], dtmTriangle[0][2]);
                    Point3 p2 = Point3.Create(dtmTriangle[1][0], dtmTriangle[1][1], dtmTriangle[1][2]);
                    Point3 p3 = Point3.Create(dtmTriangle[2][0], dtmTriangle[2][1], dtmTriangle[2][2]);

                    //add points to list [note: logging will be done in support function]
                    int pnrP1 = terrain.addToList(uptList, p1);
                    int pnrP2 = terrain.addToList(uptList, p2);
                    int pnrP3 = terrain.addToList(uptList, p3);

                    //add triangle via point numbers above
                    tinBuilder.AddTriangle(pnrP1, pnrP2, pnrP3);

                    //log
                    LogWriter.Add(LogType.verbose, "[REB] Triangle [" + pnrP1 + "; " + pnrP2 + "; " + pnrP3 + "] set.");
                }

                //loop through point list 
                foreach (Geometry.uPoint3 pt in uptList)
                {
                    tinBuilder.AddPoint(pt.pnr, pt.point3);
                }
            }

            //Build a TIN via BimGisCad class library and log
            Tin tin = tinBuilder.ToTin(out var pointIndex2NumberMap, out var triangleIndex2NumberMap);
            LogWriter.Add(LogType.verbose, "[REB] Creating TIN via TIN builder.");

            //Pass TIN to result and log
            result.Tin = tin;
            LogWriter.Add(LogType.info, "Reading REB data successful.");
            result.rPoints = tin.Points.Count;
            result.rFaces = tin.NumTriangles;
            LogWriter.Add(LogType.debug, "Points: " + result.Tin.Points.Count + "; Triangles: " + result.Tin.NumTriangles + " processed");

            //Return the result as a TIN
            return result;
        }

        public static bool PrepareRebData(RebDaData rebData, JsonSettings jSettings, out List<double[]> dtmPointData, out List<List<double[]>> dtmLineData)
        {
            dtmPointData = new List<double[]>();
            dtmLineData = new List<List<double[]>>();
            int horizon = jSettings.horizon;

            foreach (var point in rebData.Points)
            {
                dtmPointData.Add(point.Value);
            }

            if (rebData.Lines.TryGetValue(horizon, out var lines))
            {
                foreach (var line in lines)
                {
                    List<double[]> lineDataList = new List<double[]>();
                    while (lineDataList.Count < 2)
                    {
                        foreach (var point in rebData.Points)
                        {
                            if (point.Key == line.P1)
                            {
                                lineDataList.Add(point.Value);
                            }
                            if (point.Key == line.P2)
                            {
                                lineDataList.Add(point.Value);
                            }
                        }
                    }
                    dtmLineData.Add(lineDataList);
                }
            }
            return CheckDtmDataLists(dtmPointData, dtmLineData);
        }

        /// <summary>
        /// A auxiliary function to check if lists contain data. 
        /// </summary>
        /// <param name="dtmPointData">A list of double arrays. Each array contains the x, y and z coordinate of a DTM point.</param>
        /// <param name="dtmLineData">A list of lists of double arrays. Each array contains the x, y and z coordinate of a DTM point.</param>
        /// <returns>True or false.</returns>
        public static bool CheckDtmDataLists(List<double[]> dtmPointData, List<List<double[]>> dtmLineData)
        {
            if (dtmPointData.Count == 0)
            {
                LogWriter.Add(LogType.error, "[REB] No point data found.");
                return false;
            }
            else if (dtmLineData.Count == 0)
            {
                LogWriter.Add(LogType.warning, "[REB] Reading point data was successful. No line data found.");
                return true;
            }
            else
            {
                LogWriter.Add(LogType.info, "[REB] Reading point and line data was successful.");
                return true;
            }
        }
    }
}
