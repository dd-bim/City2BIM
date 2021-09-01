using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//NumberStyles + CultureInfo
using System.Globalization;

//File handling
using System.IO;

//Transfer class for the reader (IFCTerrain + Revit)
using BIMGISInteropLibs.IfcTerrain;

//Logging
using BIMGISInteropLibs.Logging;
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

using NetTopologySuite.Geometries;

namespace BIMGISInteropLibs.GEOgraf
{
    /// <summary>
    /// Processing GEOgraf OUT (based on meshing)
    /// </summary>
    public static class ReadOUT
    {
        /// <summary>
        /// Class for reading out data
        /// </summary>
        public static Result readOutData(Config config)
        {
            //init new result
            Result res = new Result();

            //logging
            LogWriter.Add(LogType.verbose, "[Grafbat] start file reading...");

            //check if file exsists 
            if (File.Exists(config.filePath))
            {
                //read faces (& breaklines)
                if (!readFaces(config, res))
                {
                    return null;
                }
            }
            //if file path isn't valid
            else
            {
                //error logging
                LogWriter.Add(LogType.error, "[Grafbat] File path (" + config.filePath + ") could not be read!");
                return null;
            }

            //logging
            LogWriter.Add(LogType.info, "[GRAFBAT] Reading grafbat file data successful.");
            
            return res;
        }


        

        /// <summary>
        /// reads out data via points & triangles
        /// </summary>
        private static bool readFaces(Config config, Result res)
        {
            var points = new HashSet<Point>();
            var lines = new List<LineString>();
            var triMap = new HashSet<Triangulator.triangleMap>();

            string horizonFilter = string.Empty;

            //pass through each line of the file
            foreach (var line in File.ReadAllLines(config.filePath))
            {
                //get line value
                var values = line.Split(new[] { ',' });

                //read point data
                if (line.StartsWith("PK") && values.Length > 4
                    && int.TryParse(values[0].Substring(2, values[0].IndexOf(':') - 2), out int pnr)
                    //&& int.TryParse(values[1].Substring(0, values[1].IndexOf('.')+3), out int pointtype)
                    && double.TryParse(values[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double x)
                    && double.TryParse(values[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double y)
                    && double.TryParse(values[4], NumberStyles.Float, CultureInfo.InvariantCulture, out double z))
                {

                    var pT = values[1].Split('.');
                    int.TryParse(pT[1], out int pointtype);

                    if (horizonFilter.Contains(pointtype.ToString()) || !config.readPoints.GetValueOrDefault())
                    {
                        //create new point
                        Point p = new Point(x, y, z);

                        //set point number
                        p.UserData = pnr;

                        //add point to hash set
                        points.Add(p);

                        //logging
                        LogWriter.Add(LogType.verbose, "[Grafbat] Point (" + p.UserData + ") set (x= " + x + "; y= " + y + "; z= " + z + ")");

                    }
                    else
                    {
                        LogWriter.Add(LogType.verbose, "[Grafbat] Point (" + pnr + ") has not been added.");
                    }

                }

                //read breaklines
                if(config.breakline == true
                    && line.StartsWith("LI") && values.Length > 11
                    && int.TryParse(values[0].Substring(2, values[0].IndexOf(':') - 2), out int ln)
                    && int.TryParse(values[0].Substring(values[0].IndexOf(':') + 5, 5), out int ls)
                    && int.TryParse(values[1].Substring(3), out int le)
                    && int.TryParse(values[2].Split('.').GetValue(1).ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int la)
                    )
                {
                    if (config.breakline_layer.Contains(la.ToString()))
                    {
                        var pts = points.ToList();

                        Point p1 = pts.Find(p => (int)p.UserData == ls);
                        CoordinateZ c1 = new CoordinateZ(p1.X, p1.Y, p1.Z);
                        
                        Point p2 = pts.Find(p => (int)p.UserData == le);
                        CoordinateZ c2 = new CoordinateZ(p2.X, p2.Y, p2.Z);

                        Coordinate[] lineCoords = new Coordinate[] { c1, c2 };

                        LineString breakline = new LineString(lineCoords);

                        lines.Add(breakline);

                        LogWriter.Add(LogType.verbose, "[Grafbat] Breakline added");
                    }
                    else
                    {
                        LogWriter.Add(LogType.debug, "Line skipped. Not in breakline filter!");
                    }
                }

                //read faces
                if (line.StartsWith("DG") && values.Length > 9
                    && config.readPoints.GetValueOrDefault() == false
                    && config.breakline.GetValueOrDefault() == false
                    && int.TryParse(values[0].Substring(2, values[0].IndexOf(':') - 2), out int tn)
                    && int.TryParse(values[0].Substring(values[0].IndexOf(':') + 1, 3), out int hnr)
                    && int.TryParse(values[1].Substring(3), out int va)
                    && int.TryParse(values[2].Substring(3), out int vb)
                    && int.TryParse(values[3].Substring(3), out int vc))
                {
                    //parse points as list
                    var pts = points.ToList();

                    //get index in list
                    int p1 = pts.FindIndex(p => (int)p.UserData == va);
                    int p2 = pts.FindIndex(p => (int)p.UserData == vb);
                    int p3 = pts.FindIndex(p => (int)p.UserData == vc);

                    //check if horizon filtering is enabled
                    if (config.onlyHorizon.GetValueOrDefault())
                    {
                        //check if face horizon fits to filter
                        if(hnr == config.horizon)
                        {
                            //add
                            triMap.Add(new Triangulator.triangleMap()
                            {
                                triNumber = tn,
                                triValues = new int[] { p1, p2, p3 }
                            });
                            LogWriter.Add(LogType.verbose, "[Grafbat] Triangle ("+ tn + ") set - horizon filter (" + config.horizon + ")");
                        }
                        else
                        {
                            LogWriter.Add(LogType.debug, "[Grafbat] Triangle has been skipped - out of horizon filter + ("+ config.horizon +")");
                        }
                    }
                    //add all faces
                    else
                    {
                        triMap.Add(new Triangulator.triangleMap()
                        {
                            triNumber = tn,
                            triValues = new int[] { p1, p2, p3 }
                        });
                        LogWriter.Add(LogType.verbose, "[Grafbat] Triangle (" + tn + ") set");
                    }
                }
            }
            
            
            //handle result
            res.pointList = points.ToList();
            if (config.breakline.GetValueOrDefault())
            {
                if(lines.Count > 0)
                {
                    res.currentConversion = DtmConversionType.points_breaklines;
                    res.lines = lines;
                }
                else { return false; }
            }
            else if(config.readPoints.GetValueOrDefault()
                && !config.breakline.GetValueOrDefault())
            {
                res.currentConversion = DtmConversionType.points;
            }
            else
            {
                if(triMap.Count > 0)
                {
                    //set converison type
                    res.currentConversion = DtmConversionType.conversion;
                    res.triMap = triMap;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }
}
