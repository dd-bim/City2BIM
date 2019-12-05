using Autodesk.Revit.DB;
using City2BIM.GetGeometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace City2BIM.RevitBuilder
{
    class RevitBuilder
    {
        private XYZ GetRevPt(C2BPoint rawPt)
        {
            //Transformation for revit
            var ptCalc = new GeorefCalc();
            var unprojectedPt = ptCalc.CalcUnprojectedPoint(rawPt, true);

            var revitPt = unprojectedPt / Prop.feetToM;

            //Creation of Revit point
            var revitXYZ = new XYZ(revitPt.Y, revitPt.X, revitPt.Z);

            //Transform global coordinate to Revit project coordinate system (system of project base point)
            var revTransXYZ = Revit_Prop.TrafoPBP.OfPoint(revitXYZ);

            return revTransXYZ;
        }

    }
}
