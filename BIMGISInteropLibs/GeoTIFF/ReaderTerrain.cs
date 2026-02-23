using BIMGISInteropLibs.IfcTerrain;
//Logging
using BIMGISInteropLibs.Logging;
using NetTopologySuite.Geometries;
using OSGeo.GDAL;
using OSGeo.OSR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain;

namespace BIMGISInteropLibs.GeoTIFF
{
    public class ReaderTerrain
    {
        public static Result readGeoTIFF(Config config)
        {
            if (readPointData(config, out Result result))
            {
                //return result class
                return result;
            }
            else
            {
                //do not store any results --> processing will be canceld!
                return null;
            }
        }

        private static bool readPointData(Config config, out Result result)
        {
            //Log successful reading
            LogWriter.Add(LogType.info, "[Grid] reading grid file (" + config.fileName + ")");

            //init result
            result = new Result();

            //Initialize list for DTM point data
            var pointList = new List<Point>();

            //set conversion type
            result.currentConversion = DtmConversionType.points;
            try
            {                 
                Gdal.AllRegister();

                using (Dataset ds = Gdal.Open(config.filePath, Access.GA_ReadOnly))
                {
                    if (ds == null) throw new InvalidOperationException("[GDAL] Could not open file: " + config.fileName);

                    // GeoTransform: [0]=originX, [1]=pixelWidth, [2]=rotX, [3]=originY, [4]=rotY, [5]=-pixelHeight (oft)
                    double[] gt = new double[6];
                    ds.GetGeoTransform(gt);

                    int width = ds.RasterXSize;
                    int height = ds.RasterYSize;
                    Band band = ds.GetRasterBand(1);

                    double noDataValue = double.NaN;
                    int hasNoData = 0;
                    band.GetNoDataValue(out noDataValue, out hasNoData);

                    DataType dt = band.DataType;
                    switch (dt)
                    {
                        case DataType.GDT_Float32:
                            ProcessRows<float>(band, width, height, gt, noDataValue, hasNoData, pointList);
                            break;
                        case DataType.GDT_Float64:
                            ProcessRows<double>(band, width, height, gt, noDataValue, hasNoData, pointList);
                            break;
                    }
                }

                 //logging
                 LogWriter.Add(LogType.info, "[GeoTIFF] Reading GeoTIFF data successful.");

                 result.currentConversion = DtmConversionType.points;
                 result.pointList = pointList.ToList();
                 //Result handed over
                 return true;
            }
            catch (Exception ex)
            {
                //error logging
                LogWriter.Add(LogType.error, "[GeoTIFF] file (" + config.fileName + ") could not be read! Error: " + ex.Message);
                return false;
            }
        }

        private static void ProcessRows<T>(Band band, int width, int height, double[] gt, double noDataValue, int hasNoData, List<Point> pointList)
            where T : struct
        {
            T[] scanline = new T[width];
            double tol = 1e-9;

            for (int row = 0; row < height; row++)
            {
                // use correct overload for float[] or double[]
                if (typeof(T) == typeof(float))
                {
                    band.ReadRaster(0, row, width, 1, scanline as float[], width, 1, 0, 0);
                }
                else if (typeof(T) == typeof(double))
                {
                    band.ReadRaster(0, row, width, 1, scanline as double[], width, 1, 0, 0);
                }
                else
                {
                    throw new NotSupportedException($"[GeoTIFF] Datatype {typeof(T)} not supported for TIFF.");
                }

                for (int col = 0; col < width; col++)
                {
                    double val = Convert.ToDouble(scanline[col]);

                    if (hasNoData == 1 && !double.IsNaN(noDataValue) && Math.Abs(val - noDataValue) < tol)
                    {
                        continue;
                    }

                    double x = gt[0] + col * gt[1] + row * gt[2];
                    double y = gt[3] + col * gt[4] + row * gt[5];
                    double z = val;

                    pointList.Add(new Point(x, y, z));
                }
            }
        }
    }
}
