﻿using System;

using Autodesk.Revit.DB;


namespace CityBIM.Calc
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

        public Plane getGeomPlane(XYZ normal, XYZ origin)
        {
            Plane geomPlane = Plane.CreateByNormalAndOrigin(normal, origin);
            return geomPlane;
        }

        public XYZ getProjectBasePoint(Document doc)
        {
            ProjectLocation projloc = doc.ActiveProjectLocation;
            ProjectPosition position_data = projloc.GetProjectPosition(XYZ.Zero);
            double elevation = position_data.Elevation;
            double easting = position_data.EastWest;
            double northing = position_data.NorthSouth;

            XYZ pbp = new XYZ(Math.Round((easting / feetToMeter), 4), (Math.Round((northing / feetToMeter), 4)), (Math.Round((elevation / feetToMeter), 4)));

            return pbp;
        }

        public Double getAngle(Document doc)
        {
            ProjectLocation projloc = doc.ActiveProjectLocation;
            ProjectPosition position_data = projloc.GetProjectPosition(XYZ.Zero);
            double angle = position_data.Angle;

            return angle;
        }
    }
}