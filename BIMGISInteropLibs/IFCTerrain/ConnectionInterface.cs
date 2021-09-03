using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;

//embed IFC
using BIMGISInteropLibs.IFC;    //IFC-Writer

//embed for Logging
using BIMGISInteropLibs.Logging; //logging
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

//IxMilia: for processing dxf files
using IxMilia.Dxf;

using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace BIMGISInteropLibs.IfcTerrain
{
    public class ConnectionInterface
    {
        /// <summary>
        /// ConnectionInterface between file reader and ifc writer
        /// </summary>
        public bool mapProcess(Config config, JsonSettings_DIN_SPEC_91391_2 jSettings_DIN91931, JsonSettings_DIN_18740_6 jSettings_DIN18740)
        {
            LogWriter.Add(LogType.info, "Processing-Protocol for IFCTerrain");
            LogWriter.Add(LogType.info, "--------------------------------------------------");
            LogWriter.Add(LogType.verbose, "Mapping process started.");

            //check if file exists otherwise return allready here false
            if (!File.Exists(config.filePath)) 
            {
                LogWriter.Add(LogType.error, "[READER] File not found at path: " + config.filePath);
                return false;
            }
            LogWriter.Add(LogType.verbose, "[READER] can read file: " + config.filePath);

            //The processing is basically done by a reader and a writer (these are included in the corresponding regions)
            #region reader
            //initalize transfer class
            var result = new Result();

            //In the following a mapping is made on the basis of the data type, so that the respective reader is called up
            switch (config.fileType)
            {
                case IfcTerrainFileType.DXF:
                    //read dxfFile via filepath
                    if(DXF.ReaderTerrain.readFile(config.filePath, out DxfFile dxfFile))
                    {
                        //read dtm
                        result = DXF.ReaderTerrain.readDxf(config, dxfFile);
                    }
                    break;

                case IfcTerrainFileType.LandXML:
                    result = LandXML.ReaderTerrain.readDtmData(config);
                    break;

                case IfcTerrainFileType.CityGML:
                    result = CityGML.CityGMLReaderTerrain.readTin(config);
                    break;

                case IfcTerrainFileType.Grafbat:
                    result = GEOgraf.ReadOUT.readOutData(config);
                    break;

                case IfcTerrainFileType.PostGIS:
                    result = PostGIS.ReaderTerrain.readPostGIS(config);
                    break;

                case IfcTerrainFileType.Grid:
                    result = ElevationGrid.ReaderTerrain.readGrid(config);
                    break;

                case IfcTerrainFileType.REB:
                    result = REB.ReaderTerrain.readDtm(config);
                    break;

                case IfcTerrainFileType.GeoJSON:
                    result = GeoJSON.ReaderTerrain.readGeoJson(config);
                    break;
                    //breakline processing
                    //(first check and with the method breaklines will be processed)
                    /*
                    if (config.breakline.GetValueOrDefault()
                        && GeoJSON.ReaderTerrain.readBreakline(config, result))
                    {
                        LogWriter.Add(LogType.info, "[READER] Breaklines are considered!");
                    };
                    break;
                    */
            }

            //error handling
            if (result == null)
            {
                LogWriter.Add(LogType.error, "[READER] File reading failed (result is null) - processing canceld!");
                return false;
            }
            else if(result.pointList == null)
            {
                LogWriter.Add(LogType.error, "[READER] File reading failed (point list is empty) - processing canceld!");
                return false;
            }

            //so that from the reader is passed to respective "classes"
            LogWriter.Add(LogType.debug, "Reading file completed.");

            #endregion reader
            if(result.currentConversion == DtmConversionType.conversion)
            {
                LogWriter.Add(LogType.info, "Faces readed: " + result.triMap.Count + " Points readed: " + result.pointList.Count);

                //log
                LogWriter.Add(LogType.info, "Processing via delaunay triangulation is not necessary.");

                //set point list to multi points
                Point[] points = result.pointList.ToArray();

                //create point collection
                MultiPoint pointCollection = new MultiPoint(points);

                //create coord list from points
                CoordinateList cL = new CoordinateList();
                foreach (var point in points)
                {
                    //add point as coordinate
                    cL.Add(point.Coordinate);
                }
                result.coordinateList = cL;

                //get centroid from point collection
                var centroid = NetTopologySuite.Algorithm.Centroid.GetCentroid(pointCollection);

                //set origion for processing exchange
                result.origin = centroid;
            }

            //if index map is not aviable triangulate
            else
            {
                //dtm processing via delaunay triangulation
                Triangulator.DelaunayTriangulation.triangulate(result);
            }

            //from here are the IFC writers
            #region writer
            #region small export of geometry as WKT/ WKB / GML (in a txt file)
            if (config.outFileType != IfcFileType.Step
                && config.outFileType != IfcFileType.ifcXML
                && config.outFileType != IfcFileType.ifcZip)
            {
                if(writeToText(config, result))
                {
                    LogWriter.Add(LogType.info, "File created please check file path: "
                        + Environment.NewLine + Path.GetDirectoryName(config.destFileName));
                    return true;
                }
                else { return false; }
            }
            #endregion
            //set write input
            var writeInput = utils.setWriteInput(result, config);

            //init empty model
            Xbim.Ifc.IfcStore model = null;

            //region for ifc writer control
            switch (config.outIFCType)
            {
                case IfcVersion.IFC2x3:
                    model = IFC.Ifc2x3.Store.Create(
                        result,
                        config,
                        writeInput,
                        jSettings_DIN91931,
                        jSettings_DIN18740);
                    break;

                case IfcVersion.IFC4:
                    model = config.geoElement.GetValueOrDefault()
                        ? IFC.Ifc4.Geo.Create(
                            result,
                            config,
                            writeInput,
                            jSettings_DIN91931,
                            jSettings_DIN18740)
                        : IFC.Ifc4.Store.Create(
                            result,
                            config,
                            writeInput,
                            jSettings_DIN91931,
                            jSettings_DIN18740);
                    break;

                case IfcVersion.IFC4dot3:
                    throw new NotImplementedException();
            }

            //access to file writer
            utils.WriteFile(model, config);

            //logging file info
            LogWriter.Add(LogType.info, "IFC file writen: " + config.destFileName);
            LogWriter.Add(LogType.verbose, "IFC writer completed.");
            LogWriter.Add(LogType.info, "--------------------------------------------------");
            #endregion writer
            
            return true;
        }

        private static bool writeToText(Config config, Result result)
        {
            //xml writer settings
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Encoding = Encoding.UTF8;
            xmlWriterSettings.Indent = true;
            xmlWriterSettings.OmitXmlDeclaration = true;
            xmlWriterSettings.NewLineOnAttributes = true;

            if(result.currentConversion == DtmConversionType.conversion)
            {
                List<Polygon> polygons = new List<Polygon>();

                var pointList = result.pointList.ToArray();

                foreach(var tri in result.triMap)
                {
                    List<Coordinate> coordinates = new  List<Coordinate>();

                    foreach(var pointIndex in tri.triValues)
                    {
                        Coordinate c = pointList[pointIndex].Coordinate;
                        coordinates.Add(c);
                    }

                    //
                    coordinates.Add(coordinates.First());
                    //
                    LinearRing linearRing = new LinearRing(coordinates.ToArray());
                    //
                    polygons.Add(new Polygon(linearRing));
                }

                GeometryCollection geometryCollection = new GeometryCollection(polygons.ToArray());

                result.geomStore = geometryCollection;
            }


            var exportGeom = result.geomStore;
            try
            {
                LogWriter.Add(LogType.info, "Export geometry as: " + config.outFileType.ToString());
                switch (config.outFileType)
                {
                    case IfcFileType.wkt:
                        using (StreamWriter sw = new StreamWriter(Path.ChangeExtension(config.destFileName, "txt")))
                        {
                            WKTWriter wktWriter = new WKTWriter(3);
                            
                            //set that formatted is true formatted 
                            wktWriter.Formatted = true;

                            //set to xyz (so z value will not be dropped)
                            wktWriter.OutputOrdinates = Ordinates.XYZ;
                            wktWriter.WriteFormatted(exportGeom, sw);
                        }
                        return true;
                    case IfcFileType.wkb:
                        using (StreamWriter sw = new StreamWriter(Path.ChangeExtension(config.destFileName, "txt")))
                        {
                            //set byte order
                            ByteOrder byteOrder = ByteOrder.BigEndian;
                            
                            //set wkb writer (without dropping z value)
                            WKBWriter wkbWriter = new WKBWriter(byteOrder, true, true);
                           
                            var bytes = wkbWriter.Write(exportGeom);
                            sw.BaseStream.Write(bytes, 0, bytes.Length);
                        }
                        return true;

                    case IfcFileType.gml2:
                        //
                        NetTopologySuite.IO.GML2.GMLWriter gml2Writer
                            = new NetTopologySuite.IO.GML2.GMLWriter();

                        //
                        var xmlWriterGml2 = XmlWriter.Create(Path.ChangeExtension(config.destFileName, "gml"), xmlWriterSettings);
                        gml2Writer.Write(exportGeom, xmlWriterGml2);
                        
                        return true;

                    case IfcFileType.gml3:

                        //
                        NetTopologySuite.IO.GML3.GML3Writer gml3Writer 
                            = new NetTopologySuite.IO.GML3.GML3Writer();

                        

                        //
                        var xmlWriterGml3 = XmlWriter.Create(
                            Path.ChangeExtension(config.destFileName, "gml"), xmlWriterSettings);
                        
                        gml3Writer.Write(exportGeom, xmlWriterGml3);
                      
                        return true;

                    default: return false;
                }
            }
            catch (Exception ex)
            {
                LogWriter.Add(LogType.error, ex.Message);
                return false;
            }
        }
    }
}