
using BIMGISInteropLibs.IFC;
using BIMGISInteropLibs.IfcTerrain;
using BIMGISInteropLibs.Logging;
using IxMilia.Dxf;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Npgsql.Replication.TestDecoding;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using Xbim.IO.Xml.BsConf;
using Xunit.Abstractions;
using Xunit.Sdk;

using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

namespace UnitTest
{
    public sealed class TestOutputWriter : TextWriter
    {
        readonly ITestOutputHelper _output;
        readonly StringBuilder _sb = new StringBuilder();

        public TestOutputWriter(ITestOutputHelper output) => _output = output;
        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            if (value == '\n')
            {
                if (_sb.Length > 0)
                {
                    _output.WriteLine(_sb.ToString().TrimEnd('\r'));
                    _sb.Clear();
                }
            }
            else
            {
                _sb.Append(value);
            }
        }

        public override void Write(string? value)
        {
            if (string.IsNullOrEmpty(value)) return;
            int start = 0;
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == '\n')
                {
                    _sb.Append(value, start, i - start);
                    _output.WriteLine(_sb.ToString().TrimEnd('\r'));
                    _sb.Clear();
                    start = i + 1;
                }
            }
            if (start < value.Length) _sb.Append(value, start, value.Length - start);
        }

        public override void WriteLine(string? value)
        {
            Write(value);
            Write('\n');
        }

        protected override void Dispose(bool disposing)
        {
            if (_sb.Length > 0)
            {
                try { _output.WriteLine(_sb.ToString()); } catch { }
                _sb.Clear();
            }
            base.Dispose(disposing);
        }
    }

    public class FunctionTestHelper
    {
        static internal int digits = 4; //number of digits for rounding in Assert.Equal
        public static IEnumerable<object[]> RotationTestCases()
        {
            // angle in degrees, expected X, expected Y for RefDirection
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