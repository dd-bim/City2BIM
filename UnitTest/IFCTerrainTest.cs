
using BIMGISInteropLibs.IFC;
using BIMGISInteropLibs.IfcTerrain;
using BIMGISInteropLibs.Logging;
using IxMilia.Dxf;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Npgsql.Replication.TestDecoding;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Xbim.IO.Xml.BsConf;
using Xunit.Abstractions;
using Xunit.Sdk;

using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

namespace UnitTest
{
    public class IFCTerrainTest
    {
        public static string TestDataPath = "D:\\DGM_Testdaten";
        public static int numTries = 0;
        [Theory]
        [MemberData(nameof(TestCombinations))]
        public void ConversionTest(string FilePath, IfcVersion IFCVersion = IfcVersion.IFC4, SurfaceType surfaceType = SurfaceType.SBSM)
        {
            //Interface between GUI, reader and writer
            Config config = new Config()
            {
                filePath = FilePath,
                fileName = Path.GetFileName(FilePath),
                outSurfaceType = surfaceType,
                outIFCType = IFCVersion
            };
            switch (Path.GetExtension(FilePath))
            {
                case ".xml":
                    config.fileType = IfcTerrainFileType.LandXML;
                break;
                case ".gml":
                    config.fileType = IfcTerrainFileType.CityGML;
                break;
                case ".dxf":
                    using (var fileStream = new FileStream(config.filePath, FileMode.Open))
                    {
                        //open dxf file
                        DxfFile dxf = DxfFile.Load(fileStream);
                        var DEMLayers = dxf.Layers.Where(item => Regex.IsMatch(item.Name ?? "", @"DEM|DGM", RegexOptions.IgnoreCase)).ToList(); // for Testing we search for Layers named DEM/DGM
                        if(DEMLayers.Count <= numTries)
                        {
                            Assert.True(false, "File contains no convertable DEM/DGM Layer");
                            numTries = 0;
                            return;
                        }
                        config.readPoints = true;
                        config.layer = DEMLayers[numTries].Name; 
                    }
                    config.fileType = IfcTerrainFileType.DXF;
                break;
                case ".txt":
                    config.fileType = IfcTerrainFileType.Grid;
                    break;
                case ".reb":
                    config.fileType = IfcTerrainFileType.REB;
                break;
                case ".out":
                    config.fileType = IfcTerrainFileType.Grafbat;
                break;
                default:
                    Assert.True(false, "Filetype not supported for Test");
                    return;
            }
            // Temp-Output
            string resultsDir = Path.Combine(AppContext.BaseDirectory, "TestArtifacts");
            Directory.CreateDirectory(resultsDir);
            string suffix = $"{config.outIFCType}_{config.outSurfaceType}" +
                            $"{(config.breakline == true ? "_BL" : "")}" +
                            $"_{config.logeoref}";
            config.destFileName = Path.Combine(resultsDir, $"{Path.GetFileNameWithoutExtension(config.fileName)}_{suffix}.ifc"); 

            ConnectionInterface conInt = new ConnectionInterface();
            LogWriter.initLogger(config);
            bool result = conInt.mapProcess(config, null, null);
            if (!result)
            {
                if (config.fileType == IfcTerrainFileType.DXF)
                {
                    numTries++;
                    ConversionTest(FilePath, IFCVersion, surfaceType);
                    return;
                }
                Assert.True(false, "Mapping process failed. Check log for details.");
                return;
            }
            // Try reading the generated IFC file
            Xbim.Ifc.IfcStore ifcStore = Xbim.Ifc.IfcStore.Open(config.destFileName);
            Assert.NotNull(ifcStore);
            numTries = 0;
        }

        public static IEnumerable<object[]> TestFiles => 
            new[] { ".xml", ".gml", ".dxf", ".txt", ".reb", ".out" }
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
            TestFiles.SelectMany(file =>
            IFCVersions.SelectMany(version =>
            SurfaceOptions.Select(option =>
            new object[] { file[0], version[0], option[0] }))); // extract inner items

    }


}