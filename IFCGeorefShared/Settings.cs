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

        private static void EnsureGdalNativePath()
        {
            // Bestimme Architektur-Ordner (x64/x86)
            var arch = IntPtr.Size == 8 ? "x64" : "x86";

            // Basis-Ausgabeverzeichnis der Anwendung
            var baseDir = AppContext.BaseDirectory;

            // Erwarteter Pfad, wenn Sie GDAL natives in "$(OutputPath)/gdal/<arch>/" kopieren
            var gdalNativeDir = Path.Combine(baseDir, "gdal", arch);

            if (Directory.Exists(gdalNativeDir))
            {
                var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                // Nur hinzufügen, falls noch nicht vorhanden
                var paths = path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
                if (!paths.Any(p => string.Equals(Path.GetFullPath(p).TrimEnd(Path.DirectorySeparatorChar), Path.GetFullPath(gdalNativeDir).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)))
                {
                    var newPath = gdalNativeDir + Path.PathSeparator + path;
                    Environment.SetEnvironmentVariable("PATH", newPath);
                }
            }

            // GDAL_DATA (falls im Paket enthalten unter gdal/data oder gdal/<arch>/data)
            var possibleGdalData = new[]
            {
                Path.Combine(baseDir, "gdal", "data"),
                Path.Combine(baseDir, "gdal", arch, "data"),
                Path.Combine(baseDir, "gdal", "share", "gdal"),
                Path.Combine(baseDir, "share", "gdal")
            };
            var gdalDataDir = possibleGdalData.FirstOrDefault(Directory.Exists);
            if (gdalDataDir != null)
                Environment.SetEnvironmentVariable("GDAL_DATA", gdalDataDir);

            // PROJ_LIB (Projektionen), falls vorhanden
            var possibleProj = new[]
            {
                Path.Combine(baseDir, "gdal", "proj"),
                Path.Combine(baseDir, "gdal", "share", "proj"),
                Path.Combine(baseDir, "share", "proj"),
                Path.Combine(baseDir, "share")
            };
            var projDir = possibleProj.FirstOrDefault(Directory.Exists);
            if (projDir != null)
                Environment.SetEnvironmentVariable("PROJ_LIB", projDir);
        }

        public static bool configureOgr()
        {
            EnsureGdalNativePath();

            Ogr.RegisterAll();

            return true;
        }

    }
}
