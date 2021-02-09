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

//Transfer class for the reader (IFCTerrain + Revit)
using BIMGISInteropLibs.IFCTerrain;

namespace BIMGISInteropLibs.DXF
{
    
    class ReaderTerrain
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
        /// <returns></returns>
        public static bool ReadFile(string fileName, out DxfFile dxfFile)
        {
            //try to open DXF file via FileStream
            try
            {
                //fileName will be given by Interface (IFCTerrainGUI or ~Revit~)
                using (var fs = new FileStream(fileName, FileMode.Open))
                {
                    dxfFile = DxfFile.Load(fs);
                    return true;
                }
            }
            //[BAD SOLUTION] - return of error is missing!
            //if it can't be opend
            catch
            {
                dxfFile = null;
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
        /// <returns></returns>
        public static Result ReadDXFTin(DxfFile dxfFile, string layer, string breaklinelayer, double minDist, bool breakline)
        {
            //[TODO]
            //use is3d is not implemented right now

            double minDistSq = minDist * minDist;
            
            //new result for handover of the tin (and mesh)
            var result = new Result();
            
            //Review, scale shouldn't be static??? [TODO]
            if (!UnitToMeter.TryGetValue(dxfFile.Header.DefaultDrawingUnits, out double scale))
            {
                scale = 1.0;
            }

            //TIN-Builder initalise
            var tinB = Tin.CreateBuilder(true);

            //Dictionary for "saving" breaklines
            Dictionary<int, Line3> breaklines = new Dictionary<int, Line3>(); 

            //Logger logger = LogManager.GetCurrentClassLogger(); 

            int processedBreaklines = 0;

            //PNR counter to increment the point number
            int pnr = 0;

            //loop to go through all entities of the DXF file
            foreach (var entity in dxfFile.Entities)
            {
                //Check if the layer to be processed corresponds to the "current" entity
                //furthermore it is checked if it is a face
                if (entity.Layer == layer && entity is Dxf3DFace face)
                {
                    //query the four points of the face and pass them to variable p1 ... p4 passed
                    var p1 = Point3.Create(face.FirstCorner.X * scale, face.FirstCorner.Y * scale, face.FirstCorner.Z * scale);
                    var p2 = Point3.Create(face.SecondCorner.X * scale, face.SecondCorner.Y * scale, face.SecondCorner.Z * scale);
                    var p3 = Point3.Create(face.ThirdCorner.X * scale, face.ThirdCorner.Y * scale, face.ThirdCorner.Z * scale);
                    var p4 = Point3.Create(face.FourthCorner.X * scale, face.FourthCorner.Y * scale, face.FourthCorner.Z * scale);
                    if (Vector3.Norm2(p4 - p3) < minDistSq)
                    {
                        //Add points & increment one point number at a time
                        tinB.AddPoint(pnr++, p1);
                        tinB.AddPoint(pnr++, p2);
                        tinB.AddPoint(pnr++, p3);

                        //Loop to create the triangle
                        for (int i = pnr - 3; i < pnr; i++)
                        {
                            tinB.AddTriangle(i++, i++, i++);
                        }
                    }
                }

                //Check if selected layer of break edges is present in current entity
                //furthermore it is checked if the functionality should be used at all
                if (entity.Layer == breaklinelayer && breakline == true)
                {
                    switch (entity.EntityType)
                    {
                        /*case DxfEntityType.Vertex: //Point removed
                            var vtx = (DxfVertex)entity;
                            pp_bl.AddPoint(Point3.Create(vtx.Location.X, vtx.Location.Y, vtx.Location.Z));
                            break;*/
                        case DxfEntityType.Line: //Linie
                            var line = (DxfLine)entity;
                            Point3 p1 = Point3.Create(line.P1.X * scale, line.P1.Y * scale, line.P1.Z * scale);
                            Point3 p2 = Point3.Create(line.P2.X * scale, line.P2.Y * scale, line.P2.Z * scale);
                            Vector3 v12 = Vector3.Create(p2);
                            Direction3 d12 = Direction3.Create(v12, scale);
                            Line3 l = Line3.Create(p1, d12);
                            try
                            {
                                breaklines.Add(processedBreaklines++, l);
                            }
                            catch
                            {
                                processedBreaklines++;
                            }
                            break;
                            /*case DxfEntityType.Polyline: //Bögen
                                var poly = (DxfPolyline)entity;
                                int last = -1;
                                foreach (var v in poly.Vertices)
                                {
                                    int curr = pp_bl.AddPoint(Point3.Create(v.Location.X * scale, v.Location.Y * scale, v.Location.Z * scale));
                                    if (last >= 0)
                                    {
                                        pp_bl.FixEdge(last, curr);
                                    }
                                    last = curr;
                                }
                            break;*/
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
            
            //Result describe
            result.Tin = tin;

            //Describe result for break edges
            result.Breaklines = breaklines;

            //logging [TODO]
            //logger.Info("Reading DXF-data successful");
            //logger.Info(result.Tin.Points.Count() + " points; " + result.Tin.NumTriangles + " triangels processed");

            //Transferring the result: for further processing in IFCTerrain or Revit
            return result;
        }
    }
}
