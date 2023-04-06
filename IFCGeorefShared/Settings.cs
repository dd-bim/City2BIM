using OSGeo.GDAL;
using OSGeo.OGR;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IFCGeorefShared
{
    public class Settings
    {
        private readonly string regionPath;
        public string RegionPath { get { return regionPath; } }
        
        private static Settings? instance;

        private Settings() 
        {
            this.regionPath = @".\Shapefiles\Regions\ne_10m_admin_1_states_provinces.shp";
        }

        public static Settings GetSettings()
        {
            if (instance == null)
            {
                instance = new Settings();
            }
            return instance;
        }

        public static bool configureOgr()
        {
            string executingAssemblyFile = new Uri(Assembly.GetExecutingAssembly().GetName().CodeBase!).LocalPath;
            string executingDirectory = Path.GetDirectoryName(executingAssemblyFile)!;

            if (string.IsNullOrEmpty(executingDirectory))
            {
                Log.Error("cannot get executing directory");
                throw new InvalidOperationException("cannot get executing directory");
            }


            string gdalPath = Path.Combine(executingDirectory, "gdal");
            string nativePath = Path.Combine(gdalPath, "x64");
            if (!Directory.Exists(nativePath))
            {
                Log.Error("Did not found GDAL-Directory!");
                throw new DirectoryNotFoundException($"GDAL native directory not found at '{nativePath}'");
            }

            if (!File.Exists(Path.Combine(nativePath, "gdal_wrap.dll")))
            {
                Log.Error("Could not find gdal_wrap.dll in directory: " + nativePath);
                throw new FileNotFoundException(
                    $"GDAL native wrapper file not found at '{Path.Combine(nativePath, "gdal_wrap.dll")}'");
            }


            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + nativePath);

            // Set the additional GDAL environment variables.
            string gdalData = Path.Combine(gdalPath, "data");
            Environment.SetEnvironmentVariable("GDAL_DATA", gdalData);
            Gdal.SetConfigOption("GDAL_DATA", gdalData);

            string driverPath = Path.Combine(nativePath, "plugins");
            Environment.SetEnvironmentVariable("GDAL_DRIVER_PATH", driverPath);
            Gdal.SetConfigOption("GDAL_DRIVER_PATH", driverPath);

            Environment.SetEnvironmentVariable("GEOTIFF_CSV", gdalData);
            Gdal.SetConfigOption("GEOTIFF_CSV", gdalData);

            string projSharePath = Path.Combine(gdalPath, "share");
            Environment.SetEnvironmentVariable("PROJ_LIB", projSharePath);
            Gdal.SetConfigOption("PROJ_LIB", projSharePath);
            OSGeo.OSR.Osr.SetPROJSearchPaths(new[] { projSharePath });

            Ogr.RegisterAll();

            return true;
        }

    }
}
