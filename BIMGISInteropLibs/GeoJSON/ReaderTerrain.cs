using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

//Transfer class (Result) for the reader (IFCTerrain + Revit)
using BIMGISInteropLibs.IfcTerrain;

//Logging
using BIMGISInteropLibs.Logging;
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

using Newtonsoft.Json;
namespace BIMGISInteropLibs.GeoJSON
{
    public static class ReaderTerrain
    {
        public static Result readGeoJson(Config config)
        {
            LogWriter.Add(LogType.verbose, "[GeoJSON] start reading...");

            //file reader (using NetTopologySuite to parse into NTS types)
            //
            var geoJsonData = File.ReadAllText(config.filePath);

            //init GeoJSON reader
            var serializer = GeoJsonSerializer.Create();

            GeometryCollection geometry;

            using (var stringReader = new StringReader(geoJsonData))
            using (var jsonReader = new JsonTextReader(stringReader))
            {
                geometry = serializer.Deserialize<GeometryCollection>(jsonReader);
            }

            if (geometry.IsEmpty)
            {
                LogWriter.Add(LogType.error, "[GeoJSON] '"+config.fileName+"' could not be deserialized (read).");
                return null;
            }
            else
            {
                if(geometry.OgcGeometryType.Equals(OgcGeometryType.MultiPolygon))
                {
                    MultiPolygon multiPolygon = geometry as MultiPolygon;



                }


            }

            //TODO
            return null;
        }
    }
}
