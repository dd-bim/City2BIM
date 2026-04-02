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
            // Determine architecture
            var arch = IntPtr.Size == 8 ? "x64" : "x86";

            var baseDir = AppContext.BaseDirectory;

            // Expected Dir, where GDAL.native copies gdal libraries
            var gdalNativeDir = Path.Combine(baseDir, "gdal", arch);

            if (Directory.Exists(gdalNativeDir))
            {
                var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                var paths = path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
                if (!paths.Any(p => string.Equals(Path.GetFullPath(p).TrimEnd(Path.DirectorySeparatorChar), Path.GetFullPath(gdalNativeDir).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)))
                {
                    var newPath = gdalNativeDir + Path.PathSeparator + path;
                    Environment.SetEnvironmentVariable("PATH", newPath);
                }
            }

            // PROJ_LIB
            var possibleProj = new[]
            {
                Path.Combine(baseDir, "gdal", "proj"),
                Path.Combine(baseDir, "gdal", "share", "proj"),
                Path.Combine(baseDir, "share", "proj"),
                Path.Combine(baseDir, "gdal", "share")
            };
            var projDir = possibleProj.FirstOrDefault(Directory.Exists);
            if (projDir != null)
                OSGeo.OSR.Osr.SetPROJSearchPath(projDir); //Use this GDAL method to set PROJ_LIB instead of environment variable!! second is not reliable!

            Ogr.RegisterAll();        
            return true;
        }
    }
}
