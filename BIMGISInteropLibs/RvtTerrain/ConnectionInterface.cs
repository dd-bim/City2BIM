using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//used for result class (may update to seperate class for rvtTerrain)
using BIMGISInteropLibs.IfcTerrain;

using geojson = BIMGISInteropLibs.GeoJSON.ReaderTerrain;

//IxMilia: for processing dxf files
using IxMilia.Dxf;

using Serilog;

namespace BIMGISInteropLibs.RvtTerrain
{
    /// <summary>
    /// connection between file reader & DTM2BIM writer
    /// </summary>
    public class ConnectionInterface
    {
        /// <summary>
        /// file reading / tin build process
        /// </summary>
        /// <param name="config">setting to config file processing and conversion process</param>
        public static Result mapProcess(Config config)
        {
            //init transfer class (DTM2BIM)
            Result resTerrain = new Result();
            
            switch (config.fileType)
            {
                case IfcTerrainFileType.DXF:
                    //read dxfFile via filepath
                    if (DXF.ReaderTerrain.readFile(config.filePath, out DxfFile dxfFile))
                    {
                        //read dtm
                        resTerrain = DXF.ReaderTerrain.readDxf(config, dxfFile);
                    }
                    break;

                case IfcTerrainFileType.LandXML:
                    resTerrain = LandXML.ReaderTerrain.readDtmData(config);
                    break;

                case IfcTerrainFileType.CityGML:
                    resTerrain = CityGML.CityGMLReaderTerrain.readTin(config);
                    break;

                case IfcTerrainFileType.Grafbat:
                    resTerrain = GEOgraf.ReadOUT.readOutData(config);
                    break;

                case IfcTerrainFileType.PostGIS:
                    resTerrain = PostGIS.ReaderTerrain.readPostGIS(config);
                    break;

                case IfcTerrainFileType.Grid:
                    resTerrain = ElevationGrid.ReaderTerrain.readGrid(config);
                    break;

                case IfcTerrainFileType.REB:
                    resTerrain = REB.ReaderTerrain.readDtm(config);
                    break;

                case IfcTerrainFileType.GeoJSON:
                    resTerrain = geojson.readGeoJson(config);
                    break;
            }

            //error handling
            if (resTerrain == null)
            {
                Log.Error("[READER] File reading failed (result is null) - processing canceld!");
                return null;
            }
            else if (resTerrain.pointList == null || resTerrain.pointList.Count.Equals(0))
            {
                Log.Error("[READER] File reading failed(point list is empty) - processing canceld!");
                return null;
            }

            if(resTerrain.currentConversion != DtmConversionType.conversion)
            {
                Log.Information("A Delaunay triangulation will be calculated...");
                Triangulator.DelaunayTriangulation.triangulate(resTerrain);
            }

            Log.Information("File readed. Result => Faces (Triangles): " + resTerrain.triMap.Count + " Points: " + resTerrain.pointList);

            return resTerrain;
        }
    }
}
