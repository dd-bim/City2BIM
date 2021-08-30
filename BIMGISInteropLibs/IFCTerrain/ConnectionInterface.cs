using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

//embed IFC
using BIMGISInteropLibs.IFC;    //IFC-Writer

//embed for Logging
using BIMGISInteropLibs.Logging; //logging
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

//IxMilia: for processing dxf files
using IxMilia.Dxf;

using NetTopologySuite.Geometries;

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
            #region [TODO] small export of geometry as WKT (in a txt file)
            if (config.outFileType == IfcFileType.text)
            {
                var exportGeom = result.geomStore;
                using (StreamWriter sw = new StreamWriter(Path.GetFullPath(config.destFileName)+".txt"))
                {
                    //NetTopologySuite.IO.WKTWriter writer = new NetTopologySuite.IO.WKTWriter();
                    sw.WriteLine(exportGeom);
                }
                return true;
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
    }
}