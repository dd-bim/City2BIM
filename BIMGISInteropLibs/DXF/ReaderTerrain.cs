using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//File handling
using System.IO;

//IxMilia - Bib for processing DXF files
using IxMilia.Dxf;          //file handling
using IxMilia.Dxf.Entities; //entites in dxf file (used for processing of faces)

//implement BimGisCad
using BimGisCad.Representation.Geometry.Elementary; //Points, Lines, ...
using BimGisCad.Representation.Geometry.Composed;   //TIN
using BimGisCad.Collections;                        //MESH

//Transfer class for the reader (IFCTerrain + Revit)
using BIMGISInteropLibs.IfcTerrain;

//embed for error handling
using System.Windows; //error handling (message box)

//Logging
using BIMGISInteropLibs.Logging;
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages
using BIMGISInteropLibs.NTSApi;

namespace BIMGISInteropLibs.DXF
{    
    /// <summary>
    /// Reader for DXF file processing includes more classes for read file, process dtm,...
    /// </summary>
    public class ReaderTerrain
    {
        /// <summary>
        /// Dictionary for DxfUnits - need for different unit import
        /// </summary>
        public static readonly Dictionary<DxfUnits, double> UnitToMeter = new Dictionary<DxfUnits, double>()
        {
            [DxfUnits.Millimeters] = 0.001,
            [DxfUnits.Centimeters] = 0.01,
            [DxfUnits.Decimeters] = 0.1,
            [DxfUnits.Meters] = 1.0,
            [DxfUnits.Kilometers] = 1000.0,
            [DxfUnits.Feet] = 0.3048,
            [DxfUnits.Inches] = 0.0254,
            [DxfUnits.Miles] = 1609.34,
            [DxfUnits.USSurveyFeet] = 1200.0 / 3937.0,
            [DxfUnits.Unitless] = 1.0
        };

        /// <summary>
        /// Reading DXF FILE and output DXF FILE for further processing
        /// </summary>
        /// <param name="fileName">Location of the DXF file</param>
        /// <param name="dxfFile">Output for further processing</param>
        /// <returns>DXFFile for processing via IxMilia</returns>
        public static bool ReadFile(string fileName, out DxfFile dxfFile)
        {
            dxfFile = null;

            //try to open DXF file via FileStream
            try
            {
                //fileName will be given by Interface (IFCTerrainGUI or ~Revit~)
                using (var fs = new FileStream(fileName, FileMode.Open))
                {
                    dxfFile = DxfFile.Load(fs);

                    LogWriter.Entries.Add(new LogPair(LogType.verbose, "DXF file has been read (" + fileName + ")"));

                    return true;
                }
            }
            
            //if it can't be opend
            catch(Exception ex)
            {
                LogWriter.Entries.Add(new LogPair(LogType.error, "DXF file could not be read (" + fileName + ")"));

                MessageBox.Show("DXF file could not be read: \n" + ex.Message, "DXF file reader", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Processing the meshing of a DXF file
        /// </summary>
        /// <param name="dxfFile">DXF-File</param>
        /// <param name="layer">all layers of the mesh</param>
        /// <param name="breaklinelayer">all layers of the breaklines</param>
        /// <param name="minDist">minimal distance</param>
        /// <param name="breakline">boolean value whether to process break edges</param>
        /// <returns>TIN (in form of result.Tin)</returns>
        public static Result ReadDxfTin(DxfFile dxfFile, JsonSettings jsonSettings)
        {
            //[TODO]
            //use of is3D is not implemented right now
            double minDistSq = jsonSettings.minDist * jsonSettings.minDist;
            
            //new result for handover of the tin (and mesh)
            var result = new Result();
            
            //Review, scale shouldn't be static??? [TODO]
            if (!UnitToMeter.TryGetValue(dxfFile.Header.DefaultDrawingUnits, out double scale))
            {
                scale = 1.0;
                LogWriter.Entries.Add(new LogPair(LogType.verbose, "[DXF] Set scale to: " + scale.ToString()));
            }

            //TIN-Builder initalise
            var tinB = Tin.CreateBuilder(true);
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[DXF] Initialize a TIN builder."));

            //Dictionary for "saving" breaklines
            Dictionary<int, Line3> breaklines = new Dictionary<int, Line3>(); 

            int processedBreaklines = 0;

            //PNR counter to increment the point number
            int pnr = 0;

            //loop to go through all entities of the DXF file
            foreach (var entity in dxfFile.Entities)
            {
                //Check if the layer to be processed corresponds to the "current" entity
                //furthermore it is checked if it is a face
                if (entity.Layer == jsonSettings.layer && entity is Dxf3DFace face)
                {
                    //query the four points of the face and pass them to variable p1 ... p4 passed
                    var p1 = Point3.Create(Math.Round(face.FirstCorner.X * scale,3),Math.Round(face.FirstCorner.Y * scale,3), Math.Round(face.FirstCorner.Z * scale,3));
                    var p2 = Point3.Create(Math.Round(face.SecondCorner.X * scale,3), Math.Round(face.SecondCorner.Y * scale,3), Math.Round(face.SecondCorner.Z * scale,3));
                    var p3 = Point3.Create(Math.Round(face.ThirdCorner.X * scale,3), Math.Round(face.ThirdCorner.Y * scale,3), Math.Round(face.ThirdCorner.Z * scale,3));
                    var p4 = Point3.Create(Math.Round(face.FourthCorner.X * scale,3), Math.Round(face.FourthCorner.Y * scale,3), Math.Round(face.FourthCorner.Z * scale,3));
                    if (Vector3.Norm2(p4 - p3) < minDistSq)
                    {
                        //Add points & increment one point number at a time
                        tinB.AddPoint(pnr++, p1);
                        LogWriter.Entries.Add(new LogPair(LogType.verbose, "[DXF] Point set (x= " + p1.X + "; y= " + p1.Y +"; z= " + p1.Z +")"));
                        tinB.AddPoint(pnr++, p2);
                        LogWriter.Entries.Add(new LogPair(LogType.verbose, "[DXF] Point set (x= " + p2.X + "; y= " + p2.Y + "; z= " + p2.Z + ")"));
                        tinB.AddPoint(pnr++, p3);
                        LogWriter.Entries.Add(new LogPair(LogType.verbose, "[DXF] Point set (x= " + p3.X + "; y= " + p3.Y + "; z= " + p3.Z + ")"));

                        //Loop to create the triangle
                        for (int i = pnr - 3; i < pnr; i++)
                        {
                            //add triangle to tin builder
                            tinB.AddTriangle(i++, i++, i++);
                            
                            //logging
                            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[DXF] Triangle set."));
                        }
                    }
                }

                //Check if selected layer of break edges is present in current entity
                //furthermore it is checked if the functionality should be used at all
                if (entity.Layer == jsonSettings.breakline_layer && jsonSettings.breakline == true)
                {
                    switch (entity.EntityType)
                    {
                        //case line
                        case DxfEntityType.Line: //Linie
                            //get entity line
                            var line = (DxfLine)entity;
                            
                            //first point
                            Point3 p1 = Point3.Create(line.P1.X * scale, line.P1.Y * scale, line.P1.Z * scale);
                            
                            //next point
                            Point3 p2 = Point3.Create(line.P2.X * scale, line.P2.Y * scale, line.P2.Z * scale);

                            //create line
                            Vector3 v12 = Vector3.Create(p2);
                            Direction3 d12 = Direction3.Create(v12, scale);
                            Line3 l = Line3.Create(p1, d12);

                            //add breaklines to dictionary
                            breaklines.Add(processedBreaklines++, l);
                            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[DXF] Breakline added ( xA= " + l.Position.X + "; yA= " + l.Position.Y + "; zA= " + l.Position.Z + "; xE= " + l.Direction.X + "; yE= " + l.Direction.Y + "; zE= " + l.Direction.Z + ")"));
                            
                            break;

                        //TODO add case polyline
                    }
                }
            }
            /* EDIT with new query! [TODO]
            if(!tin.Points.Any() || !tin.FaceEdges.Any())
            {
                result.Error = Properties.Resources.errNoLineData;
                logger.Error("Error. No line data found");
                return result;
            }
            */

            //Generate TIN from TIN Builder
            Tin tin = tinB.ToTin(out var pointIndex2NumberMap, out var triangleIndex2NumberMap);
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[DXF] Creating TIN via TIN builder."));

            //Result describe
            result.Tin = tin;

            //Describe result for break edges
            result.Breaklines = breaklines;

            //logging
            LogWriter.Entries.Add(new LogPair(LogType.info, "Reading DXF data successful."));
            LogWriter.Entries.Add(new LogPair(LogType.debug, "Points: " + result.Tin.Points.Count + "; Triangles: " + result.Tin.NumTriangles + " processed"));

            //add to results (stats)
            result.rPoints = tin.Points.Count;
            result.rFaces = tin.NumTriangles;

            //Transferring the result: for further processing in IFCTerrain or ~Revit~
            return result;
        }

        /// <summary>
        /// reading dxf via points and lines<para/>
        /// need mesh to build (otherwise not able to process)
        /// </summary>
        /// <param name="dxfFile">DXF file</param>
        /// <param name="jSettings">JSON settings</param>
        /// <returns>MESH (in form of result.Mesh)</returns>
        public static Result ReadDxfPoly(DxfFile dxfFile, JsonSettings jSettings)
        {
            //get boolean value if 2D or 3D
            bool is3d = jSettings.is3D;
            
            //minDist (currently not supported!)
            double minDist = jSettings.minDist;
            
            //get layer (TODO: multiple layer selection)
            string layer = jSettings.layer;
            
            //init result class
            var result = new Result();

            //check units
            if (!UnitToMeter.TryGetValue(dxfFile.Header.DefaultDrawingUnits, out double scale))
            {
                //TODO dynamic scaling
                scale = 1.0;
            }

            //create mesh builder
            var pp = new Mesh(is3d, minDist);
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[DXF] Initialize a MESH builder."));

            //go through all entities in dxf file
            foreach (var entity in dxfFile.Entities)
            {
                //if selected layer is matching
                if (entity.Layer == layer)
                {
                    //case disinction
                    switch (entity.EntityType)
                    {
                        //dxf entity case vertex --> only need to add point
                        case DxfEntityType.Vertex:
                            
                            //get entity
                            var vtx = (DxfVertex)entity;

                            //creat point via entity
                            var pt = Point3.Create(vtx.Location.X, vtx.Location.Y, vtx.Location.Z);
                            
                            //add point to mesh
                            pp.AddPoint(pt);

                            //logging
                            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[DXF] Point set (x= " + pt.X + "; y= " + pt.Y + "; z= " + pt.Z + ")"));
                            
                            break;

                        //dxf entity line --> add every line
                        case DxfEntityType.Line:
                            
                            //catch entity line
                            var line = (DxfLine)entity;

                            //first point (var)
                            var pt1 = Point3.Create(line.P1.X * scale, line.P1.Y * scale, line.P1.Z * scale);
                            
                            //create index (point 1)
                            int p1 = pp.AddPoint(pt1);

                            //logging
                            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[DXF] Point set (x= " + pt1.X + "; y= " + pt1.Y + "; z= " + pt1.Z + ")"));

                            //secount point (var)
                            var pt2 = Point3.Create(line.P2.X * scale, line.P2.Y * scale, line.P2.Z * scale);

                            //logging
                            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[DXF] Point set (x= " + pt2.X + "; y= " + pt2.Y + "; z= " + pt2.Z + ")"));

                            //create index (point 2)
                            int p2 = pp.AddPoint(pt2);
                            
                            //add edge to mesh builder
                            pp.FixEdge(p1, p2);

                            //logging
                            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[DXF] Line set (P1 (Index) = " + p1 + "; P2 (Index) = " + p2 + ")"));

                            break;

                        //dxf case polyline
                        case DxfEntityType.Polyline:

                            //get poly line entity
                            var poly = (DxfPolyline)entity;
                            
                            //create index "last"
                            int last = -1;
                            
                            //go through all vertices
                            foreach (var v in poly.Vertices)
                            {
                                //get index of current point
                                var ptpoly = Point3.Create(v.Location.X * scale, v.Location.Y * scale, v.Location.Z * scale);
                                LogWriter.Entries.Add(new LogPair(LogType.verbose, "[DXF] Point set (x= " + ptpoly.X + "; y= " + ptpoly.Y + "; z= " + ptpoly.Z + ")"));

                                //create index and map to mesh
                                int curr = pp.AddPoint(ptpoly);

                                //for last line
                                if (last >= 0)
                                {
                                    //create fix edge
                                    pp.FixEdge(last, curr);
                                    
                                    //logging
                                    LogWriter.Entries.Add(new LogPair(LogType.verbose, "[DXF] Line set (P1 (Index) = " + last + "; P2 (Index) = " + curr + ")"));
                                }
                                last = curr;
                            }
                            break;
                    }
                }
            }
            
            //error handler check if mesh is empty
            if (!pp.Points.Any() || !pp.FixedEdges.Any())
            {
                //logging
                LogWriter.Entries.Add(new LogPair(LogType.error, "[DXF] file could not be processed!"));

                //write to logging file
                LogWriter.WriteLogFile(jSettings.logFilePath, jSettings.verbosityLevel, System.IO.Path.GetFileNameWithoutExtension(jSettings.destFileName));
            }

            //pass the mesh to the result class
            result.Mesh = pp;

            //logging
            LogWriter.Entries.Add(new LogPair(LogType.info, "[DXF] Reading data successful!"));
            LogWriter.Entries.Add(new LogPair(LogType.info, "[DXF] " + pp.Points.Count + " points, " + pp.FixedEdges.Count + " lines and " + pp.FaceEdges.Count + " faces read."));
            
            result.rPoints = pp.Points.Count;
            result.rLines = pp.FixedEdges.Count;
            result.rFaces = pp.FaceEdges.Count;

            //return result - processing via ifc writer will be enabled 
            return result;
        }

        /// <summary>
        /// A function to create a TIN using NetTopologySuite class library and BimGisCad TIN builder.
        /// </summary>
        /// <param name="dxfFile">The DXF file containing the DTM data (poins and lines).</param>
        /// <param name="jSettings">The global settings for processing.</param>
        /// <returns>A result object which encapsulates the calculated TIN.</returns>
        public static Result CalculateDxfTin(DxfFile dxfFile, JsonSettings jSettings)
        {
            //Initialize result
            Result result = new Result();

            //Initialize TIN builder
            var tinBuilder = Tin.CreateBuilder(true);

            //Log TIN builder initalization
            AddToLogWriter(LogType.verbose, "[DXF] Initialize a TIN builder.");

            //Read out DTM data from DXF file. If successful than process data and create TIN
            if (ReadDxfDtmData(dxfFile, jSettings, out List<double[]> dtmPointData, out List<List<double[]>> dtmLineData))
            {
                //Get a list of triangles via NetTopologySuite class library using the interface object
                List<List<double[]>> dtmTriangleList = new NtsApi().MakeTriangleList(dtmPointData, dtmLineData);

                //Read out each triangle from the triangle list
                int pnr = 0;
                foreach (List<double[]> dtmTriangle in dtmTriangleList)
                {
                    //Read out the three vertices of one triangle at each loop
                    Point3 p1 = Point3.Create(dtmTriangle[0][0], dtmTriangle[0][1], dtmTriangle[0][2]);
                    Point3 p2 = Point3.Create(dtmTriangle[1][0], dtmTriangle[1][1], dtmTriangle[1][2]);
                    Point3 p3 = Point3.Create(dtmTriangle[2][0], dtmTriangle[2][1], dtmTriangle[2][2]);

                    //Add the triangle vertices to the TIN builder and log point coordinates
                    tinBuilder.AddPoint(pnr++, p1);
                    AddToLogWriter(LogType.verbose, "[DXF] Point set (x= " + p1.X + "; y= " + p1.Y + "; z= " + p1.Z + ")");
                    tinBuilder.AddPoint(pnr++, p2);
                    AddToLogWriter(LogType.verbose, "[DXF] Point set (x= " + p2.X + "; y= " + p2.Y + "; z= " + p2.Z + ")");
                    tinBuilder.AddPoint(pnr++, p3);
                    AddToLogWriter(LogType.verbose, "[DXF] Point set (x= " + p3.X + "; y= " + p3.Y + "; z= " + p3.Z + ")");

                    //Add the index of each vertex to the TIN builder (defines triangle) and log
                    for (int i = pnr - 3; i < pnr; i++)
                    {
                        tinBuilder.AddTriangle(i++, i++, i++);
                        AddToLogWriter(LogType.verbose, "[DXF] Triangle set.");
                    }
                }
            }

            //Build a TIN via BimGisCad class library and log
            Tin tin = tinBuilder.ToTin(out var pointIndex2NumberMap, out var triangleIndex2NumberMap);
            AddToLogWriter(LogType.verbose, "[DXF] Creating TIN via TIN builder.");

            //Pass TIN to result and log
            result.Tin = tin;
            AddToLogWriter(LogType.info, "Reading DXF data successful.");
            result.rPoints = tin.Points.Count;
            result.rFaces = tin.NumTriangles;
            AddToLogWriter(LogType.debug, "Points: " + result.Tin.Points.Count + "; Triangles: " + result.Tin.NumTriangles + " processed");

            //Return the result as a TIN
            return result;
        }

        /// <summary>
        /// An auxiliary function to read out point and line data from a DXF file according to the specified layers.
        /// </summary>
        /// <param name="dxfFile">The DXF file containing the DTM data (poins and lines).</param>
        /// <param name="jSettings">The global settings for processing.</param>
        /// <param name="dtmPointData">A list of double arrays. Each array contains the x-, y- and z-value of one point.</param>
        /// <param name="dtmLineData">A list of lists of double arrays. Each array contains the x-, y- and z-value of one point of one line.</param>
        /// <returns>True or false respectively depending on successful reading.</returns>
        public static bool ReadDxfDtmData(DxfFile dxfFile, JsonSettings jSettings, out List<double[]> dtmPointData, out List<List<double[]>> dtmLineData)
        {
            //A list of point data
            dtmPointData = new List<double[]>();

            //A list of lists of point data. Each list represents a line or polyline respectively
            dtmLineData = new List<List<double[]>>();

            //Get layer (TODO: multiple layer selection)
            string dtmLayer = jSettings.layer;

            string lineLayer = jSettings.breakline_layer;

            //Check units
            if (!UnitToMeter.TryGetValue(dxfFile.Header.DefaultDrawingUnits, out double scale))
            {
                //TODO dynamic scaling
                scale = 1.0;
            }
            
            //Go through all entities in dxf file
            foreach (var entity in dxfFile.Entities)
            {
                if (entity.Layer == dtmLayer || entity.Layer == lineLayer)
                {
                    //case disinction
                    switch (entity.EntityType)
                    {
                        case DxfEntityType.Face:
                            var dxfFace = (Dxf3DFace)entity;
                            dtmPointData.Add(new double[] { dxfFace.FirstCorner.X, dxfFace.FirstCorner.Y, dxfFace.FirstCorner.Z });
                            AddToLogWriter(LogType.verbose, "[DXF] Point set (x= " + dxfFace.FirstCorner.X + "; y= " + dxfFace.FirstCorner.Y + "; z= " + dxfFace.FirstCorner.Z + ")");
                            dtmPointData.Add(new double[] { dxfFace.SecondCorner.X, dxfFace.SecondCorner.Y, dxfFace.SecondCorner.Z });
                            AddToLogWriter(LogType.verbose, "[DXF] Point set (x= " + dxfFace.SecondCorner.X + "; y= " + dxfFace.SecondCorner.Y + "; z= " + dxfFace.SecondCorner.Z + ")");
                            dtmPointData.Add(new double[] { dxfFace.ThirdCorner.X, dxfFace.ThirdCorner.Y, dxfFace.ThirdCorner.Z });
                            AddToLogWriter(LogType.verbose, "[DXF] Point set (x= " + dxfFace.ThirdCorner.X + "; y= " + dxfFace.ThirdCorner.Y + "; z= " + dxfFace.ThirdCorner.Z + ")");
                            break;
                        //dxf entity line --> add every line
                        case DxfEntityType.Line:

                            //catch entity line
                            var line = (DxfLine)entity;

                            //A list of double arrays. Each array contains the x-, y- and z-values od a point of the lines
                            List<double[]> linePointData = new List<double[]>();

                            //Get the first point data of the line
                            linePointData.Add(new double[] { line.P1.X * scale, line.P1.Y * scale, line.P1.Z * scale });

                            //Log first point of line
                            AddToLogWriter(LogType.verbose, "[DXF] Point set (x= " + linePointData[0][0] + "; y= " + linePointData[0][1] + "; z= " + linePointData[0][2] + ")");

                            //Get the second point data of the line
                            linePointData.Add(new double[] { line.P2.X * scale, line.P2.Y * scale, line.P2.Z * scale });

                            //Log second point of line
                            AddToLogWriter(LogType.verbose, "[DXF] Point set (x= " + linePointData[1][0] + "; y= " + linePointData[1][1] + "; z= " + linePointData[1][2] + ")");

                            //Add the line data to the line data list
                            dtmLineData.Add(linePointData);
                            break;

                        //dxf case polyline
                        case DxfEntityType.Polyline:

                            //get poly line entity
                            var polyline = (DxfPolyline)entity;

                            //A list of double arrays. Each array contains the x-, y- and z-values od a point of the lines
                            List<double[]> polylinePointData = new List<double[]>();

                            //go through all vertices
                            foreach (var vertex in polyline.Vertices)
                            {
                                //Prepare x-, y- and z-coordinates of the vertex
                                double x = vertex.Location.X * scale;
                                double y = vertex.Location.Y * scale;
                                double z = vertex.Location.Z * scale;

                                //Add vertex cooedinates to data list
                                polylinePointData.Add(new double[] { x, y, z });

                                //Log point data
                                AddToLogWriter(LogType.verbose, "[DXF] Point set (x= " + x + "; y= " + y + "; z= " + z + ")");
                            }
                            break;
                    }
                }
            }

            //Check if lists contain data and return true or false
            return CheckDtmDataLists(dtmPointData, dtmLineData);
        }

        /// <summary>
        /// A auxiliary function to feed the log writer.
        /// </summary>
        /// <param name="logType">The type of logging.</param>
        /// <param name="message">The message to log.</param>
        public static void AddToLogWriter(LogType logType, string message)
        {
            LogWriter.Entries.Add(new LogPair(logType, message));
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
                AddToLogWriter(LogType.info, "[DXF] No point data found.");
                return false;
            }
            else if (dtmLineData.Count == 0)
            {
                AddToLogWriter(LogType.info, "[DXF] Reading point data was successful. No line data found.");
                return true;
            }
            else
            {
                AddToLogWriter(LogType.info, "[DXF] Reading point and line data was successful.");
                return true;
            }
        }
    }
}