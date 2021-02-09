using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using C2BPoint = BIMGISInteropLibs.Geometry.C2BPoint;
using GmlAttribute = BIMGISInteropLibs.Semantic.Xml_AttrRep;

namespace City2RVT.Reader
{
    public class ReadTerrain
    {
        // The main Execute method (inherited from IExternalCommand) must be public
        public ReadTerrain(Document doc)
        {
            //Import via Dialog:
            FileDialog imp = new FileDialog();
            var path = imp.ImportPath(FileDialog.Data.DGM);
            //-------------------------------
            System.IO.StreamReader file = new System.IO.StreamReader(path);

            int lastIndex = path.LastIndexOf("\\");
            var dtmFileName = (path.Substring(lastIndex + 1)).Split('.').First();
            GUI.Prop_NAS_settings.DtmFile = dtmFileName;

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

            var rev = new Builder.RevitTopoSurfaceBuilder(doc);
            rev.CreateDTM(dgmPtList);
        }
    }
}