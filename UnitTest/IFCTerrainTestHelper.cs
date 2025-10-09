
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
    public class FunctionTestHelper
    {
        static internal int digits = 4; //number of digits for rounding in Assert.Equal
        public static IEnumerable<object[]> RotationTestCases()
        {
            yield return new object[] { 30.0, 0.8660, -0.5 };
            yield return new object[] { 45.0, 0.7071, -0.7071 };
            yield return new object[] { 60.0, 0.5, -0.8660 };
            yield return new object[] { 90.0, 0.0, -1.0 };
            yield return new object[] { 100.0, -0.1736, -0.9848 };
            yield return new object[] { 200.0, -0.9397, 0.3420 };
            yield return new object[] { 300.0, 0.5000, 0.8660 };
            // weitere Werte nach Bedarf
        }
    }
}