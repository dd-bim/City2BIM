using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO; //file handling

//IxMilia - Bib for processing DXF files
using IxMilia.Dxf;          //file handling
using IxMilia.Dxf.Entities; //entites in dxf file (used for processing of faces)

//Transfer class for the reader (IFCTerrain + Revit)
using BIMGISInteropLibs.IfcTerrain;

//Logging
using BIMGISInteropLibs.Logging;
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

using NetTopologySuite.Geometries;
using BIMGISInteropLibs.Geometry;

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
        public static bool readFile(string fileName, out DxfFile dxfFile)
        {
            dxfFile = null;

            //try to open DXF file via FileStream
            try
            {
                //fileName will be given by Interface (IFCTerrainGUI or ~Revit~)
                using (var fs = new FileStream(fileName, FileMode.Open))
                {
                    dxfFile = DxfFile.Load(fs);

                    LogWriter.Entries.Add(new LogPair(LogType.verbose, "[DXF] file has been read (" + fileName + ")"));

                    return true;
                }
            }
            
            //if it can't be opend
            catch(Exception ex)
            {
                LogWriter.Entries.Add(new LogPair(LogType.error, ex.Message));
                return false;
            }
        }

        public static Result readDxf(Config config, DxfFile dxfFile)
        {
            if(readDtm(dxfFile, config, out Result result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        
        public static bool readDtm(DxfFile dxfFile, Config config, out Result dxfResult)
        {
            //new result for handover of the dtm data
            dxfResult = new Result();
            
            //Review, scale shouldn't be static
            if (!UnitToMeter.TryGetValue(dxfFile.Header.DefaultDrawingUnits, out double scale))
            {
                //if scale can not be read readed
                scale = 1.0;
                LogWriter.Add(LogType.warning, "[DXF] Scale can not be readed!");
                LogWriter.Add(LogType.warning, "[DXF] Scale has been set to: " + scale.ToString());
            }

            if (!config.readPoints.GetValueOrDefault()) 
            {
                LogWriter.Add(LogType.debug, "[DXF] Reading 3DFaces...");

                config.minDist = 1;

                //read faces
                readFaces(dxfFile, config.layer, scale, config.minDist, dxfResult);
            } 
            else
            {
                LogWriter.Add(LogType.debug, "[DXF] Reading points...");
                //read points
                readPoints(dxfFile, config.layer, scale, dxfResult);
            }
            
            //if breaklines should be processed
            if (config.breakline.GetValueOrDefault())
            {
                //read faces
                readBreaklines(dxfFile, config.breakline_layer, scale, dxfResult);
            }

            //logging
            LogWriter.Add(LogType.info, "[DXF] data reading successfully.");
            
            //return true --> reading sucessfully
            return true;
        }

        /// <summary>
        /// reading faces of dxf file
        /// </summary>
        private static void readFaces(DxfFile dxfFile, string dxfLayer, double scale, double minDist, Result dxfResult)
        {
            //set conversion type (needed for processing via NTS
            dxfResult.currentConversion = DtmConversionType.conversion;

            var triMap = new HashSet<Triangulator.triangleMap>();
            var pointList = new HashSet<Point>();

            LogWriter.Add(LogType.verbose, "[DXF] read faces ...");

            //loop to go through all entities of the DXF file
            foreach (var entity in dxfFile.Entities)
            {
                //Check if the layer to be processed corresponds to the "current" entity
                //furthermore it is checked if it is a face
                if (entity.Layer == dxfLayer && entity is Dxf3DFace face)
                {
                    //query the four points of the face and pass them to variable p1 ... p4 passed
                    //set point
                    int p1 = terrain.addPoint(pointList, new Point(face.FirstCorner.X * scale, face.FirstCorner.Y * scale, face.FirstCorner.Z * scale));
                    int p2 = terrain.addPoint(pointList, new Point(face.SecondCorner.X * scale, face.SecondCorner.Y * scale, face.SecondCorner.Z * scale));
                    int p3 = terrain.addPoint(pointList, new Point(face.ThirdCorner.X * scale, face.ThirdCorner.Y * scale, face.ThirdCorner.Z * scale));

                    triMap.Add(new Triangulator.triangleMap()
                    {
                        triNumber = triMap.Count,
                        triValues = new int[] { p1, p2, p3 }
                    });

                    //log
                    LogWriter.Add(LogType.verbose, "[DXF] Triangle set.");

                    //CoordinateZ p4 = new CoordinateZ(face.FourthCorner.X * scale, face.FourthCorner.Y * scale, face.FourthCorner.Z * scale);

                    /*
                    Coordinate[] coords = new Coordinate[] { p1, p2, p3, p1 };
                    Polygon triangle = new Polygon(new LinearRing(coords));
                    //add polygon to list
                    triangleList.Add(triangle);
                    */
                }
            }

            //set to result
            dxfResult.triMap = triMap;
            dxfResult.pointList = pointList.ToList();
        }
    
        /// <summary>
        /// reading point data from dxf file and layer
        /// </summary>
        private static void readPoints(DxfFile dxfFile, string dxfLayer, double scale, Result dxfResult)
        {
            //set conversion type (needed for processing via NTS
            dxfResult.currentConversion = DtmConversionType.points;

            var pointList = new HashSet<Point>();

            LogWriter.Add(LogType.verbose, "[DXF] read points ...");

            //loop to go through all entities of the DXF file
            foreach (var entity in dxfFile.Entities)
            {
                //Check if the layer to be processed corresponds to the "current" entity
                //furthermore it is checked if it is a face
                if (entity.Layer == dxfLayer && entity is DxfInsert point)
                {
                    //get point data
                    var dxfPoint = new Point(point.Location.X * scale, point.Location.Y * scale, point.Location.Z * scale);
                    
                    //set point to point list
                    pointList.Add(dxfPoint);

                    //log
                    LogWriter.Add(LogType.verbose, "[DXF] Point data added.");
                }
            }

            //set to result
            dxfResult.pointList = pointList.ToList();
        }

        /// <summary>
        /// reading breaklines in an dxf file via current settings 
        /// </summary>
        private static void readBreaklines(DxfFile dxfFile, string breaklineLayer , double scale, Result res)
        {
            //set conversion type --> breaklines will be processed
            res.currentConversion = DtmConversionType.points_breaklines;

            var lines = new List<LineString>();

            LogWriter.Add(LogType.verbose, "[DXF] read breaklines ...");

            foreach (var entity in dxfFile.Entities)
            {
                if(entity.Layer == breaklineLayer && entity.EntityType.Equals(DxfEntityType.Line))
                {
                    var dxfLine = (DxfLine)entity;
                    
                    CoordinateZ pA = new CoordinateZ(dxfLine.P1.X * scale, dxfLine.P1.Y * scale, dxfLine.P1.Z * scale);
                    CoordinateZ pE = new CoordinateZ(dxfLine.P2.X * scale, dxfLine.P2.Y * scale, dxfLine.P2.Z * scale);
                    LineString line = new LineString(new CoordinateZ[] { pA, pE });

                    lines.Add(line);
                    LogWriter.Add(LogType.verbose, "[DXF] Breakline set.");
                }
            }
            res.lines = lines;
        }
    }


}