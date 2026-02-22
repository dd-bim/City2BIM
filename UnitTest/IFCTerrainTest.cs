using BIMGISInteropLibs.IFC;
using BIMGISInteropLibs.IfcTerrain;
using BIMGISInteropLibs.Logging;
using IxMilia.Dxf;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using NetTopologySuite.Geometries;
using Npgsql.Replication.TestDecoding;
using System.ComponentModel;
using System.DirectoryServices;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Shapes;
using Xbim.IO.Xml.BsConf;
using Xunit.Abstractions;
using Xunit.Sdk;
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain;
using Path = System.IO.Path; //to set log messages

namespace UnitTest
{
    public class IFCTerrainTest : IDisposable
    {
        public static int testId;
        public static string TestDataPath = "D:\\DGM_Testdaten";
        public static int numTries = 0;
        static bool readPoints = false;

        private readonly ITestOutputHelper _out;

        public IFCTerrainTest(ITestOutputHelper output)
        {
             _out = output;

            // schnelle, sichere Sink: schreibt LogPair in Test-Output
            LogWriter.RegisterSink(lp =>
            {
                try
                {
                    // formatieren nach Wunsch
                    _out.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{lp.Type}] {lp.Message}");
                }
                catch
                {
                    // sink darf keine Exceptions nach außen werfen
                }
            });

            // optional: Init Logger / Pfade hier setzen
        }

        public void Dispose()
        {
            try { LogWriter.WriteLogFile(); } catch { }
            LogWriter.UnregisterSink();
        }

        [Theory]
        [MemberData(nameof(TestCombinations))]
        public void IFCTypeTest(int testId, string FilePath, IfcVersion IFCVersion = IfcVersion.IFC4dot3, SurfaceType surfaceType = SurfaceType.TIN)
        {
            Config config = new Config()
            {
                filePath = FilePath,
                fileName = Path.GetFileName(FilePath),
                outSurfaceType = surfaceType,
                outIFCType = IFCVersion,
                readPoints = readPoints
            };
            string resultsDir = Path.Combine(AppContext.BaseDirectory, "TestArtifacts");
            Directory.CreateDirectory(resultsDir);
            string suffix = $"{config.outIFCType}_{config.outSurfaceType}" +
                            $"{(config.breakline == true ? "_BL" : "")}" +
                            $"_{config.logeoref}";
            config.destFileName = Path.Combine(resultsDir, $"{testId.ToString()}_{System.IO.Path.GetFileNameWithoutExtension(config.fileName)}_{suffix}.ifc");
            ConversionTest(FilePath, config, testId.ToString());
        }
        [Theory]
        [MemberData(nameof(EnvelopeCombinations))]
        public void EnvelopeTest(int testId, string FilePath, double zFilter = 0.0, double xExtend = 0.0, double yExtend = 0.0)
        {
            Config config = new Config()
            {
                filePath = FilePath,
                fileName = System.IO.Path.GetFileName(FilePath),
                outSurfaceType = SurfaceType.TIN,
                outIFCType = IfcVersion.IFC4dot3,
                readPoints = readPoints,
                zFilter = zFilter != 0.0 ? zFilter : (double?)null
            };
            if (xExtend != 0.0 || yExtend != 0.0)
            {
                config.xExtend = xExtend;
                config.yExtend = yExtend;
                config.customOrigin = true;
            }
            string resultsDir = Path.Combine(AppContext.BaseDirectory, "TestArtifacts");
            Directory.CreateDirectory(resultsDir);
            string suffix = $"xExtend_{config.xExtend}_yExtend{config.yExtend}" +
                            (config.zFilter.HasValue ? $"_zFilter_{config.zFilter}" : "");
            config.destFileName = Path.Combine(resultsDir, $"{testId.ToString()}_{System.IO.Path.GetFileNameWithoutExtension(config.fileName)}_{suffix}.ifc");
            ConversionTest(FilePath, config, testId.ToString());
        }
        private void ConversionTest(string FilePath, Config config, string testID = "") //double zFilter = 0.0, double xExtend = 0.0, double yExtend = 0.0)
        {
            LogWriter.initLogger(config);
            IfcTerrainFileType? fileType;
            Coordinate origin = GetOriginFromFile(FilePath, out fileType);
            if (fileType == null)
            {
                Assert.Fail("File type not supported for testing.");
                return;
            }
            config.fileType = fileType.Value;
            if (config.customOrigin == true)
            {
                config.xOrigin = origin != null ? origin.X : 600250;
                config.yOrigin = origin != null ? origin.Y : 5650250;
            }
            if (fileType == IfcTerrainFileType.DXF)
            {
                using (var fileStream = new FileStream(config.filePath, FileMode.Open))
                {
                    //open dxf file
                    DxfFile dxf = DxfFile.Load(fileStream);
                    var DEMLayers = dxf.Layers.Where(item => Regex.IsMatch(item.Name ?? "", @"DEM|DGM|TIN", RegexOptions.IgnoreCase)).ToList(); // for Testing we search for Layers named DEM/DGM
                    if (DEMLayers.Count <= numTries)
                    {
                        Assert.Fail("File contains no convertable DEM/DGM Layer");
                        numTries = 0;
                        return;
                    }
                    config.layer = new string[] { DEMLayers[numTries].Name };
                }
            }

            ConnectionInterface conInt = new ConnectionInterface();
            bool result = conInt.mapProcess(config, null, null);
            if (!result)
            {
                if (config.fileType == IfcTerrainFileType.DXF) //Try multiple options for DXf Files
                {
                    //retry 
                    if (!config.readPoints.Value) //with Processing via Points
                    {
                        config.readPoints = true;
                        ConversionTest(FilePath, config);
                    }
                    else //with next Layer
                    {
                        numTries++;
                        config.readPoints = false;
                        ConversionTest(FilePath, config);
                    }
                    return;
                }
                Assert.Fail("Mapping process failed. Check log for details.");
                return;
            }
            // Try reading the generated IFC file
            Xbim.Ifc.IfcStore ifcStore = Xbim.Ifc.IfcStore.Open(config.destFileName);
            Assert.NotNull(ifcStore);
            numTries = 0;
        }

        public static IEnumerable<object[]> TestFiles => 
            new[] {".gml", ".dxf", ".txt", ".xyz", ".reb", ".out" }
            .SelectMany(ext => Directory.EnumerateFiles(TestDataPath, "*" + ext, SearchOption.AllDirectories))
            .Select(path => new object[] { path });

        public static IEnumerable<object[]> IFCVersions =>
            Enum.GetValues(typeof(IfcVersion))
            .Cast<IfcVersion>()
            .Select(opt => new object[] { opt });

        public static IEnumerable<object[]> SurfaceOptions =>
            Enum.GetValues(typeof(SurfaceType))
            .Cast<SurfaceType>()
            .Select(opt => new object[] { opt });

        public static IEnumerable<object[]> TestCombinations =>
            TestFiles.SelectMany(f => IFCVersions.SelectMany(v => SurfaceOptions.Select(s => new
            {
                File = (string)f[0],
                Version = (IfcVersion)v[0],
                Surface = (SurfaceType)s[0]
            })))
            .Where(c => !(c.Surface == SurfaceType.TIN &&
                 (c.Version == IfcVersion.IFC2x3 || c.Version == IfcVersion.IFC4))) // Remove Test using TIN with IFC2x3 and IFC4, as TIN is not supported in these versions
            .Select(c => new object[] { IFCTerrainTest.testId++, c.File, c.Version, c.Surface }); 

        // EnvelopeCombinations: kombiniert TestFiles mit zFilter- und Extend-Werten
        public static IEnumerable<object[]> EnvelopeCombinations =>
            TestFiles.SelectMany(f =>
                new double[] { 0.0, 0.1, 1.0 } // zFilter-Werte
                .SelectMany(zf =>
                new double[] {0.0, 250.0, 500.0 } // xExtend-Werte
                .SelectMany(xe =>
                new double[] {0.0, 250.0, 500.0 } // yExtend-Werte
                .Select(ye => new object[] { IFCTerrainTest.testId++, f[0], zf, xe, ye }))))
            .ToList();

        // Helper: versuche Centroid (Origin) aus Quelldatei zu berechnen
        private static Coordinate GetOriginFromFile(string path, out IfcTerrainFileType? fileType)
        {
            fileType = null;
            try
            {
                var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
                BIMGISInteropLibs.IfcTerrain.Result res = new Result();
                switch (ext)
                {
                    case ".dxf":
                        fileType = IfcTerrainFileType.DXF;
                        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                        {
                            var dxf = DxfFile.Load(fs);
                            var cfg = new Config() { filePath = path, fileName = Path.GetFileName(path) };
                            // ReaderTerrain.readDxf gibt ein Result zurück (wie in mapProcess verwendet)
                            res = BIMGISInteropLibs.DXF.ReaderTerrain.readDxf(cfg, dxf);
                        }
                        break;

                    case ".xml":
                        res = BIMGISInteropLibs.LandXML.ReaderTerrain.readDtmData(new Config() { filePath = path, fileName = Path.GetFileName(path) });
                        fileType = IfcTerrainFileType.LandXML;
                        break;

                    case ".gml":
                        res = BIMGISInteropLibs.CityGML.CityGMLReaderTerrain.readTin(new Config() { filePath = path, fileName = Path.GetFileName(path) });
                        fileType = IfcTerrainFileType.CityGML;
                        break;

                    case ".txt":
                    case ".xyz":
                        res = BIMGISInteropLibs.ElevationGrid.ReaderTerrain.readGrid(new Config() { filePath = path, fileName = Path.GetFileName(path) });
                        fileType = IfcTerrainFileType.Grid;
                        break;

                    case ".reb":
                        res = BIMGISInteropLibs.REB.ReaderTerrain.readDtm(new Config() { filePath = path, fileName = Path.GetFileName(path) });
                        fileType = IfcTerrainFileType.REB;
                        break;

                    case ".out":
                        res = BIMGISInteropLibs.GEOgraf.ReadOUT.readOutData(new Config() { filePath = path, fileName = Path.GetFileName(path) });
                        fileType = IfcTerrainFileType.Grafbat;
                        break;

                    default:
                        fileType = null;
                        return null;
                }

                if (res != null && res.pointList != null && res.pointList.Count > 0)
                {
                    // Centroid der Punktmenge
                    var pts = res.pointList.ToArray();
                    var mp = new MultiPoint(pts);
                    var cent = NetTopologySuite.Algorithm.Centroid.GetCentroid(mp);
                    return cent.CoordinateValue;
                }
            }
            catch
            {
                // still return null on any problem — Test verwendet dann Default origins
            }
            return null;
        }
    }
}