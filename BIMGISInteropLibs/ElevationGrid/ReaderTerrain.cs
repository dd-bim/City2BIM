using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//CultureInfo usw.
using System.Globalization;

//Transfer class for the reader (IFCTerrain + Revit)
using BIMGISInteropLibs.IfcTerrain;

//Logging
using BIMGISInteropLibs.Logging;
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

using NetTopologySuite.Geometries;

namespace BIMGISInteropLibs.ElevationGrid
{
    class ReaderTerrain
    {
        /// <summary>
        /// Reads out a grid file
        /// </summary>
        /// <returns>TIN or MESH for processing in IFCTerrain (and Revit)</returns>
        public static Result readGrid(Config config)
        {
            if (readPointData(config, out Result result))
            {
                //return result class
                return result;
            }
            else
            {
                //do not store any results --> processing will be canceld!
                return null;
            }
        }

       
        /// <summary>
        /// read grid data 
        /// </summary>
        /// <param name="config">setting to read grid file (may contains bounding box settings)</param>
        /// <param name="gridResult">return value</param>
        /// <returns>result class for further processing</returns>
        public static bool readPointData(Config config, out Result gridResult)
        {
            //Log successful reading
            LogWriter.Add(LogType.info, "[Grid] reading grid file (" + config.fileName + ")");

            //init result
            gridResult = new Result();

            //Initialize list for DTM point data
            var pointList = new List<Point>();

            //set conversion type
            gridResult.currentConversion = DtmConversionType.points;
            
            try
            {
                //A single line of the input file
                string line;

                //Create a file reader to trim points based on bounding box 
                System.IO.StreamReader file = new System.IO.StreamReader(config.filePath);

                switch (config.bBox.GetValueOrDefault())
                {
                    //case use of bounding box
                    case true:
                        while ((line = file.ReadLine()) != null)
                        {
                            //splite line data
                            string[] str = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            //if line contains values (parse them to the specific values)
                            if (str.Length > 2
                                && double.TryParse(str[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double x)
                                && double.TryParse(str[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y)
                                && double.TryParse(str[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double z))
                            {
                                //Filter coordinates by min and max values of the bounding box (x & y)
                                if (y >= config.bbWest && y <= config.bbEast
                                    && x >= config.bbNorth && x <= config.bbSouth)
                                {
                                    //Prepare Point Data for NetTopologySuite
                                    pointList.Add(new Point(x, y, z));
                                }
                            }
                        }
                        break;

                    //case without bounding box
                    case false:
                        while ((line = file.ReadLine()) != null)
                        {
                            //splite line data
                            string[] str = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            //if line contains values (parse them to the specific values)
                            if (str.Length > 2
                                && double.TryParse(str[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double x)
                                && double.TryParse(str[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y)
                                && double.TryParse(str[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double z))
                            {
                                //Prepare Point Data for NetTopologySuite
                                pointList.Add(new Point(x, y, z));
                            }
                        }
                        break;
                }
                //Log num of readed points
                LogWriter.Add(LogType.debug, "[Grid] - readed points: " + pointList.Count);

                //Close file reader
                file.Close();
                if (pointList != null)
                {
                    gridResult.pointList = pointList;

                    //Return true in case reading the input file was successful
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                //Log failed reading
                LogWriter.Add(LogType.error, "XYZ file could not be read (" + config.fileName + ")");

                //write to console
                Console.WriteLine("XYZ file could not be read: " + Environment.NewLine + e.Message);

                //Return false in case reading the input file failed
                return false;
            }
        }
    }
}
