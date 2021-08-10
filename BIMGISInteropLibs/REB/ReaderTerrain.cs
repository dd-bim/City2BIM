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

//Logging
using BIMGISInteropLibs.Logging;
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

using NetTopologySuite.Geometries;

namespace BIMGISInteropLibs.REB
{
    /// <summary>
    /// Class to implement different readers if necessary
    /// </summary>
    public static class ReaderTerrain
    {

        /*
         * ADD LOGGING
         */

        public static Result readDtm(Config config)
        {
            var rebResult = ReadReb(config);

            if (rebResult.triMap.Count != 0)
            {
                return rebResult;
            }
            else
            {
                //TODO
            }

            return null;
        }

        /// <summary>
        /// Reading REB data via one specific horizon
        /// </summary>
        public static Result ReadReb(Config config)
        {
            //init reb result for data exchange
            Result rebResult = new Result();

            //Points collection
            CoordinateList coordinates = new CoordinateList();
            List<Point> points = new List<Point>();

            //Triangle index collection
            HashSet<Triangulator.triangleMap> triangleMap = new HashSet<Triangulator.triangleMap>();

            //Breakline collection (TODO!)
            List<LineString> rebBreaklines = new List<LineString>();

            //set scaling (TODO: read dynamic from file?)
            double scale = 0.001;

            //[TODO] add an error message if file cannot be processed
            try
            {
                if (File.Exists(config.filePath))
                {
                    using (var sr = new StreamReader(config.filePath))
                    {
                        //String to buffer a line
                        string line;

                        //Line by line reader
                        while ((line = sr.ReadLine()) != null)
                        {
                            //Coordinates work out via "identifier 45"
                            if (line.Length >= 40
                                && line[0] == '4'
                                && line[1] == '5')
                            {
                                if (int.TryParse(line.Substring(2, 7), out int nr)
                                   && long.TryParse(line.Substring(10, 10), out long x)
                                   && long.TryParse(line.Substring(20, 10), out long y)
                                   && long.TryParse(line.Substring(30, 10), out long z)
                                   )
                                {
                                    /*
                                    double[] pointCoords = new double[] { x * scale, y * scale, z * scale };
                                    rebData.Points.Add(nr, pointCoords);
                                    */
                                    //add point to point list
                                    coordinates.Add(new CoordinateZ(x * scale, y * scale, z * scale));
                                    points.Add(new Point(x * scale, y * scale, z * scale));
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
                                    /*
                                    if (rebData.Lines.TryGetValue(hz, out var ls))
                                    {
                                        ls = new List<RLine>();
                                        rebData.Lines.Add(hz, ls);
                                    }
                                    ls.Add(new RLine { P1 = p1, P2 = p2 });
                                    */

                                    //TODO
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
                                    /*
                                    if (!rebData.Tris.TryGetValue(hz, out var ts))
                                    {
                                        ts = new List<RTri>();
                                        rebData.Tris.Add(hz, ts);
                                    }
                                    
                                    ts.Add(new RTri { P1 = p1, P2 = p2, P3 = p3 });
                                    */
                                    triangleMap.Add(new Triangulator.triangleMap()
                                    {
                                        triNumber = triangleMap.Count,
                                        triValues = new int[] { (int)p1 - 1, (int)p2 - 1, (int)p3 - 1 }
                                    });
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

            //set point coordinates to result
            rebResult.coordinateList = coordinates;
            rebResult.pointList = points;

            //set tri map
            rebResult.triMap = triangleMap;

            //Return of a REB data set
            return rebResult;
        }
    }
}
