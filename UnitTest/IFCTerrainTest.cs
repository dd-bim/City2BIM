
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
        [Fact]
        public void EnvelopeTest()
        {
            string filepath = "D:\\DGM_Testdaten\\07_eigen\\DGM_Eigen_25062025.dxf";
            Config config = new Config()
            {
                filePath = filepath,
                fileName = Path.GetFileName(filepath),
                layer = new string[]{ "C-TINN-VIEW" },
                outSurfaceType = SurfaceType.TIN,
                outIFCType = IfcVersion.IFC4dot3,
                readPoints = false,
                fileType = IfcTerrainFileType.DXF,
                customOrigin = true,
                xOrigin = 600250,
                yOrigin = 5650250,
                xExtend = 0,
                yExtend = 250,
                destFileName = "D:\\Out\\test_env.ifc",
                breakline = false,
                breakline_layer = "_Linien"
            };
            ConnectionInterface conInt = new ConnectionInterface();
            LogWriter.initLogger(config);
            bool result = conInt.mapProcess(config, null, null);
        }

        public static string TestDataPath = "D:\\DGM_Testdaten";
        public static int numTries = 0;
        static bool readPoints = false;
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
                outIFCType = IFCVersion,
                readPoints = readPoints
            };
            if (readPoints) readPoints = false; //reset to default (false) for next try
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
                        var DEMLayers = dxf.Layers.Where(item => Regex.IsMatch(item.Name ?? "", @"DEM|DGM|TIN", RegexOptions.IgnoreCase)).ToList(); // for Testing we search for Layers named DEM/DGM
                        if(DEMLayers.Count <= numTries)
                        {
                            Assert.True(false, "File contains no convertable DEM/DGM Layer");
                            numTries = 0;
                            return;
                        }
                        config.layer = new string[] { DEMLayers[numTries].Name }; 
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
            string suffix = $"{config.fileType}_{config.outIFCType}_{config.outSurfaceType}" +
                            $"{(config.breakline == true ? "_BL" : "")}" +
                            $"_{config.logeoref}";
            config.destFileName = Path.Combine(resultsDir, $"{Path.GetFileNameWithoutExtension(config.fileName)}_{suffix}.ifc"); 

            ConnectionInterface conInt = new ConnectionInterface();
            LogWriter.initLogger(config);
            bool result = conInt.mapProcess(config, null, null);
            if (!result)
            {
                if (config.fileType == IfcTerrainFileType.DXF) //Try multiple options for DXf Files
                {
                    //retry 
                    if (!readPoints) //with Processing via Points
                    {
                        readPoints = true;
                        ConversionTest(FilePath, IFCVersion, surfaceType);
                    }
                    else //with next Layer
                    {
                        numTries++;
                        ConversionTest(FilePath, IFCVersion, surfaceType);
                    }
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
            new object[] { file[0], version[0], option[0] })))
            .Where(combination => !(combination[2].Equals(SurfaceType.TIN) && 
            (combination[1].Equals(IfcVersion.IFC2x3) || combination[1].Equals(IfcVersion.IFC4)))); // Remove Test using TIN with IFC2x3 and IFC4, as TIN is not supported in these versions

    }


}