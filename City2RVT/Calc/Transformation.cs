using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;

namespace City2RVT.Calc
{
    class Transformation
    {
        double feetToMeter = 1.0 / 0.3048;

        /// <summary>
        /// Transforms local coordinates to relative coordinates due to the fact that revit has a 20 miles limit for presentation of geometry. 
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public Transform transform(Document doc)
        {
            //Zuerst wird die Position des Projektbasispunkts bestimmt
            ProjectLocation projloc = doc.ActiveProjectLocation;
            ProjectPosition position_data = projloc.GetProjectPosition(XYZ.Zero);
            double angle = position_data.Angle;
            double elevation = position_data.Elevation;
            double easting = position_data.EastWest;
            double northing = position_data.NorthSouth;

            // Der Ostwert des PBB wird als mittlerer Ostwert für die UTM Reduktion verwendet.
            double xSchwPktFt = easting;
            double xSchwPktKm = (double)((xSchwPktFt / feetToMeter) / 1000);
            double xSchwPkt500 = xSchwPktKm - 500;
            double R = 1;

            Transform trot = Transform.CreateRotation(XYZ.BasisZ, -angle);
            XYZ vector = new XYZ(easting, northing, elevation);
            XYZ vectorRedu = vector / R;
            Transform ttrans = Transform.CreateTranslation(-vectorRedu);
            Transform transf = trot.Multiply(ttrans);

            return transf;
        }

        public Plane getGeomPlane(Document doc, XYZ normal, XYZ origin)
        {
            ProjectLocation projloc = doc.ActiveProjectLocation;
            ProjectPosition position_data = projloc.GetProjectPosition(XYZ.Zero);
            double elevation = position_data.Elevation;

            //XYZ origin = new XYZ(0, 0, 0);
            //XYZ normal = new XYZ(direction.X, direction.Y, direction.Z);
            //XYZ normal = new XYZ(0, 0, 1);
            Plane geomPlane = Plane.CreateByNormalAndOrigin(normal, origin);

            return geomPlane;
        }
    }
}
