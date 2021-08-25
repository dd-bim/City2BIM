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
            LogWriter.Add(LogType.verbose, "[REB] start reading.");
            var rebResult = ReadReb(config);

            if (rebResult.triMap.Count != 0)
            {
                return rebResult;
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

            LogWriter.Add(LogType.debug, "[REB] set scaling to: " + scale);

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
                                    //add point to point list
                                    coordinates.Add(new CoordinateZ(x * scale, y * scale, z * scale));
                                    points.Add(new Point(x * scale, y * scale, z * scale));

                                    LogWriter.Add(LogType.verbose, "[REB] add coordinate(X: " + x*scale + "; Y:" + y*scale + "; Z: "+ z*scale+")");
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
                                    throw new NotImplementedException();
                                }
                            }
                            //TIN work out via "identifier 58"
                            else if (line.Length >= 50
                                && line[0] == '5'
                                && line[1] == '8')
                            {
                                if (
                                   long.TryParse(line.Substring(20, 10), out long p1)
                                   && long.TryParse(line.Substring(30, 10), out long p2)
                                   && long.TryParse(line.Substring(40, 10), out long p3))
                                {
                                    //add triangle map
                                    triangleMap.Add(new Triangulator.triangleMap()
                                    {
                                        triNumber = triangleMap.Count,
                                        triValues = new int[] { (int)p1 - 1, (int)p2 - 1, (int)p3 - 1 }
                                    });

                                    LogWriter.Add(LogType.verbose, "[REB] triangle index set");
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                LogWriter.Add(LogType.error, "[REB] error: " + ex.Message);
                return null;
            }

            //set conversion type
            rebResult.currentConversion = DtmConversionType.conversion;

            //set point coordinates to result
            rebResult.coordinateList = coordinates;
            rebResult.pointList = points;
            LogWriter.Add(LogType.info, "[REB] readed points: " + coordinates.Count);

            //set tri map
            rebResult.triMap = triangleMap;
            LogWriter.Add(LogType.info, "[REB] readed triangels: " + triangleMap.Count);

            //Return of a REB data set
            return rebResult;
        }

        public static HashSet<int> readHorizon(string filePath)
        {
            HashSet<int> horizons = new HashSet<int>();

            if (File.Exists(filePath))
            {
                using (var sr = new StreamReader(filePath))
                {
                    //String to buffer a line
                    string line;

                    //Line by line reader
                    while ((line = sr.ReadLine()) != null)
                    {
                        if(line.Length >= 50
                             && line[0] == '5'
                             && line[1] == '8')
                        {
                            int.TryParse(line.Substring(7, 2), out int hz);
                            if (!horizons.Contains(hz))
                            {
                                horizons.Add(hz);
                            }
                        }

                    }
                }
                return horizons;
            }
            else
            {
                LogWriter.Entries.Add(new LogPair(LogType.error, "[REB] file can not be processed"));
                return null;
            }
        }
    }
}
