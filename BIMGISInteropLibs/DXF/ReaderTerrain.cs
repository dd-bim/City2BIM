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

//shortcut for tin building class
using terrain = BIMGISInteropLibs.Geometry.terrain;

//Logging
using BIMGISInteropLibs.Logging;
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

//Compute triangulation
using BIMGISInteropLibs.Triangulator;

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

                    LogWriter.Add(LogType.verbose, "DXF file has been read (" + fileName + ")");

                    return true;
                }
            }
            
            //if it can't be opend
            catch(Exception ex)
            {
                LogWriter.Add(LogType.error, "DXF file could not be read (" + fileName + ")");

                Console.WriteLine("DXF file could not be read: " + Environment.NewLine + ex.Message);
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
                LogWriter.Add(LogType.verbose, "[DXF] Set scale to: " + scale.ToString());
            }

            //TIN-Builder initalise
            var tinB = Tin.CreateBuilder(true);
            LogWriter.Add(LogType.verbose, "[DXF] Initialize a TIN builder.");

            //Dictionary for "saving" breaklines
            Dictionary<int, Line3> breaklines = new Dictionary<int, Line3>(); 

            int processedBreaklines = 0;

            //init hash set
            var uptList = new HashSet<Geometry.uPoint3>();

            //loop to go through all entities of the DXF file
            foreach (var entity in dxfFile.Entities)
            {
                //Check if the layer to be processed corresponds to the "current" entity
                //furthermore it is checked if it is a face
                if (entity.Layer == jsonSettings.layer && entity is Dxf3DFace face)
                {
                    //query the four points of the face and pass them to variable p1 ... p4 passed
                    var p1 = Point3.Create(face.FirstCorner.X * scale,face.FirstCorner.Y * scale, face.FirstCorner.Z * scale);
                    var p2 = Point3.Create(face.SecondCorner.X * scale, face.SecondCorner.Y * scale, face.SecondCorner.Z * scale);
                    var p3 = Point3.Create(face.ThirdCorner.X * scale, face.ThirdCorner.Y * scale, face.ThirdCorner.Z * scale);
                    var p4 = Point3.Create(face.FourthCorner.X * scale, face.FourthCorner.Y * scale, face.FourthCorner.Z * scale);

                    //check if point is under min dist
                    if (Vector3.Norm2(p4 - p3) < minDistSq)
                    {
                        //add points to list [note: logging will be done in support function]
                        int pnrP1 = terrain.addToList(uptList, p1);
                        int pnrP2 = terrain.addToList(uptList, p2);
                        int pnrP3 = terrain.addToList(uptList, p3);
                        
                        //add triangle via point numbers above
                        tinB.AddTriangle(pnrP1, pnrP2, pnrP3);
                        
                        //log
                        LogWriter.Add(LogType.verbose, "[DXF] Triangle ["+pnrP1+"; "+pnrP2 + "; " + pnrP3+ "] set.");
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
                            LogWriter.Add(LogType.verbose, "[DXF] Breakline added ( xA= " + l.Position.X + "; yA= " + l.Position.Y + "; zA= " + l.Position.Z + Environment.NewLine + "; xE= " + l.Direction.X + "; yE= " + l.Direction.Y + "; zE= " + l.Direction.Z + ")");
                            
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

            //loop through point list 
            foreach (Geometry.uPoint3 pt in uptList)
            {
                tinB.AddPoint(pt.pnr, pt.point3);
            }

            //Generate TIN from TIN Builder
            Tin tin = tinB.ToTin(out var pointIndex2NumberMap, out var triangleIndex2NumberMap);
            LogWriter.Add(LogType.verbose, "[DXF] Creating TIN via TIN builder.");

            //Result describe
            result.Tin = tin;

            //Describe result for break edges
            result.Breaklines = breaklines;

            //logging
            LogWriter.Add(LogType.info, "Reading DXF data successful.");
            LogWriter.Add(LogType.debug, "Points: " + result.Tin.Points.Count + "; Triangles: " + result.Tin.NumTriangles + " processed");

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
            LogWriter.Add(LogType.verbose, "[DXF] Initialize a MESH builder.");

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
                            LogWriter.Add(LogType.verbose, "[DXF] Point set (x= " + pt.X + "; y= " + pt.Y + "; z= " + pt.Z + ")");
                            
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
                            LogWriter.Add(LogType.verbose, "[DXF] Point set (x= " + pt1.X + "; y= " + pt1.Y + "; z= " + pt1.Z + ")");

                            //secount point (var)
                            var pt2 = Point3.Create(line.P2.X * scale, line.P2.Y * scale, line.P2.Z * scale);

                            //logging
                            LogWriter.Add(LogType.verbose, "[DXF] Point set (x= " + pt2.X + "; y= " + pt2.Y + "; z= " + pt2.Z + ")");

                            //create index (point 2)
                            int p2 = pp.AddPoint(pt2);
                            
                            //add edge to mesh builder
                            pp.FixEdge(p1, p2);

                            //logging
                            LogWriter.Add(LogType.verbose, "[DXF] Line set (P1 (Index) = " + p1 + "; P2 (Index) = " + p2 + ")");

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
                                LogWriter.Add(LogType.verbose, "[DXF] Point set (x= " + ptpoly.X + "; y= " + ptpoly.Y + "; z= " + ptpoly.Z + ")");

                                //create index and map to mesh
                                int curr = pp.AddPoint(ptpoly);

                                //for last line
                                if (last >= 0)
                                {
                                    //create fix edge
                                    pp.FixEdge(last, curr);
                                    
                                    //logging
                                    LogWriter.Add(LogType.verbose, "[DXF] Line set (P1 (Index) = " + last + "; P2 (Index) = " + curr + ")");
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
                LogWriter.Add(LogType.error, "[DXF] file could not be processed!");
            }

            //pass the mesh to the result class
            result.Mesh = pp;

            //logging
            LogWriter.Add(LogType.info, "[DXF] Reading data successful!");
            LogWriter.Add(LogType.info, "[DXF] " + pp.Points.Count + " points, " + pp.FixedEdges.Count + " lines and " + pp.FaceEdges.Count + " faces read.");
            
            //set data for result processing
            result.rPoints = pp.Points.Count;
            result.rLines = pp.FixedEdges.Count;
            result.rFaces = pp.FaceEdges.Count;

            //return result - processing via ifc writer will be enabled 
            return result;
        }

        /// <summary>
        /// A function to recalculate a TIN using NetTopologySuite class library and BimGisCad TIN builder.
        /// </summary>
        /// <param name="dxfFile">The DXF file containing the TIN data (triangles, points and breaklines).</param>
        /// <param name="jSettings">The global settings for processing.</param>
        /// <returns>A result object which encapsulates the calculated TIN.</returns>
        public static Result RecalculateTin(DxfFile dxfFile, JsonSettings jSettings)
        {
            //Initialize TIN
            Tin tin;

            //Initialize result
            Result result = new Result();

            //Read out DXF data and create TIN via IfcTerrainTriangulator
            if (ReadDxfDtmData(dxfFile, jSettings, out List<double[]> dtmPointData, out List<double[]> constraintData))
            {
                tin = IfcTerrainTriangulator.CreateTin(dtmPointData, constraintData);
            }

            //Pass TIN to result and log
            result.Tin = tin;
            LogWriter.Add(LogType.info, "Reading DXF data successful.");
            result.rPoints = tin.Points.Count;
            result.rFaces = tin.NumTriangles;
            LogWriter.Add(LogType.debug, "Points: " + result.Tin.Points.Count + "; Triangles: " + result.Tin.NumTriangles + " processed");

            //Return the result as a TIN
            return result;
        }

        /// <summary>
        /// A function to calculate a TIN using NetTopologySuite class library and BimGisCad TIN builder.
        /// </summary>
        /// <param name="dxfFile">The DXF file containing the TIN data (triangles, points and breaklines).</param>
        /// <param name="jSettings">User input.</param>
        /// <returns>A result object which encapsulates the calculated TIN.</returns>
        public static Result CalculateTin(DxfFile dxfFile, JsonSettings jSettings)
        {
            //Initialize TIN
            Tin tin;

            //Initialize result
            Result result = new Result();

            //Read out DXF data and create TIN via IfcTerrainTriangulator
            if (jSettings.breakline.GetValueOrDefault() && ReadDxfDtmData(dxfFile, jSettings, out List<double[]> dtmPointData, out List<double[]> constraintData))
            {
                tin = IfcTerrainTriangulator.CreateTin(dtmPointData, constraintData);
            }
            else if (ReadDxfDtmData(dxfFile, jSettings, out dtmPointData, out constraintData))
            {
                tin = IfcTerrainTriangulator.CreateTin(dtmPointData);
            }

            //Pass TIN to result and log
            result.Tin = tin;
            LogWriter.Add(LogType.info, "Reading DXF data successful.");
            result.rPoints = tin.Points.Count;
            result.rFaces = tin.NumTriangles;
            LogWriter.Add(LogType.debug, "Points: " + result.Tin.Points.Count + "; Triangles: " + result.Tin.NumTriangles + " processed");

            //Return the result as a TIN
            return result;
        }

        /// <summary>
        /// An auxiliary function to read out point and line data from a DXF file according to the specified layers.
        /// </summary>
        /// <param name="dxfFile">The DXF file containing the DTM data (poins and lines).</param>
        /// <param name="jSettings">The global settings for processing.</param>
        /// <param name="dtmPointData">A list of double arrays. Each array contains the x-, y- and z-value of one point.</param>
        /// <param name="constraintData">A list of lists of double arrays. Each array contains the x-, y- and z-value of one point of one line.</param>
        /// <returns>True or false respectively depending on successful reading.</returns>
        public static bool ReadDxfDtmData(DxfFile dxfFile, JsonSettings jSettings, out List<double[]> dtmPointData, out List<double[]> constraintData)
        {
            //A list of point data
            dtmPointData = new List<double[]>();

            //A list of point data. Each double array represents a constraint vertex
            constraintData = new List<double[]>();

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
                        case DxfEntityType.Insert:
                            var dxfPoint = (DxfInsert)entity;
                            dtmPointData.Add(new double[] { dxfPoint.Location.X, dxfPoint.Location.Y, dxfPoint.Location.Z });
                            LogWriter.Add(LogType.verbose, "[DXF] Point set (x= " + dxfPoint.Location.X + "; y= " + dxfPoint.Location.Y + "; z= " + dxfPoint.Location.Z + ")");
                            break;
                        
                        //
                        case DxfEntityType.Face:
                            var dxfFace = (Dxf3DFace)entity;
                            dtmPointData.Add(new double[] { dxfFace.FirstCorner.X, dxfFace.FirstCorner.Y, dxfFace.FirstCorner.Z });
                            LogWriter.Add(LogType.verbose, "[DXF] Point set (x= " + dxfFace.FirstCorner.X + "; y= " + dxfFace.FirstCorner.Y + "; z= " + dxfFace.FirstCorner.Z + ")");
                            dtmPointData.Add(new double[] { dxfFace.SecondCorner.X, dxfFace.SecondCorner.Y, dxfFace.SecondCorner.Z });
                            LogWriter.Add(LogType.verbose, "[DXF] Point set (x= " + dxfFace.SecondCorner.X + "; y= " + dxfFace.SecondCorner.Y + "; z= " + dxfFace.SecondCorner.Z + ")");
                            dtmPointData.Add(new double[] { dxfFace.ThirdCorner.X, dxfFace.ThirdCorner.Y, dxfFace.ThirdCorner.Z });
                            LogWriter.Add(LogType.verbose, "[DXF] Point set (x= " + dxfFace.ThirdCorner.X + "; y= " + dxfFace.ThirdCorner.Y + "; z= " + dxfFace.ThirdCorner.Z + ")");
                            break;
                        
                        //dxf entity line --> add every line
                        case DxfEntityType.Line:

                            //catch entity line
                            var line = (DxfLine)entity;

                            //Get the first point data of the line
                            double p1X = line.P1.X * scale;
                            double p1Y = line.P1.Y * scale;
                            double p1Z = line.P1.Z * scale;

                            //Add point data to constraint data list
                            constraintData.Add(new double[] { line.P1.X * scale, line.P1.Y * scale, line.P1.Z * scale });

                            //Log first point of line
                            LogWriter.Add(LogType.verbose, "[DXF] Point set (x= " + p1X + "; y= " + p1Y + "; z= " + p1Z + ")");

                            //Get the second point data of the line
                            double p2X = line.P2.X * scale;
                            double p2Y = line.P2.Y * scale;
                            double p2Z = line.P2.Z * scale;

                            //Add point data to constraint data list
                            constraintData.Add(new double[] { p2X, p2Y, p2Z });

                            //Log second point of line
                            LogWriter.Add(LogType.verbose, "[DXF] Point set (x= " + p2X + "; y= " + p2Y + "; z= " + p2Z + ")");
                            break;

                        //dxf case polyline
                        case DxfEntityType.Polyline:

                            //get poly line entity
                            var polyline = (DxfPolyline)entity;

                            //go through all vertices
                            foreach (var vertex in polyline.Vertices)
                            {
                                //Prepare x-, y- and z-coordinates of the vertex
                                double x = vertex.Location.X * scale;
                                double y = vertex.Location.Y * scale;
                                double z = vertex.Location.Z * scale;

                                //Add vertex cooedinates to data list
                                constraintData.Add(new double[] { x, y, z });

                                //Log point data
                                LogWriter.Add(LogType.verbose, "[DXF] Point set (x= " + x + "; y= " + y + "; z= " + z + ")");
                            }
                            break;
                    }
                }
            }

            //Check if lists contain data and return true or false
            return CheckDtmDataLists(dtmPointData, constraintData);
        }

        /// <summary>
        /// A auxiliary function to check if lists contain data. 
        /// </summary>
        /// <param name="dtmPointData">A list of double arrays. Each array contains the x, y and z coordinate of a DTM point.</param>
        /// <param name="dtmLineData">A list of lists of double arrays. Each array contains the x, y and z coordinate of a DTM point.</param>
        /// <returns>True or false.</returns>
        public static bool CheckDtmDataLists(List<double[]> dtmPointData, List<double[]> dtmLineData)
        {
            if (dtmPointData.Count == 0)
            {
                LogWriter.Add(LogType.error, "[DXF] No point data found.");
                return false;
            }
            else if (dtmLineData.Count == 0)
            {
                LogWriter.Add(LogType.warning, "[DXF] Reading point data was successful. No line data found.");
                return true;
            }
            else
            {
                LogWriter.Add(LogType.info, "[DXF] Reading point and line data was successful.");
                return true;
            }
        }
        /*
        public static Result ReadDXFMESH(DxfFile dxfFile, JsonSettings jSettings)
        {
            //get boolean value if 2D or 3D
            bool is3d = jSettings.is3D;

            //minDist (currently not supported!)
            double minDist = jSettings.minDist;

            //get layer (TODO: multiple layer selection)
            string layer = jSettings.layer;

            double minDistSq = minDist * minDist;
            var result = new Result();
            if (!UnitToMeter.TryGetValue(dxfFile.Header.DefaultDrawingUnits, out double scale))
            {
                scale = 1.0;
            }
            var tin = new Mesh(is3d, minDist);

            foreach (var entity in dxfFile.Entities)
            {
                if (entity.Layer == layer && entity is Dxf3DFace face)
                {
                    var p1 = Point3.Create(face.FirstCorner.X * scale, face.FirstCorner.Y * scale, face.FirstCorner.Z * scale);
                    var p2 = Point3.Create(face.SecondCorner.X * scale, face.SecondCorner.Y * scale, face.SecondCorner.Z * scale);
                    var p3 = Point3.Create(face.ThirdCorner.X * scale, face.ThirdCorner.Y * scale, face.ThirdCorner.Z * scale);
                    var p4 = Point3.Create(face.FourthCorner.X * scale, face.FourthCorner.Y * scale, face.FourthCorner.Z * scale);
                    if (Vector3.Norm2(p4 - p3) < minDistSq)
                    {
                        int i1 = tin.AddPoint(p1);
                        int i2 = tin.AddPoint(p2);
                        int i3 = tin.AddPoint(p3);
                        try
                        {
                            tin.AddFace(new[] { i1, i2, i3 });
                        }
                        catch
                        {
                            
                        }
                    }
                }
            }

            //add to results (stats)
            result.rPoints = tin.Points.Count;
            result.rFaces = tin.FaceEdges.Count;

            result.Mesh = tin;


            
            return result;
        }
        */
    }
}