using Autodesk.Revit.DB;
using BIMGISInteropLibs.Geometry;
using CityBIM.GUI;
using Microsoft.Isam.Esent.Interop;
using System.Collections.Generic;

namespace CityBIM.Builder
{
    public class RevitBuilder
    {
        public Document doc { get; protected set; }
        private readonly XYZ pbp;
        private C2BPoint pbp_c2b { get => new C2BPoint(pbp.X, pbp.Y, pbp.Z); }
        private readonly double projectScale;
        public Autodesk.Revit.DB.Transform trafo2RevitCS { get; protected set; }

        public RevitBuilder(Document doc)
        {
            this.doc = doc;
            this.pbp = utils.getProjectBasePointMeter(doc);
            this.projectScale = utils.getHTWDDProjectScale(doc);
            this.trafo2RevitCS = utils.getGlobalToRevitTransform(this.doc);
        }

        public XYZ GetUnProjectedPoint(XYZ geoPnt, XYZ offset = null)
        {
            if (offset != null)
            {
                geoPnt = geoPnt + offset;
            }

            var geoPntUnProjX = geoPnt.X - (geoPnt.X - this.pbp.X) + (geoPnt.X - this.pbp.X) / this.projectScale;
            var geoPntUnProjY = geoPnt.Y - (geoPnt.Y - this.pbp.Y) + (geoPnt.Y - this.pbp.Y) / this.projectScale;

            return new XYZ(geoPntUnProjX, geoPntUnProjY, geoPnt.Z);
        }

        public C2BPoint GetUnprojectedPoint(C2BPoint geoPnt, C2BPoint offset = null)
        {
            if (offset != null)
            {
                geoPnt = geoPnt + offset;
            }

            var geoPntUnProjX = geoPnt.X - (geoPnt.X - pbp.X) + (geoPnt.X - pbp.X) / projectScale;
            var geoPntUnProjY = geoPnt.Y - (geoPnt.Y - pbp.Y) + (geoPnt.Y - pbp.Y) / projectScale;

            return new C2BPoint(geoPntUnProjX, geoPntUnProjY, geoPnt.Z);
        }

        public XYZ GetRevitPt(XYZ point)
        {
            var pnt_feet = point.Divide(0.3048);
            return this.trafo2RevitCS.OfPoint(pnt_feet);
        }

        public XYZ GetRevitPt(C2BPoint point)
        {
            var pnt_feet = new XYZ(point.X, point.Y, point.Z).Divide(0.3048);
            return this.trafo2RevitCS.OfPoint(pnt_feet);
        }

        /// <summary>
        /// Creates List of CurveLoops with exterior ring and may interior rings
        /// </summary>
        /// <returns>List of CurveLoops for Revit geometry builder e.g. extrusion profile</returns>
        public List<CurveLoop> CreateExteriorCurveLoopList(List<C2BPoint> exteriorRing, List<List<C2BPoint>> interiorRings, out XYZ planeNormal, C2BPoint calcOffset = null)
        {
            List<CurveLoop> rvtPoly = new List<CurveLoop>();        //return variable --> extrusion profile (1 x exterior ring, n x interior rings)

            planeNormal = CalcExteriorFace(exteriorRing/*, interiorRings*/, out List<C2BPoint> extPlane/*, out List<List<C2BPoint>> intPlaneLi*/);   //calculation of normal pointing

            //map to revit and reverse calculation of projection - exterior

            List<XYZ> rvtExteriorXYZ = new List<XYZ>();

            foreach (var pt in extPlane)
            {
                C2BPoint unprojectedPt = GetUnprojectedPoint(pt, calcOffset);
                XYZ revPt = GetRevitPt(unprojectedPt);    //transform to Revit Pt

                rvtExteriorXYZ.Add(revPt);
            }

            //create CurveLoops - exterior

            List<Curve> edgesExt = new List<Curve>();

            for (var c = 1; c < rvtExteriorXYZ.Count; c++)
            {
                Line edgeEx = Line.CreateBound(rvtExteriorXYZ[c - 1], rvtExteriorXYZ[c]);

                edgesExt.Add(edgeEx);
            }

            CurveLoop outerLoop = CurveLoop.Create(edgesExt);
            rvtPoly.Add(outerLoop);

            return rvtPoly;
        }


        /// <summary>
        /// Calculates a plane representation of the polygon
        /// </summary>
        /// <param name="extPts">exteriorRing</param>
        /// <param name="intPtsL">interiorRings</param>
        /// <returns>equalizedNormal</returns>
        private static XYZ CalcExteriorFace(List<C2BPoint> extPts, out List<C2BPoint> extPtsPl)
        {
            extPtsPl = new List<C2BPoint>();
            //intPtsLPl = new List<List<C2BPoint>>();

            //plane polygon needs one normal (balanced out of all segments) for further calculations

            C2BPoint centroidExt = new C2BPoint(0, 0, 0);   //needed later, but can be calculated in first loop

            //Normal calculation
            //------------------

            C2BPoint normalVc = new C2BPoint(0, 0, 0);

            for (var c = 1; c < extPts.Count; c++)
            {
                normalVc += C2BPoint.CrossProduct(extPts[c - 1], extPts[c]);

                centroidExt += extPts[c];
            }

            C2BPoint normalizedVc = C2BPoint.Normalized(normalVc);
            C2BPoint normalizedVcCW = new C2BPoint(-normalizedVc.X, -normalizedVc.Y, -normalizedVc.Z);

            //Improvements of points (matches the new balanced plane) 
            //------------------

            C2BPoint centroidExtR = centroidExt / (extPts.Count - 1);

            foreach (var pt in extPts)
            {
                C2BPoint vecPtCent = pt - centroidExtR;
                double d = C2BPoint.ScalarProduct(vecPtCent, normalizedVc);

                C2BPoint vecLotCent = new C2BPoint(d * normalizedVc.X, d * normalizedVc.Y, d * normalizedVc.Z);
                extPtsPl.Add(pt - vecLotCent);
            }

            return new XYZ(normalizedVc.X, normalizedVc.Y, normalizedVc.Z);
        }
    }
}
