//Transfer class for the reader (IFCTerrain + Revit)
using BIMGISInteropLibs.IfcTerrain;

//Logging
using BIMGISInteropLibs.Logging;

//Using Npgsql - .NET Access to PostgreSQL
//Link: https://www.npgsql.org/
using Npgsql;

using System;
using System.Collections.Generic;
using System.Linq;

//embed for CultureInfo handling
using System.Globalization;
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

using NetTopologySuite.Geometries;
using BIMGISInteropLibs.Geometry;

using Npgsql.NetTopologySuite;

namespace BIMGISInteropLibs.PostGIS
{
    public class ReaderTerrain
    {
        public static Result readPostGIS(Config config)
        {
            NpgsqlConnection.GlobalTypeMapper.UseNetTopologySuite(handleOrdinates: Ordinates.XYZ);

            if (connectDB(config, out NpgsqlConnection connection))
            {
                //init new result
                var res = new Result();
                LogWriter.Add(LogType.verbose, "[PostGIS] connected to Database: " + connection.Database);

                try
                {
                    //try to open connection
                    connection.Open();
                }
                catch (Exception ex)
                {
                    LogWriter.Add(LogType.error, "[PostGIS] " + ex);
                    LogWriter.Add(LogType.error, "[PostGIS] can not open a valid connection!");
                    return null;
                }

                if (!config.readPoints.GetValueOrDefault())
                {
                    //reading tin data
                    if (!readTinData(config, connection, res))
                    {
                        return null; //return null if something failed
                    }
                }
                else
                {
                    //reading point data
                    if (!readPointData(config, connection, res) && config.readPoints.GetValueOrDefault())
                    {
                        return null;
                    }
                }

                //reading breaklines (if user made selection)
                if (config.breakline.GetValueOrDefault()
                    && !readBreaklines(config, connection, res))
                {
                    return null; //return null if something failed
                }
                return res;
            }

            //will be executed if db connection failed
            LogWriter.Add(LogType.error, "Database connection failed. Processing canceld!");
            return null;
        }

        /// <summary>
        /// PostGIS - query to establish DB connections to retrieve a TIN and, if necessary, the break edges.
        /// </summary>
        /// <returns>TIN via Result for terrain processing (IFCTerrain/Revit)</returns>
        public static bool connectDB(Config config, out NpgsqlConnection connection)
        {
            //prepare string for database connection
            string connString =
                string.Format(
                    "Host={0};Port={1};Username={2};Password={3};Database={4};",
                    config.host,
                    config.port,
                    config.user,
                    config.pwd,
                    config.database
                    );

            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[PostGIS] Connection string: " + connString));

            //build connection
            try
            {
                //return connection 
                connection = new NpgsqlConnection(connString);
                return true;
            }
            catch (Exception ex)
            {
                //set connection to null --> processing have to be stopped
                connection = null;

                LogWriter.Entries.Add(new LogPair(LogType.error, ex.Message));
                return false;
            }
        }

        /// <summary>
        /// reading tin data from database and set them to result class
        /// </summary>
        public static bool readTinData(Config config, NpgsqlConnection connection, Result res)
        {
            //local storage
            var triangleMap = new HashSet<Triangulator.triangleMap>();

            //
            var points = new HashSet<Point>();

            string select = null;
            if (!string.IsNullOrEmpty(config.queryString))
            {
                select = config.queryString;
            }
            else
            {
                select =
                "SELECT " + "ST_AsEWKT(" + config.tin_column + ") " +
                "as wkt FROM " + config.schema + "." + config.tin_table +
                " WHERE " + config.tinid_column + " = " + "'" + config.tin_id + "'";
            }
            LogWriter.Add(LogType.debug, "[PostGIS] Query string for TIN data: "
                + Environment.NewLine + select);

            using (var cmd = new NpgsqlCommand(select, connection))
            using (var reader = cmd.ExecuteReader(System.Data.CommandBehavior.SingleResult))
            {
                //read data from query
                reader.Read();

                /*
                //
                var queryData = reader[0] as GeometryCollection;
                if (queryData == null)
                {
                    return false;
                }
                //loop through all polygons
                foreach (var polygon in queryData.Geometries)
                {

                    int p1 = terrain.addPoint(points, new Point(polygon.Coordinates[0]));
                    int p2 = terrain.addPoint(points, new Point(polygon.Coordinates[1]));
                    int p3 = terrain.addPoint(points, new Point(polygon.Coordinates[2]));

                    triangleMap.Add(new Triangulator.triangleMap()
                    {
                        triNumber = triangleMap.Count,
                        triValues = new int[] { p1, p2, p3 }
                    });

                }

                //set type to conversion
                res.currentConversion = DtmConversionType.conversion;

                //set point list
                res.pointList = points.ToList();

                //set triangle map (needed for conversion)
                res.triMap = triangleMap;
                */

                //parse string
                string queryData;

                try
                {
                   queryData = reader[0].ToString();
                }
                catch (Exception ex)
                {
                    LogWriter.Add(LogType.error, ex.Message);
                    return false;
                }

                //split geomerty
                string[] geometry = queryData.Split(';');

                //
                string tinData = geometry[1];

                //Split for the beginning of the TIN
                char[] trim = { 'T', 'I', 'N', '(' };

                //Remove start string
                tinData = tinData.TrimStart(trim);

                string[] separator = { ")),((" };
                string[] tin = tinData.Split(separator, System.StringSplitOptions.RemoveEmptyEntries);

                //
                var pointList = new HashSet<Point>();

                foreach(string face in tin)
                {
                    string[] facePoints = face.Split(',');

                    //P1
                    string[] P01 = facePoints[0].Split(' ');
                    double p1X = Convert.ToDouble(P01[0], CultureInfo.InvariantCulture);
                    double p1Y = Convert.ToDouble(P01[1], CultureInfo.InvariantCulture);
                    double p1Z = Convert.ToDouble(P01[2], CultureInfo.InvariantCulture);

                    //P2
                    string[] P02 = facePoints[1].Split(' ');
                    double p2X = Convert.ToDouble(P02[0], CultureInfo.InvariantCulture);
                    double p2Y = Convert.ToDouble(P02[1], CultureInfo.InvariantCulture);
                    double p2Z = Convert.ToDouble(P02[2], CultureInfo.InvariantCulture);

                    //P2
                    string[] P03 = facePoints[2].Split(' ');
                    double p3X = Convert.ToDouble(P03[0], CultureInfo.InvariantCulture);
                    double p3Y = Convert.ToDouble(P03[1], CultureInfo.InvariantCulture);
                    double p3Z = Convert.ToDouble(P03[2], CultureInfo.InvariantCulture);

                    //get indices
                    int p1 = terrain.addPoint(pointList, new Point(p1X, p1Y, p1Z));
                    int p2 = terrain.addPoint(pointList, new Point(p2X, p2Y, p2Z));
                    int p3 = terrain.addPoint(pointList, new Point(p3X, p3Y, p3Z));

                    //set indices to triangle map
                    triangleMap.Add(new Triangulator.triangleMap() 
                    {
                        triNumber = triangleMap.Count,
                        triValues = new int[] {p1, p2, p3}
                    });
                }

                res.currentConversion = DtmConversionType.conversion;
                res.pointList = pointList.ToList();
                res.triMap = triangleMap;
            }
            
            connection.Close();
            return true;
        }

        /// <summary>
        /// read breakline data (using NTS)
        /// </summary>
        public static bool readBreaklines(Config config, NpgsqlConnection connection, Result res)
        {
            //open connection
            connection.Open();

            string selectBreakline = null;
            if (!string.IsNullOrEmpty(config.breaklineQueryString) && config.breakline.GetValueOrDefault())
            {
                selectBreakline = config.breaklineQueryString;
            }
            else
            {
                selectBreakline =
                "SELECT " + config.breakline_table + "." + config.breakline_column
                + " FROM " + config.schema + "." + config.breakline_table
                + " JOIN " + config.schema + "." + config.tin_table
                + " ON (" + config.breakline_table + "." + config.breakline_tin_id
                + " = " + config.tin_table + "." + config.tinid_column
                + ") WHERE " + config.tin_table + "." + config.tinid_column
                + " = " + "'" + config.tin_id + "'";
            }
            LogWriter.Add(LogType.debug, "[PostGIS] Query string for #breakline# data: " + Environment.NewLine + selectBreakline);

            //local storage for breaklines
            var lines = new List<LineString>();

            //start reading from postgis db
            using (var cmd = new NpgsqlCommand(selectBreakline, connection))
            using (var reader = cmd.ExecuteReader())
            {
                //loop with while --> getting all line entries which are connected to the dtm
                while (reader.Read())
                {
                    LineString queryData = null;
                    try
                    {
                        //get data (row wise) from query
                        queryData = reader.GetValue(0) as LineString;

                        //check if line string ist "closed" polygon
                        if (queryData.IsClosed)
                        {
                            //split line into "specific parts
                            Coordinate[] coords = queryData.Coordinates;

                            //int number for processing
                            int v = 0;
                            do
                            {
                                //coordinate for line string
                                Coordinate[] cs = new CoordinateZ[2];

                                //check if it is "last" line
                                if (!(coords[v].Equals(coords[0])
                                    && v != 0))
                                {
                                    //start point
                                    cs[0] = coords[v++];

                                    //end point
                                    cs[1] = coords[v];

                                    //add line to list
                                    LineString ls = new LineString(cs);
                                    lines.Add(ls);
                                }
                                else
                                {
                                    //break --> last line is not necessary (redundant)
                                    break;
                                }
                            }
                            while (v < coords.Count());
                        }
                        //add line (only for lines which are not closed
                        else if (queryData.Coordinates.Count() == 2)
                        {
                            //add line to list
                            lines.Add(queryData);
                        }
                        else
                        {
                            //return warning
                            LogWriter.Add(LogType.warning, "[PostGIS] Line data has been skipped!");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogWriter.Add(LogType.error, "[PostGIS] " + ex.Message);
                        return false;
                    }
                };

                //set conversion type
                res.currentConversion = DtmConversionType.points_breaklines;

                //handle lines to result class
                res.lines = lines;
            }

            //close connection
            connection.Close();

            //return true --> processing succesful
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool readPointData(Config config, NpgsqlConnection connection, Result res)
        {
            //local storage
            List<Point> points = new List<Point>();

            //query string
            string select =
                "SELECT " + config.tin_column + " " +
                "FROM " + config.schema + "." + config.tin_table + " " +
                "WHERE " + config.tinid_column + " = " + "'" + config.tin_id + "'";

            LogWriter.Add(LogType.debug, "[PostGIS] Query string for #point# data: " + Environment.NewLine + select);

            //start reading from postgis db
            using (var cmd = new NpgsqlCommand(select, connection))
            using (var reader = cmd.ExecuteReader())
            {
                reader.Read();

                if (reader.HasRows)
                {
                    //readed points
                    MultiPoint multiPoint = reader.GetValue(0) as MultiPoint;
                    LogWriter.Add(LogType.debug, "[PostGIS] Readed points: " + multiPoint.Count);

                    foreach (Point p in multiPoint)
                    {
                        points.Add(p);
                    }
                }
                else
                {
                    return false;
                }
            }

            //set conversion type
            res.currentConversion = DtmConversionType.points;

            //set points to result
            res.pointList = points;

            //close connection
            connection.Close();
            return true;
        }
    }
}