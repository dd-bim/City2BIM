﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Using Npgsql - .NET Access to PostgreSQL
//Link: https://www.npgsql.org/
using Npgsql;

//embed for CultureInfo handling
using System.Globalization;

//BimGisCad - Bibliothek einbinden
using BimGisCad.Representation.Geometry.Elementary; //Points, Lines, ...
using BimGisCad.Representation.Geometry.Composed;   //TIN

//Transfer class for the reader (IFCTerrain + Revit)
using BIMGISInteropLibs.IfcTerrain;

//[TODO #1]Revise reader into smaller structure
//[TODO #2]Logging
//[TOOD #3]add error handling + tests
namespace BIMGISInteropLibs.PostGIS
{
    public class ReaderTerrain
    {
        /// <summary>
        /// PostGIS - query to establish DB connections to retrieve a TIN and, if necessary, the break edges.
        /// </summary>
        /// <param name="Host">Link to the database server</param>
        /// <param name="Port">Port</param>
        /// <param name="User">Username</param>
        /// <param name="Password">Password to connect with database</param>
        /// <param name="DBname">DB designation on which is queried</param>
        /// <param name="schema">Scheme that contains the TIN (if necessary, also the broken edges)</param>
        /// <param name="tintable">Table in which the TIN is held</param>
        /// <param name="tincolumn">Column in which the TIN geometry is kept</param>
        /// <param name="tinidcolumn">Column designation in which the TIN IDs are managed</param>
        /// <param name="tinid">TIN ID for the TIN to be processed</param>
        /// <param name="postgis_bl">Decision whether to process broken edges</param>
        /// <param name="bl_column">Spalte in dem die Bruchkanten geführt werden</param>
        /// <param name="bl_table">Tabelle, dass die Bruchkanten enthält</param>
        /// <param name="bl_tinid">Column in which the TIN-ID is kept (needed to create JOIN to TIN-Table)</param>
        /// <returns>TIN via Result for terrain processing (IFCTerrain/Revit)</returns>
        public static Result ReadPostGIS(string Host, int Port, string User, string Password, string DBname, string schema, string tintable, string tincolumn, string tinidcolumn, int tinid, bool postgis_bl, string bl_column, string bl_table, string bl_tinid)
        {
            //TODO dynamic scaling
            double scale = 1.0;
            var result = new Result();

            //create TIN Builder
            var tinB = Tin.CreateBuilder(true);

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

                //TIN Request
                using (var conn = new NpgsqlConnection(connString))
                {
                    //Establish database connection
                    conn.Open();

                    NpgsqlConnection.GlobalTypeMapper.UseLegacyPostgis();

                    //TODO: Check whether other query options exist

                    //select request for tin without breaklines via TIN ID
                    string tin_select = "SELECT " + "ST_AsEWKT(" + tincolumn + ") as wkt FROM " + schema + "." + tintable + " WHERE " + tinidcolumn + " = " + tinid;

                    //select request for breaklines via TIN ID + JOIN
                    string bl_select = null;
                    if (postgis_bl == true)
                    {
                        bl_select = "SELECT ST_AsEWKT(" + bl_table + "." + bl_column + ") FROM " + schema + "." + bl_table + " JOIN " + schema + "." + tintable + " ON (" + bl_table + "." + bl_tinid + " = " + tintable + "." + tinidcolumn + ") WHERE " + tintable + "." + tinidcolumn + " = " + tinid;
                    }
                    //TIN abfragen
                    using (var command = new NpgsqlCommand(tin_select, conn))
                    {
                        var reader = command.ExecuteReader();
                        //Logger.Info("The following REQUEST have been sent: \n" + tin_select);
                        while (reader.Read())
                        {
                            //read column --> as EWKT
                            string geom_string = (reader.GetValue(0)).ToString();
                            
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

                            //Create point number for "artificial" indexing
                            int pnr = 0;

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

                                //Add points & increment one point number at a time
                                tinB.AddPoint(pnr++, p1);
                                tinB.AddPoint(pnr++, p2);
                                tinB.AddPoint(pnr++, p3);

                                //Loop to create the triangle
                                //TODO: check if it is a bad solution
                                for (int i = pnr - 3; i < pnr; i++)
                                {
                                    tinB.AddTriangle(i++, i++, i++);
                                }
                            }
                        }
                        //Close DB connection --> allows to establish further connections
                        conn.Close();
                    }
                    //TIN generate from TIN builder
                    Tin tin = tinB.ToTin(out var pointIndex2NumberMap, out var triangleIndex2NumberMap);
                    //hand over tin to result
                    result.Tin = tin;

                    //*** BREAKLINE PROCESSING ***
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
                            //Logger.Info("The following REQUEST have been sent: \n" + bl_select);

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
                }
            }
            catch (Exception e)
            {
                //TODO: add log with Exception
                Console.WriteLine(e.ToString());
                //Logger.Error("Database connection failed!");
            }
            Console.ReadLine();
            return result;
        }
    }
}