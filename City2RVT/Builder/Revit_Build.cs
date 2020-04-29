using Autodesk.Revit.DB;
using City2RVT.GUI;
using City2BIM.Geometry;
using System.Collections.Generic;

namespace City2RVT.Builder
{
    public static class Revit_Build
    {
        /// <summary>
        /// Creates List of CurveLoops with exterior ring and may interior rings
        /// </summary>
        /// <returns>List of CurveLoops for Revit geometry builder e.g. extrusion profile</returns>
        public static List<CurveLoop> CreateExteriorCurveLoopList(List<C2BPoint> exteriorRing, List<List<C2BPoint>> interiorRings, out XYZ planeNormal, C2BPoint calcOffset = null)
        {
            List<CurveLoop> rvtPoly = new List<CurveLoop>();        //return variable --> extrusion profile (1 x exterior ring, n x interior rings)

            planeNormal = CalcExteriorFace(exteriorRing/*, interiorRings*/, out List<C2BPoint> extPlane/*, out List<List<C2BPoint>> intPlaneLi*/);   //calculation of normal pointing

            //map to revit and reverse calculation of projection - exterior

            List<XYZ> rvtExteriorXYZ = new List<XYZ>();

            foreach (var pt in extPlane)
            {
                C2BPoint unprojectedPt = Calc.GeorefCalc.CalcUnprojectedPoint(pt, Prop_CityGML_settings.IsGeodeticSystem, calcOffset); //calc unprojected coords
                XYZ revPt = GetRevPt(unprojectedPt);    //transform to Revit Pt

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

        public static List<CurveLoop> CreateInteriorCurveLoopList(/*List<C2BPoint> exteriorRing,*/ List<List<C2BPoint>> interiorRings, out XYZ planeNormal, C2BPoint calcOffset = null)
        {
            List<CurveLoop> rvtPoly = new List<CurveLoop>();        //return variable --> extrusion profile (1 x exterior ring, n x interior rings)

            planeNormal = CalcInteriorFace(interiorRings, out List<List<C2BPoint>> intPlaneLi);   //calculation of normal pointing

            //map to revit and reverse calculation of projection - interior

            List<List<XYZ>> rvtInteriorXYZList = new List<List<XYZ>>();

            foreach (var ring in intPlaneLi)
            {
                List<XYZ> rvtInteriorXYZ = new List<XYZ>();

                foreach (var pt in ring)
                {
                    C2BPoint unprojectedPt = Calc.GeorefCalc.CalcUnprojectedPoint(pt, Prop_CityGML_settings.IsGeodeticSystem, calcOffset); //calc unprojected coords
                    XYZ revPt = GetRevPt(unprojectedPt);    //transform to Revit Pt

                    rvtInteriorXYZ.Add(revPt);
                }
                rvtInteriorXYZList.Add(rvtInteriorXYZ);
            }

            //create CurveLoops - interior
            foreach (var rvtInteriorXYZ in rvtInteriorXYZList)
            {
                List<Curve> edgesInt = new List<Curve>();

                for (var c = 1; c < rvtInteriorXYZ.Count; c++)
                {
                    Line edgeIn = Line.CreateBound(rvtInteriorXYZ[c - 1], rvtInteriorXYZ[c]);

                    edgesInt.Add(edgeIn);
                }

                CurveLoop innerLoop = CurveLoop.Create(edgesInt);
                rvtPoly.Add(innerLoop);
            }
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

        private static XYZ CalcInteriorFace(List<List<C2BPoint>> intPtsL, /*out List<C2BPoint> extPtsPl,*/ out List<List<C2BPoint>> intPtsLPl)
        {
            intPtsLPl = new List<List<C2BPoint>>();

            //plane polygon needs one normal (balanced out of all segments) for further calculations

            C2BPoint centroidExt = new C2BPoint(0, 0, 0);   //needed later, but can be calculated in first loop

            //Normal calculation
            //------------------

            C2BPoint normalVc = new C2BPoint(0, 0, 0);

            //interior points for normal (not for centroid)
            foreach (var intPts in intPtsL)
            {
                for (var c = 1; c < intPts.Count; c++)
                {
                    var intNormalVc = C2BPoint.CrossProduct(intPts[c - 1], intPts[c]);

                    normalVc += new C2BPoint(-intNormalVc.X, -intNormalVc.Y, -intNormalVc.Z); //turn sign because interior polygon is CW
                }
            }

            C2BPoint normalizedVc = C2BPoint.Normalized(normalVc);
            C2BPoint normalizedVcCW = new C2BPoint(-normalizedVc.X, -normalizedVc.Y, -normalizedVc.Z);

            foreach (var intPts in intPtsL)
            {
                List<C2BPoint> planarIntPts = new List<C2BPoint>();
                C2BPoint centroidIntPl = new C2BPoint(0, 0, 0);

                //at first calculate centroid for interior ring

                for (var c = 1; c < intPts.Count; c++)
                {
                    centroidIntPl += intPts[c];
                }

                C2BPoint centroidI = centroidIntPl / (intPts.Count - 1);

                foreach (var pt in intPts)
                {
                    C2BPoint vecPtCent = pt - centroidI;
                    double d = C2BPoint.ScalarProduct(vecPtCent, normalizedVcCW);

                    C2BPoint vecLotCent = new C2BPoint(d * normalizedVcCW.X, d * normalizedVcCW.Y, d * normalizedVcCW.Z);
                    planarIntPts.Add(pt - vecLotCent);
                }

                intPtsLPl.Add(planarIntPts);
            }

            return new XYZ(normalizedVc.X, normalizedVc.Y, normalizedVc.Z);
        }

        /// <summary>
        /// Calculates point for the needs of Revit DB (feet, PBPtrafo) 
        /// </summary>
        /// <param name="xmlPt">internal Pt</param>
        /// <returns>Revit XYZ pt</returns>
        public static XYZ GetRevPt(C2BPoint xmlPt)
        {
            var revitPt = xmlPt / Prop_Revit.feetToM;

            //Creation of Revit point
            var revitXYZ = new XYZ(revitPt.Y, revitPt.X, revitPt.Z);

            //Transform global coordinate to Revit project coordinate system (system of project base point)
            var revTransXYZ = Prop_Revit.TrafoPBP.OfPoint(revitXYZ);

            return revTransXYZ;
        }
    }
}
