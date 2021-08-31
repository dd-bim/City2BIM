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

using Newtonsoft.Json;
using GeoJSON.Net.Geometry;
using gn = GeoJSON.Net;
using NTSGeometry = NetTopologySuite.Geometries;

//
using BIMGISInteropLibs.Geometry;

namespace BIMGISInteropLibs.GeoJSON
{
    public static class ReaderTerrain
    {
        public static Result readGeoJson(Config config)
        {
            //
            config.fileName = Path.GetFileName(config.filePath);

            LogWriter.Add(LogType.verbose, "[GeoJSON] start reading...");

            //init result
            Result result = new Result();

            //file reader (using NetTopologySuite to parse into NTS types)
            //
            var geojsonString = File.ReadAllText(config.filePath);

            //init storage classes
            var points = new HashSet<NTSGeometry.Point>();
            var triMap = new HashSet<Triangulator.triangleMap>();


            //switch between different geom type (from user selection)
            LogWriter.Add(LogType.info, "[GeoJSON] parsing geometry type: " + config.geometryType);

            switch (config.geometryType)
            {
                case GeometryType.MultiPoint:


                    dynamic multiPoint;
                    try
                    {
                        multiPoint = JsonConvert.DeserializeObject<MultiPoint>(geojsonString);
                    }
                    catch (Exception ex)
                    {
                        LogWriter.Add(LogType.error, "[GeoJSON] " + ex.Message);
                        break;
                    }

                    LogWriter.Add(LogType.debug, "[GeoJSON] readed coordinates: " + multiPoint.Coordinates.Count);

                    //
                    foreach(var point in multiPoint.Coordinates)
                    {
                        var p = pointToNts(point);
                        points.Add(p);
                    }

                    if(points.Count.Equals(0))
                    {
                        LogWriter.Add(LogType.error, "[GeoJSON] geometry is empty. Please check the input geometry type!");
                        break;
                    }

                    result.currentConversion = DtmConversionType.points;
                    result.pointList = points.ToList();
                    
                    return result;

                case GeometryType.MultiPolygon:

                    dynamic multiPolygon;
                    try
                    {
                        multiPolygon = JsonConvert.DeserializeObject<MultiPolygon>(geojsonString);
                    }
                    catch (Exception ex)
                    {
                        LogWriter.Add(LogType.error, "[GeoJSON] " + ex.Message);
                        break;
                    }

                    if (multiPolygon.Coordinates.Count.Equals(0))
                    {
                        LogWriter.Add(LogType.error, "[GeoJSON] geometry is empty. Please check the input geometry type!");
                        break;
                    }

                    foreach(var polygon in multiPolygon.Coordinates)
                    {
                        readPolygon(points, triMap, polygon);
                    }
                    result.currentConversion = DtmConversionType.conversion;
                    result.pointList = points.ToList();
                    result.triMap = triMap;

                    LogWriter.Add(LogType.info, "[GeoJSON] readed (unique) points: " + points.Count);
                    LogWriter.Add(LogType.info, "[GeoJSON] readed polygons (triangles): " + triMap.Count);
                    return result;

                case GeometryType.GeometryCollection:

                    dynamic geometryCollection;
                    try
                    {
                        geometryCollection = JsonConvert.DeserializeObject<GeometryCollection>(geojsonString);
                    }
                    catch(Exception ex)
                    {
                        LogWriter.Add(LogType.error, "[GeoJSON] " + ex.Message);
                        break;
                    }

                    if (geometryCollection.Geometries.Count.Equals(0))
                    {
                        LogWriter.Add(LogType.error, "[GeoJSON] geometry is empty. Please check the input geometry type!");
                        break;
                    }

                    foreach(var geom in geometryCollection.Geometries)
                    {
                        if (geom.Type == gn.GeoJSONObjectType.Polygon)
                        {
                            readPolygon(points, triMap, geom as Polygon);
                        }
                    }
                    
                    result.currentConversion = DtmConversionType.conversion;
                    result.pointList = points.ToList();
                    result.triMap = triMap;
                    
                    LogWriter.Add(LogType.info, "[GeoJSON] readed (unique) points: " + points.Count);
                    LogWriter.Add(LogType.info, "[GeoJSON] readed polygons (triangles): " + triMap.Count);
                    return result;
            }

            //error message + return empty result (processing will stop)
            LogWriter.Add(LogType.error, "[GeoJSON] processing failed!");
            return null;
        }

        /// <summary>
        /// convert point (GeoJSON) to point (NTS)
        /// </summary>
        private static NTSGeometry.Point pointToNts(Point p)
        {
            var x = p.Coordinates.Longitude;
            var y = p.Coordinates.Latitude;
            var z = p.Coordinates.Altitude.GetValueOrDefault();

            return new NTSGeometry.Point(x, y, z);
        }

        /// <summary>
        /// read polygon
        /// </summary>
        private static void readPolygon(HashSet<NTSGeometry.Point> points, HashSet<Triangulator.triangleMap> triMap, Polygon polygon)
        {
            var lineString = polygon.Coordinates[0];

            //
            int[] tri = new int[3];

            //
            if (lineString.Coordinates[0].Equals(lineString.Coordinates[3])
                && lineString.Coordinates.Count == 4)
            {

                //
                int i = 0;
                do
                {
                    Point p = new Point(lineString.Coordinates[i]);
                    var pt = pointToNts(p);

                    int j = terrain.addPoint(points, pt);

                    tri[i++] = j;

                } while (i < 3);
            }

            triMap.Add(new Triangulator.triangleMap()
            {
                triNumber = triMap.Count,
                triValues = tri
            });
        }
    }
}
