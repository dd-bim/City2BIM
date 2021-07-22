using BimGisCad.Representation.Geometry.Composed;   //TIN
//BimGisCad - Bibliothek einbinden
using BimGisCad.Representation.Geometry.Elementary; //Points, Lines, ...
//Transfer class for the reader (IFCTerrain + Revit)
using BIMGISInteropLibs.IfcTerrain;

//[TODO #1]Revise reader into smaller structure
//Logging
using BIMGISInteropLibs.Logging;

//Using Npgsql - .NET Access to PostgreSQL
//Link: https://www.npgsql.org/
using Npgsql;

using System;
using System.Collections.Generic;
//embed for CultureInfo handling
using System.Globalization;
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

//shortcut for tin building class
using terrain = BIMGISInteropLibs.Geometry.terrain;

//Compute triangulation
using BIMGISInteropLibs.Triangulator;


using RestSharp;
using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Threading.Tasks;

//[TOOD #3]add error handling + tests
namespace BIMGISInteropLibs.PostGIS
{
    public class ReaderTerrain
    {
        /// <summary>
        /// PostGIS - query to establish DB connections to retrieve a TIN and, if necessary, the break edges.
        /// </summary>
        /// <returns>TIN via Result for terrain processing (IFCTerrain/Revit)</returns>
        public static Result ReadPostGIS(JsonSettings jSettings)
        {
            string Host = jSettings.host;
            int Port = jSettings.port.GetValueOrDefault();
            string User = jSettings.user;
            string Password = jSettings.password;
            string DBname = jSettings.database;
            string schema = jSettings.schema;
            string tintable = jSettings.tin_table;
            string tincolumn = jSettings.tin_column;
            string tinidcolumn = jSettings.tinid_column;
            dynamic tinid = jSettings.tin_id;

            bool postgis_bl = jSettings.breakline.GetValueOrDefault();
            string bl_column = jSettings.breakline_column;
            string bl_table = jSettings.breakline_table;
            string bl_tinid = jSettings.breakline_tin_id;

            //TODO dynamic scaling
            double scale = 1.0;
            LogWriter.Add(LogType.verbose, "[PostGIS] processing started.");

            var result = new Result();

            //create TIN Builder
            var tinB = Tin.CreateBuilder(true);
            LogWriter.Add(LogType.verbose, "[PostGIS] create TIN builder.");

            //Container to store breaklines
            Dictionary<int, Line3> breaklines = new Dictionary<int, Line3>();

            try
            {
                //prepare string for database connection
                string connString =
                    string.Format(
                        "Host={0};Port={1};Username={2};Password={3};Database={4};",
                        Host,
                        Port,
                        User,
                        Password,
                        DBname
                        );

                var conn = new NpgsqlConnection(connString);

                conn.Open();
                LogWriter.Add(LogType.info, "[PostGIS] Connected to Database.");

                NpgsqlConnection.GlobalTypeMapper.UseLegacyPostgis();


                //TODO: Check whether other query options exist

                //select request for tin without breaklines via TIN ID
                string tin_select = "SELECT " + "ST_AsEWKT(" + tincolumn + ") as wkt FROM " + schema + "." + tintable + " WHERE " + tinidcolumn + " = " + "'" + tinid + "'";

                //select request for breaklines via TIN ID + JOIN
                string bl_select = null;

                if (postgis_bl == true)
                {
                    bl_select = "SELECT ST_AsEWKT(" + bl_table + "." + bl_column + ") FROM " + schema + "." + bl_table + " JOIN " + schema + "." + tintable + " ON (" + bl_table + "." + bl_tinid + " = " + tintable + "." + tinidcolumn + ") WHERE " + tintable + "." + tinidcolumn + " = " + tinid;
                }
                //Query TIN
                using (var command = new NpgsqlCommand(tin_select, conn))
                {

                    //init hash set
                    var pList = new HashSet<Geometry.uPoint3>();

                    var reader = command.ExecuteReader();
                    LogWriter.Add(LogType.debug, "[PostGIS] Request sent to database: \n" + tin_select);
                    while (reader.Read())
                    {
                        //read column --> as EWKT
                        string geom_string = (reader.GetValue(0)).ToString();
                        LogWriter.Add(LogType.verbose, "[PostGIS] reading from table via 'EWKT'");

                        //Split - CRS & TIN
                        string[] geom_split = geom_string.Split(';');
                        //String for EPSG - Code [TODO]: Check if EPSG code can be used for processing to metadata
                        string tin_epsg = geom_split[0];

                        //whole TIN 
                        string tin_gesamt = geom_split[1];

                        //Split for the beginning of the TIN
                        char[] trim = { 'T', 'I', 'N', '(' };
                        //Remove start string
                        tin_gesamt = tin_gesamt.TrimStart(trim);

                        //Split for each triangle
                        string[] separator = { ")),((" };
                        string[] tin_string = tin_gesamt.Split(separator, System.StringSplitOptions.RemoveEmptyEntries);

                        //Go through each triangle
                        foreach (string face in tin_string)
                        {
                            //Points - Split via comma
                            string[] face_points = face.Split(',');

                            //Split over spaces
                            //FirstCorner
                            string[] P1 = face_points[0].Split(' ');

                            double P1X = Convert.ToDouble(P1[0], CultureInfo.InvariantCulture);
                            double P1Y = Convert.ToDouble(P1[1], CultureInfo.InvariantCulture);
                            double P1Z = Convert.ToDouble(P1[2], CultureInfo.InvariantCulture);

                            //P1 
                            var p1 = Point3.Create(P1X * scale, P1Y * scale, P1Z * scale);

                            //SecoundCorner
                            string[] P2 = face_points[1].Split(' ');

                            double P2X = Convert.ToDouble(P2[0], CultureInfo.InvariantCulture);
                            double P2Y = Convert.ToDouble(P2[1], CultureInfo.InvariantCulture);
                            double P2Z = Convert.ToDouble(P2[2], CultureInfo.InvariantCulture);

                            //P2 
                            var p2 = Point3.Create(P2X * scale, P2Y * scale, P2Z * scale);

                            //ThirdCorner
                            string[] P3 = face_points[2].Split(' ');

                            double P3X = Convert.ToDouble(P3[0], CultureInfo.InvariantCulture);
                            double P3Y = Convert.ToDouble(P3[1], CultureInfo.InvariantCulture);
                            double P3Z = Convert.ToDouble(P3[2], CultureInfo.InvariantCulture);

                            //P3 
                            var p3 = Point3.Create(P3X * scale, P3Y * scale, P3Z * scale);

                            //add points to point list
                            int pnrP1 = terrain.addToList(pList, p1);
                            int pnrP2 = terrain.addToList(pList, p2);
                            int pnrP3 = terrain.addToList(pList, p3);

                            //add triangle via indicies (above)
                            tinB.AddTriangle(pnrP1, pnrP2, pnrP3);
                            LogWriter.Add(LogType.verbose, "[PostGIS] Triangle set (" + pnrP1 + "; " + pnrP2 + "; " + pnrP3 + ")");
                        }
                    }
                    //Close DB connection --> allows to establish further connections
                    conn.Close();

                    //loop through point list 
                    foreach (Geometry.uPoint3 pt in pList)
                    {
                        //add point to tin builder
                        tinB.AddPoint(pt.pnr, pt.point3);
                    }

                    //TIN generate from TIN builder
                    Tin tin = tinB.ToTin(out var pointIndex2NumberMap, out var triangleIndex2NumberMap);
                    //hand over tin to result
                    result.Tin = tin;

                    //add to results (stats)
                    result.rPoints = tin.Points.Count;
                    result.rFaces = tin.NumTriangles;

                    //*** BREAKLINE PROCESSING ***
                    /*
                    //Query whether break edges are to be processed
                    if (postgis_bl)
                    {
                        //DB Establish connection
                        conn.Open();

                        //Create index for processed lines
                        int index_poly = 0;
                        //Create index for break edges
                        int index = 0;

                        using (var command = new NpgsqlCommand(bl_select, conn))
                        {
                            //execute the (breakline) request string
                            var reader = command.ExecuteReader();
                            
                            LogWriter.Entries.Add(new LogPair(LogType.debug, "[PostGIS] Request sent to database: \n" + bl_select));

                            while (reader.Read())
                            {
                                //entire string of a break edge
                                string polyline_string = (reader.GetValue(0)).ToString();

                                //split 
                                string[] poly_split = polyline_string.Split(';');

                                //entire polyline which should be processed
                                string poly_gesamt = poly_split[1];

                                //Remove initial character: otherwise the first data set may not be converted
                                char[] trim = { 'L', 'I', 'N', 'E', 'S', 'T', 'R', 'I', 'N', 'G', '(' };
                                poly_gesamt = poly_gesamt.TrimStart(trim);
                                
                                //Remove last character: otherwise the last data set my not be converted
                                char[] trimEnd = { ')' };
                                poly_gesamt = poly_gesamt.TrimEnd(trimEnd);

                                //split for each point
                                string[] separator = { "," };
                                string[] polyline = poly_gesamt.Split(separator, System.StringSplitOptions.RemoveEmptyEntries);

                                //Create indexing
                                int i = 0;
                                int j = 1;

                                //Pass through each point in the polyline
                                do
                                {
                                    //Starting point
                                    //split
                                    string[] point_start_values = polyline[i].Split(' ');
                                    //processing X-Value,...
                                    double p1X = Convert.ToDouble(point_start_values[0], CultureInfo.InvariantCulture);
                                    double p1Y = Convert.ToDouble(point_start_values[1], CultureInfo.InvariantCulture);
                                    double p1Z = Convert.ToDouble(point_start_values[2], CultureInfo.InvariantCulture);
                                    //Create point instance (via BimGisCad Bib.)
                                    Point3 p1 = Point3.Create(p1X * scale, p1Y * scale, p1Z * scale);

                                    //Endpoint
                                    //split
                                    string[] point_end_values = polyline[j].Split(' ');
                                    //processing X-Value,...
                                    double p2X = Convert.ToDouble(point_end_values[0], CultureInfo.InvariantCulture);
                                    double p2Y = Convert.ToDouble(point_end_values[1], CultureInfo.InvariantCulture);
                                    double p2Z = Convert.ToDouble(point_end_values[2], CultureInfo.InvariantCulture);
                                    //Create point instance (via BimGisCad Bib.)
                                    Point3 p2 = Point3.Create(p2X * scale, p2Y * scale, p2Z * scale);
                                    //create vector
                                    Vector3 v12 = Vector3.Create(p2);
                                    //create direction
                                    Direction3 d12 = Direction3.Create(v12, scale);
                                    //Lines instance (break edge) via BimGisCad Bib.
                                    Line3 l = Line3.Create(p1, d12);
                                    try //TODO: check if this is meaningful
                                    {
                                        //Add breakline
                                        breaklines.Add(index++, l);
                                    }
                                    catch
                                    {
                                        index++;
                                    }
                                    i++;
                                    j++;
                                } while (j < polyline.Length);
                                index_poly++;
                            }
                            //Broken edges transferred to the result for further processing
                            result.Breaklines = breaklines;
                        }

                        //close database connection
                        conn.Close();
                    
                     
                    }
                    //process logging
                    //Logger.Info("All database connections have been disconnected.");
                    //Logger.Info("Reading PostGIS successful");
                    //Logger.Info(result.Tin.Points.Count() + " points; " + result.Tin.NumTriangles + " triangels processed");
                */
                }

            }
            catch (Exception e)
            {
                //log error message
                LogWriter.Add(LogType.error, "[PostGIS]: " +  e.Message);

                //
                //[REWORK] MessageBox.Show("[PostGIS]: " + e.Message, "PostGIS - Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return result;
        }

        /// <summary>
        /// A existing TIN is recalculated. Using breakline data is mandatory.
        /// </summary>
        /// <param name="jSettings">User input.</param>
        /// <returns>A TIN as result.</returns>
        public static Result RecalculateTin(JsonSettings jSettings)
        {
            //Initialize TIN
            Tin tin;

            //Initialize result
            Result result = new Result();

            //Get database connection parameters
            string host = jSettings.host;
            int port = jSettings.port.GetValueOrDefault();
            string user = jSettings.user;
            string password = jSettings.password;
            string dbName = jSettings.database;
            string schema = jSettings.schema;

            //Prepare string for database connection
            string connString = string.Format("Host={0};Port={1};Username={2};Password={3};Database={4};", host, port, user, password, dbName);

            //TIN data table
            string tinDataTable = jSettings.tin_table;
            string tinDataColumn = jSettings.tin_column;

            //Prepare sql string for tin data
            string tinDataSql = "SELECT " + "ST_AsEWKT(" + tinDataColumn + ") FROM " + schema + "." + tinDataTable + ";";

            //Breakline data table
            string breaklineDataTable = jSettings.breakline_table;
            string breaklineDataColumn = jSettings.breakline_column;

            //Prepare sql string for breakline data
            string breaklineDataSql = "SELECT " + "ST_AsEWKT(" + breaklineDataColumn + ") FROM " + schema + "." + breaklineDataTable + ";";

            try
            {
                //Establish database connection
                var conn = new NpgsqlConnection(connString);

                //Try to read out TIN and breakline data to recalculate TIN
                if (GetTinData(conn, tinDataSql, out List<double[]> tinPointList) && GetBreaklineData(conn, breaklineDataSql, out List<double[]> constraintList))
                {
                    //Read DTM data from PostGis, compute triangles and create TIN
                    tin = IfcTerrainTriangulator.CreateTin(tinPointList, constraintList);
                }
            }
            catch (Exception e)
            {
                //Log error message
                LogWriter.Add(LogType.error, "[PostGIS]: " + e.Message);

                //Show error message box
                //[REWORK] MessageBox.Show("[PostGIS]: " + e.Message, "PostGIS - Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            //Pass TIN to result and log
            result.Tin = tin;
            LogWriter.Add(LogType.info, "Reading PostGIS data successful.");
            result.rPoints = tin.Points.Count;
            result.rFaces = tin.NumTriangles;
            LogWriter.Add(LogType.debug, "Points: " + result.Tin.Points.Count + "; Triangles: " + result.Tin.NumTriangles + " processed");

            //Return the result as a TIN
            return result;
        }

        /// <summary>
        /// A TIN is calculated by using point data and if choosen breakline data as well.
        /// </summary>
        /// <param name="jSettings">User input.</param>
        /// <returns>A TIN as result.</returns>
        public static Result CalculateTin(JsonSettings jSettings)
        {
            //Initialize TIN
            Tin tin;

            //Initialize result
            Result result = new Result();

            //Get database connection parameters
            string host = jSettings.host;
            int port = jSettings.port.GetValueOrDefault();
            string user = jSettings.user;
            string password = jSettings.password;
            string dbName = jSettings.database;
            string schema = jSettings.schema;

            //Prepare string for database connection
            string connString = string.Format("Host={0};Port={1};Username={2};Password={3};Database={4};", host, port, user, password, dbName);

            //DTM data table
            string dtmDataTable = jSettings.tin_table;
            string dtmDataColumn = jSettings.tin_column;

            //Prepare sql string for dtm data
            string dtmDataSql = "SELECT " + "ST_AsEWKT(" + dtmDataColumn + ") FROM " + schema + "." + dtmDataTable + ";";

            //Breakline data table
            string breaklineDataTable = jSettings.breakline_table;
            string breaklineDataColumn = jSettings.breakline_column;

            //Prepare sql string for breakline data
            string breaklineDataSql = "SELECT " + "ST_AsEWKT(" + breaklineDataColumn + ") FROM " + schema + "." + breaklineDataTable + ";";

            try
            {
                //Establish database connection
                var conn = new NpgsqlConnection(connString);

                //Decide weather breaklines shall be used or not used 
                if (jSettings.breakline.GetValueOrDefault() && GetDtmData(conn, dtmDataSql, out List<double[]> dtmPointList) && GetBreaklineData(conn, breaklineDataSql, out List<double[]> constraintList))
                {
                    //Read DTM data from PostGis, compute triangles and create TIN
                    tin = IfcTerrainTriangulator.CreateTin(dtmPointList, constraintList);
                }
                else if (GetDtmData(conn, dtmDataSql, out dtmPointList))
                {
                    tin = IfcTerrainTriangulator.CreateTin(dtmPointList);
                }
            }
            catch (Exception ex)
            {
                //Log error message
                LogWriter.Add(LogType.error, "[PostGIS]: " + ex.Message);

                //write to console
                Console.WriteLine("[PostGIS] - ERROR: " + ex.Message);
            }

            //Pass TIN to result and log
            result.Tin = tin;
            LogWriter.Add(LogType.info, "Reading PostGIS data successful.");
            result.rPoints = tin.Points.Count;
            result.rFaces = tin.NumTriangles;
            LogWriter.Add(LogType.debug, "Points: " + result.Tin.Points.Count + "; Triangles: " + result.Tin.NumTriangles + " processed");

            //Return the result as a TIN
            return result;
        }

        /// <summary>
        /// An auxiliary function to get TIN data. 
        /// </summary>
        /// <param name="conn">The connection string to connect to the database.</param>
        /// <param name="tinDataSql">Query for TIN data.</param>
        /// <param name="tinPointList">A list of double arrays. Each array contains the x, y and z coordinate of a TIN vertex.</param>
        /// <returns>True or false in case TIN data was read successful or not respectively.</returns>
        public static bool GetTinData(NpgsqlConnection conn, string tinDataSql, out List<double[]> tinPointList)
        {
            tinPointList = ReadTinData(conn, tinDataSql);
            if (tinPointList.Count == 0)
            {
                LogWriter.Add(LogType.info, "[PostGIS] No TIN data found.");
                return false;
            }
            else
            {
                LogWriter.Add(LogType.info, "[PostGIS] Reading TIN data was successful.");
                return true;
            }
        }

        /// <summary>
        /// An auxiliary function to get DTM data.
        /// </summary>
        /// <param name="conn">The connection string to connect to the database.</param>
        /// <param name="dtmDataSql">Query for DTM data.</param>
        /// <param name="dtmPointList">A list of double arrays. Each array contains the x, y and z coordinate of a DTM point.</param>
        /// <returns>True or false in case DTM data was read successful or not respectively.</returns>
        public static bool GetDtmData(NpgsqlConnection conn, string dtmDataSql, out List<double[]> dtmPointList)
        {
            dtmPointList = ReadDtmData(conn, dtmDataSql);
            if (dtmPointList.Count == 0)
            {
                LogWriter.Add(LogType.info, "[PostGIS] No DTM point data found.");
                return false;
            }
            else
            {
                LogWriter.Add(LogType.info, "[PostGIS] Reading DTM point data was successful.");
                return true;
            }
        }

        /// <summary>
        /// An auxiliary function to get breakline data.
        /// </summary>
        /// <param name="conn">The connection string to connect to the database.</param>
        /// <param name="breaklineDataSql">Query for breakline data.</param>
        /// <param name="breaklineList">A list of double arrays. Each array contains the x, y and z coordinate of a constraint vertex.</param>
        /// <returns>rue or false in case breakline data was read successful or not respectively.</returns>
        public static bool GetBreaklineData(NpgsqlConnection conn, string breaklineDataSql, out List<double[]> breaklineList)
        {
            breaklineList = ReadBreaklineData(conn, breaklineDataSql);

            if (breaklineList.Count == 0)
            {
                LogWriter.Add(LogType.info, "[PostGIS] No breakline data found.");
                return false;
            }
            else
            {
                LogWriter.Add(LogType.info, "[PostGIS] Reading breakline data was successful.");
                return true;
            }
        }

        /// <summary>
        /// An auxiliary function to read out a OGC Simple Feature TIN from PostGis.
        /// </summary>
        /// <param name="conn">The connection string to connect to the database.</param>
        /// <param name="tinDataSql">Query for TIN data.</param>
        /// <returns>A list of double arrays. Each array contains the x, y and z coordinate of a TIN vertex.</returns>
        public static List<double[]> ReadTinData(NpgsqlConnection conn, string tinDataSql)
        {
            //Output variables for WKT strings
            List<double[]> tinPointList = new List<double[]>();
            double scale = 1.0;

            //Open database connection
            conn.Open();
            LogWriter.Add(LogType.info, "[PostGIS] Connected to Database. Reading TIN data.");
            NpgsqlConnection.GlobalTypeMapper.UseLegacyPostgis();

            //Execute query
            using (var command = new NpgsqlCommand(tinDataSql, conn))
            {
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    string wktString = (reader.GetValue(0)).ToString();
                    string[] wktStringSplit = wktString.Split(';');
                    string tinAsWkt = wktStringSplit[1];
                    char[] trim = { 'T', 'I', 'N', '(' };
                    tinAsWkt = tinAsWkt.TrimStart(trim);
                    string[] separator = { ")),((" };
                    string[] tin_string = tinAsWkt.Split(separator, System.StringSplitOptions.RemoveEmptyEntries);
                    foreach (string face in tin_string)
                    {
                        //Points - Split via comma
                        string[] face_points = face.Split(',');

                        //Split over spaces
                        //FirstCorner
                        string[] p1 = face_points[0].Split(' ');

                        double p1X = Convert.ToDouble(p1[0], CultureInfo.InvariantCulture);
                        double p1Y = Convert.ToDouble(p1[1], CultureInfo.InvariantCulture);
                        double p1Z = Convert.ToDouble(p1[2], CultureInfo.InvariantCulture);

                        //P1 
                        tinPointList.Add(new double[] { p1X * scale, p1Y * scale, p1Z * scale });

                        //SecoundCorner
                        string[] p2 = face_points[1].Split(' ');

                        double p2X = Convert.ToDouble(p2[0], CultureInfo.InvariantCulture);
                        double p2Y = Convert.ToDouble(p2[1], CultureInfo.InvariantCulture);
                        double p2Z = Convert.ToDouble(p2[2], CultureInfo.InvariantCulture);

                        //P2 
                        tinPointList.Add(new double[] { p2X * scale, p2Y * scale, p2Z * scale });

                        //ThirdCorner
                        string[] p3 = face_points[2].Split(' ');

                        double p3X = Convert.ToDouble(p3[0], CultureInfo.InvariantCulture);
                        double p3Y = Convert.ToDouble(p3[1], CultureInfo.InvariantCulture);
                        double p3Z = Convert.ToDouble(p3[2], CultureInfo.InvariantCulture);

                        //P3
                        tinPointList.Add(new double[] { p3X * scale, p3Y * scale, p3Z * scale });
                    }
                }
            }
            conn.Close();
            return tinPointList;
        }

        /// <summary>
        /// An auxiliary function to read out a OGC Simple Feature Multipoint from PostGis.
        /// </summary>
        /// <param name="conn">The connection string to connect to the database.</param>
        /// <param name="dtmDataSql">Query for DTM point data.</param>
        /// <returns>A list of double arrays. Each array contains the x, y and z coordinate of a DTM point.</returns>
        public static List<double[]> ReadDtmData(NpgsqlConnection conn, string dtmDataSql)
        {
            //Output variables for WKT strings
            List<double[]> dtmPointList = new List<double[]>();

            //Open database connection
            conn.Open();
            LogWriter.Add(LogType.info, "[PostGIS] Connected to Database. Reading DTM point data.");
            NpgsqlConnection.GlobalTypeMapper.UseLegacyPostgis();

            //Execute query
            using (var command = new NpgsqlCommand(dtmDataSql, conn))
            {
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    string wktString = (reader.GetValue(0)).ToString();
                    string[] wktStringSplit = wktString.Split(';');
                    string dtmDataAsWkt = wktStringSplit[1];
                    char[] trim = { 'M', 'U', 'L', 'T', 'I', 'P', 'O', 'I', 'N', 'T', '(' };
                    dtmDataAsWkt = dtmDataAsWkt.TrimStart(trim);
                    char[] trimEnd = { ')' };
                    dtmDataAsWkt = dtmDataAsWkt.TrimEnd(trimEnd);
                    string[] separator = { "," };
                    string[] pointArr = dtmDataAsWkt.Split(separator, System.StringSplitOptions.RemoveEmptyEntries);
                    foreach (string point in pointArr)
                    {
                        string[] coords = point.Split(' ');
                        double pX = Convert.ToDouble(coords[0], CultureInfo.InvariantCulture);
                        double pY = Convert.ToDouble(coords[1], CultureInfo.InvariantCulture);
                        double pZ = Convert.ToDouble(coords[2], CultureInfo.InvariantCulture);
                        dtmPointList.Add(new double[] { pX, pY, pZ });
                    }
                }
            }
            conn.Close();
            return dtmPointList;
        }

        /// <summary>
        /// An auxiliary function to read out a OGC Simple Feature LineString from PostGis.
        /// </summary>
        /// <param name="conn">The connection string to connect to the database.</param>
        /// <param name="breaklineDataSql">Query for breakline data.</param>
        /// <returns>A list of double arrays. Each array contains the x, y and z coordinate of a constraint vertex.</returns>
        public static List<double[]> ReadBreaklineData(NpgsqlConnection conn, string breaklineDataSql)
        {
            //Prepare List of strings for breakline data
            List<double[]> constraintList = new List<double[]>();

            //Open database connection
            conn.Open();
            LogWriter.Add(LogType.info, "[PostGIS] Connected to Database. Reading breakline data.");
            NpgsqlConnection.GlobalTypeMapper.UseLegacyPostgis();

            //Execute query
            using (var command = new NpgsqlCommand(breaklineDataSql, conn))
            {
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    string wktString = (reader.GetValue(0)).ToString();
                    string[] wktStringSplit = wktString.Split(';');
                    string breaklineAsWkt = wktStringSplit[1];
                    char[] trim = { 'L', 'I', 'N', 'E', 'S', 'T', 'R', 'I', 'N', 'G', '(' };
                    breaklineAsWkt = breaklineAsWkt.TrimStart(trim);
                    char[] trimEnd = { ')' };
                    breaklineAsWkt = breaklineAsWkt.TrimEnd(trimEnd);
                    string[] separator = { "," };
                    string[] constraintArr = breaklineAsWkt.Split(separator, System.StringSplitOptions.RemoveEmptyEntries);
                    foreach (string constraint in constraintArr)
                    {
                        string[] coords = constraint.Split(' ');
                        double cX = Convert.ToDouble(coords[0], CultureInfo.InvariantCulture);
                        double cY = Convert.ToDouble(coords[1], CultureInfo.InvariantCulture);
                        double cZ = Convert.ToDouble(coords[2], CultureInfo.InvariantCulture);
                        constraintList.Add(new double[] { cX, cY, cZ });
                    }
                }
            }
            conn.Close();
            return constraintList;
        }

    }

    public class RvtReaderTerrain : RestClient
    {
        public static void RvtReadPostGIS(JsonSettings config)
        {
            //getLogin(config);

            string ConnString = connStringSSL(config);

            var con = new NpgsqlConnection(ConnString); 

            using (NpgsqlConnection conn = new NpgsqlConnection(ConnString))
            {
                try
                {
                    conn.Open();
                    //NpgsqlCommand cmd = new NpgsqlCommand("SELECT * FROM tableName", conn);
                    
                    
                }
                catch (Exception ex)
                {
                    //[REWORK] MessageBox.Show(ex.ToString());
                }
            }


        }

        public static void getLogin(JsonSettings config)
        {
            var client = new RestClient(config.host);

            string login = connString(config);

            var request = new RestRequest(Method.GET);
            request.AddParameter("application/json", login);
            request.RequestFormat = RestSharp.DataFormat.None;
           
            IRestResponse response = client.Execute(request);
            
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                try
                {
                    var conn = new NpgsqlConnection(login);

                    conn.Open();
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                return;
            }
        }

        public static string connString(JsonSettings jSettings)
        {
            return string.Format("Host={0};Port={1};Username={2};Password={3};Database={4};",
                        jSettings.host,
                        jSettings.port,
                        jSettings.user,
                        jSettings.password,
                        jSettings.database);
        }

        public static string connStringSSL(JsonSettings jSettings)
        {
            return string.Format("Server={0};Database={4};Port={1};Username={2};Password={3};Trust Server Certificate=true;Ssl Mode=Require",
                        jSettings.host,
                        jSettings.port,
                        jSettings.user,
                        jSettings.password,
                        jSettings.database);
        }

        public class LoginResponse
        {
            public LoginData data { get; set; }
        }

        public class LoginData
        {
            public string login { get; set; }
        }

        
    }
}
