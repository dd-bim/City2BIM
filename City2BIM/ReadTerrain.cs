using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Serilog;
using C2BPoint = City2BIM.GetGeometry.C2BPoint;
using GmlAttribute = City2BIM.GetSemantics.Xml_AttrRep;

namespace City2BIM
{
    public class ReadTerrain
    {
        // The main Execute method (inherited from IExternalCommand) must be public
        public ReadTerrain(Document doc)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(@"C:\Users\goerne\Desktop\logs_revit_plugin\\log_plugin" + DateTime.UtcNow.ToFileTimeUtc() + ".txt"/*, rollingInterval: RollingInterval.Day*/)
                .CreateLogger();

            Log.Information("Start...");
 
            //Import via Dialog:
            FileDialog imp = new FileDialog();
            var path = imp.ImportPath(FileDialog.Data.DGM);
            //-------------------------------
            Log.Information("File: " + path);

            Log.Information("Start reading Terrain-XYZ data...");

            System.IO.StreamReader file = new System.IO.StreamReader(path);

            var format = path.Split('.').Last();

            string line;
            var dgmPtList = new List<C2BPoint>();

            while((line = file.ReadLine()) != null)
            {
                string[] str = new string[2];

                char delim = ' ';

                if(format == "csv")
                {
                    if(line.Contains(','))
                    {
                        delim = ',';
                    }

                    if(line.Contains(';'))
                    {
                        delim = ';';
                    }
                }

                str = line.Split(new[] { delim }, StringSplitOptions.RemoveEmptyEntries);

                if(str.Length > 2
                       && double.TryParse(str[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double x)
                       && double.TryParse(str[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y)
                       && double.TryParse(str[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double z))
                {
                    dgmPtList.Add(new C2BPoint(x, y, z));
                }
            }

            var rev = new RevitBuilder.RevitTopoSurfaceBuilder(doc);
            rev.CreateDTM(dgmPtList);
        }
    }
}