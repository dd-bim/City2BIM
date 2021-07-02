﻿using BimGisCad.Representation.Geometry.Composed;   //TIN
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
using System.Windows; //include for message box (error handling db connection)
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

//shortcut for tin building class
using terrain = BIMGISInteropLibs.Geometry.terrain;

using BIMGISInteropLibs.NTSApi;

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
            int Port = jSettings.port;
            string User = jSettings.user;
            string Password = jSettings.password;
            string DBname = jSettings.database;
            string schema = jSettings.schema;
            string tintable = jSettings.tin_table;
            string tincolumn = jSettings.tin_column;
            string tinidcolumn = jSettings.tinid_column;
            dynamic tinid = jSettings.tin_id;

            bool postgis_bl = jSettings.breakline;
            string bl_column = jSettings.breakline_column;
            string bl_table = jSettings.breakline_table;
            string bl_tinid = jSettings.breakline_tin_id;

            //TODO dynamic scaling
            double scale = 1.0;
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[PostGIS] processing started."));

            var result = new Result();

            //create TIN Builder
            var tinB = Tin.CreateBuilder(true);
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[PostGIS] create TIN builder."));

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

                LogWriter.Entries.Add(new LogPair(LogType.info, "[PostGIS] Connected to Database."));

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
                    LogWriter.Entries.Add(new LogPair(LogType.debug, "[PostGIS] Request sent to database: \n" + tin_select));
                    while (reader.Read())
                    {
                        //read column --> as EWKT
                        string geom_string = (reader.GetValue(0)).ToString();
                        LogWriter.Entries.Add(new LogPair(LogType.verbose, "[PostGIS] reading from table via 'EWKT'"));

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
                            int pnrP1 = Geometry.terrain.addToList(pList, p1);
                            int pnrP2 = Geometry.terrain.addToList(pList, p2);
                            int pnrP3 = Geometry.terrain.addToList(pList, p3);

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
                LogWriter.Entries.Add(new LogPair(LogType.error, "[PostGIS]: " + e.Message));

                //write log file
                LogWriter.WriteLogFile(jSettings.logFilePath, jSettings.verbosityLevel, System.IO.Path.GetFileNameWithoutExtension(jSettings.destFileName));

                //
                MessageBox.Show("[PostGIS]: " + e.Message, "PostGIS - Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return result;
        }

        public static Result CalculatePostGisTin(JsonSettings jSettings)
        {
            //Initialize result
            Result result = new Result();

            //Initialize TIN builder
            var tinBuilder = Tin.CreateBuilder(true);

            //init hash set
            var uptList = new HashSet<Geometry.uPoint3>();

            //Log TIN builder initalization
            LogWriter.Add(LogType.verbose, "[PostGIS] Initialize a TIN builder.");

            //Read DTM data from PostGis, compute triangles and create TIN
            if (ReadPostGisDtmData(jSettings, out string dtmPointDataWKT, out string dtmLineDataWKT))
            {
                //Get a list of triangles via NetTopologySuite class library using the interface object
                List<List<double[]>> dtmTriangleList = new NtsApi().MakeTriangleList(dtmPointDataWKT, dtmLineDataWKT);

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
                    LogWriter.Add(LogType.verbose, "[PostGIS] Triangle [" + pnrP1 + "; " + pnrP2 + "; " + pnrP3 + "] set.");
                }
            }

            //loop through point list 
            foreach (Geometry.uPoint3 pt in uptList)
            {
                tinBuilder.AddPoint(pt.pnr, pt.point3);
            }

            //Build a TIN via BimGisCad class library and log
            Tin tin = tinBuilder.ToTin(out var pointIndex2NumberMap, out var triangleIndex2NumberMap);
            LogWriter.Add(LogType.verbose, "[PostGIS] Creating TIN via TIN builder.");

            //Pass TIN to result and log
            result.Tin = tin;
            LogWriter.Add(LogType.info, "Reading PostGIS data successful.");
            result.rPoints = tin.Points.Count;
            result.rFaces = tin.NumTriangles;
            LogWriter.Add(LogType.debug, "Points: " + result.Tin.Points.Count + "; Triangles: " + result.Tin.NumTriangles + " processed");

            //Return the result as a TIN
            return result;
        }

        public static bool ReadPostGisDtmData(JsonSettings jSettings, out string dtmPointDataWKT, out string dtmLineDataWKT)
        {
            //Output variables for WKT strings
            dtmPointDataWKT = "";
            dtmLineDataWKT = "";

            //Database connection parameters
            string host = "localhost";//jSettings.host;
            int port = 5432;//jSettings.port;
            string user = "postgres";//jSettings.user;
            string password = "postgres";//jSettings.password;
            string dbName = "ifcterrain";//jSettings.database;
            string schema = "public";//jSettings.schema;

            //DTM data table
            string dtmDataTable = "geosn_dtm_testdata";
            string dtmDataColumn = "geom";
            string dtmDataIdColumn = "";
            int dtmId = 0;

            //Breakline data table
            bool postgis_bl = jSettings.breakline;
            string bl_column = jSettings.breakline_column;
            string bl_table = jSettings.breakline_table;
            string bl_tinid = jSettings.breakline_tin_id;

            //Try to read data from database
            try
            {
                //Prepare string for database connection
                string connString = string.Format("Host={0};Port={1};Username={2};Password={3};Database={4};", host, port, user, password, dbName);

                //Establish database connection
                var conn = new NpgsqlConnection(connString);

                //Open database connection
                conn.Open();
                LogWriter.Add(LogType.info, "[PostGIS] Connected to Database.");
                NpgsqlConnection.GlobalTypeMapper.UseLegacyPostgis();

                //Prepare sql string
                string dtmPointDataSql = "SELECT " + "ST_AsEWKT(" + dtmDataColumn + ") FROM " + schema + "." + dtmDataTable + ";";

                //Execute query
                using (var command = new NpgsqlCommand(dtmPointDataSql, conn))
                {
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        dtmPointDataWKT += (reader.GetValue(0)).ToString() + ";";
                    }
                }
                conn.Close();
            }
            catch (Exception e)
            {
                //Log error message
                LogWriter.Entries.Add(new LogPair(LogType.error, "[PostGIS]: " + e.Message));

                //Write log file
                LogWriter.WriteLogFile(jSettings.logFilePath, jSettings.verbosityLevel, System.IO.Path.GetFileNameWithoutExtension(jSettings.destFileName));

                //Show error message box
                MessageBox.Show("[PostGIS]: " + e.Message, "PostGIS - Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return CheckDtmDataLists(dtmPointDataWKT, dtmLineDataWKT);
        }

        /// <summary>
        /// A auxiliary function to check if WKT strings contain data. 
        /// </summary>
        /// <param name="dtmPointDataWKT">A WKT string of point data.</param>
        /// <param name="dtmLineData">A WKT string line data.</param>
        /// <returns>True or false.</returns>
        public static bool CheckDtmDataLists(string dtmPointDataWKT, string dtmLineDataWKT)
        {
            if (dtmPointDataWKT.Length == 0)
            {
                LogWriter.Add(LogType.error, "[PostGIS] No point data found.");
                return false;
            }
            else if (dtmLineDataWKT.Length == 0)
            {
                LogWriter.Add(LogType.warning, "[PostGIS] Reading point data was successful. No line data found.");
                return true;
            }
            else
            {
                LogWriter.Add(LogType.info, "[PostGIS] Reading point and line data was successful.");
                return true;
            }
        }
    }
}
