using System;
using System.Collections.Generic;
using System.Linq;

using Serilog;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.ExtensibleStorage;
using OSGeo.OGR;

using BIMGISInteropLibs.OGR;
using BIMGISInteropLibs.Geometry;
using City2RVT.Calc;

namespace City2RVT.Builder
{
    class GeoObjectBuilder
    {
        private readonly Document doc;

        public GeoObjectBuilder(Document doc)
        {
            GdalConfiguration.ConfigureOgr();
            this.doc = doc;
        }

        public void buildGeoObjectsFromList(List<GeoObject> geoObjects, bool drapeOnTerrain)
        {
            
            var queryGroups = from geoObject in geoObjects
                              group geoObject by geoObject.UsageType into usageGroup
                              select usageGroup;

            ElementId terrainID = utils.getHTWDDTerrainID(doc);

            foreach (var group in queryGroups)
            {
                ElementId RefPlaneId = (terrainID == null) ? null : terrainID;
                string groupName = group.Key;
                List<GeoObject> objList = group.ToList();

                //When no DTM is availabe -> GeoObjects are mapped flat
                if (RefPlaneId == null || drapeOnTerrain == false)
                {
                    var refSurfPts = createRefSurfacePointsForObjGroup(objList);
                    List<C2BPoint> reducedPoints = refSurfPts.Select(p => GeorefCalc.CalcUnprojectedPoint(p, true)).ToList();
                    List<XYZ> RevitPts = reducedPoints.Select(p => Revit_Build.GetRevPt(p)).ToList();

                    using (Transaction trans = new Transaction(doc, "Create RefPlane"))
                    {
                        trans.Start();
                        TopographySurface refSurf = TopographySurface.Create(doc, RevitPts);
                        refSurf.Pinned = true;

                        Parameter nameParam = refSurf.LookupParameter("Name");
                        nameParam.Set("RefPlane_" + groupName);

                        RefPlaneId = refSurf.Id;
                        trans.Commit();
                    }
                }

                //When GeoObjects should be mapped on DTM data
                else
                {
                    using (Transaction trans = new Transaction(doc, "Copy DTM as new RefPlane"))
                    {
                        trans.Start();
                        TopographySurface refSurf = doc.GetElement(RefPlaneId) as TopographySurface;
                        var copyOfRefSurfId = ElementTransformUtils.CopyElement(doc, refSurf.Id, new XYZ(0, 0, 0));

                        TopographySurface copiedRefSurf = doc.GetElement(copyOfRefSurfId.First()) as TopographySurface;
                        copiedRefSurf.Pinned = true;

                        //var label = LabelUtils.GetLabelFor(BuiltInParameter.ALL_MODEL_TYPE_NAME);
                        //Parameter nameParam = copiedRefSurf.LookupParameter(label);
                        //nameParam.Set("RefPlane_" + groupName);
                        Parameter nameParam = copiedRefSurf.LookupParameter("Name");
                        nameParam.Set("RefPlane_" + groupName);

                        RefPlaneId = copiedRefSurf.Id;
                        trans.Commit();
                    }
                }

                Schema currentSchema = utils.getSchemaByName(groupName);

                using (Transaction trans = new Transaction(doc, "Create alkisObjs"))
                {
                    trans.Start();

                    foreach (var GeoObj in objList)
                    {
                        /*
                        Entity ent = new Entity(currentSchema);
                        foreach(KeyValuePair<string, string> prop in GeoObj.Properties)
                        {
                            ent.Set<string>(prop.Key, prop.Value);
                        }
                        */

                        switch (GeoObj.GeomType)
                        {
                            case wkbGeometryType.wkbPolygon:

                                List<C2BSegment> outerSegments = getOuterRingSegmentsFromPolygon(GeoObj.Geom);
                                var siteSubRegion = createSiteSubRegionFromSegments(outerSegments, RefPlaneId);
                                //siteSubRegion.TopographySurface.SetEntity(ent);
                                break;

                            case wkbGeometryType.wkbMultiPolygon:
                                
                                List<List<C2BSegment>> outerSegmentsMultiPoly = getOuterRingSegmentsFromMultiPolygon(GeoObj.Geom);
                                foreach(var poly in outerSegmentsMultiPoly)
                                {
                                    var currentSiteSubRegion = createSiteSubRegionFromSegments(poly, RefPlaneId);
                                    //currentSiteSubRegion.TopographySurface.SetEntity(ent);
                                }
                                break;
                            
                            default:
                                Log.Warning("Could not create GeoObject. GeometryType is: " + GeoObj.GeomType.ToString());
                                break;
                        }
                        
                        
                        //var siteSubRegion = createSiteSubRegion(alkisObj, RefPlaneId);

                        //var label = LabelUtils.GetLabelFor(BuiltInParameter.ALL_MODEL_TYPE_NAME);
                        //Parameter nameParam = siteSubRegion.TopographySurface.LookupParameter(label);
                        //nameParam.Set(groupName);
                        /*
                        Parameter nameParam = siteSubRegion.TopographySurface.LookupParameter("Name");
                        nameParam.Set(groupName);

                        //Add Attributes
                        Entity ent = new Entity(currentSchema);
                        var fieldList = currentSchema.ListFields();
                        var fieldNameList = new List<string>();

                        foreach (var field in fieldList)
                        {
                            fieldNameList.Add(field.FieldName);
                        }

                        Field gmlid = currentSchema.GetField("gmlid");
                        ent.Set<string>(gmlid, alkisObj.Gmlid);

                        foreach (KeyValuePair<Xml_AttrRep, string> entry in alkisObj.Attributes)
                        {
                            if (fieldNameList.Contains(entry.Key.Name))
                            {
                                Field currentField = currentSchema.GetField(entry.Key.Name);
                                ent.Set<string>(currentField, entry.Value);
                            }
                        }

                        TopographySurface topoSurface = siteSubRegion.TopographySurface;
                        topoSurface.SetEntity(ent);
                        */
                    }
                    trans.Commit();
                }

            }



        }

        private static List<C2BPoint> createRefSurfacePointsForObjGroup(List<GeoObject> objList)
        {
            List<Calc.Point> pointList = new List<Calc.Point>();
            
            foreach (var geoObject in objList)
            {
                double[] geoPoint = { 0, 0 };

                for (int i=0; i<geoObject.Geom.GetPointCount(); i++)
                {
                    geoObject.Geom.GetPoint_2D(i, geoPoint);
                    pointList.Add(new Calc.Point(geoPoint[0], geoPoint[1]));
                }
            }

            var convexHull = Calc.ConvexHull.MakeHull(pointList);

            var C2BPtList = convexHull.Select(p => new C2BPoint(p.x, p.y, 0));

            return C2BPtList.ToList();

        }

        private static List<List<C2BSegment>> getOuterRingSegmentsFromMultiPolygon(OSGeo.OGR.Geometry geom)
        {
            var nrOfPolygons = geom.GetGeometryCount();

            var outerRingList = new List<List<C2BSegment>>();

            for (int i=0; i<nrOfPolygons; i++)
            {
                outerRingList.Add(getOuterRingSegmentsFromPolygon(geom.GetGeometryRef(i)));
            }

            return null;
        }

        private static List<C2BSegment> getOuterRingSegmentsFromPolygon(OSGeo.OGR.Geometry geom)
        {
            List<C2BSegment> segments = new List<C2BSegment>();

            var outerRing = geom.GetGeometryRef(0);

            double[] geoPoint = { 0, 0, 0 };

            for (var i = 1; i < outerRing.GetPointCount(); i++)
            {
                outerRing.GetPoint(i - 1, geoPoint);
                C2BPoint startSeg = new C2BPoint(geoPoint[0], geoPoint[1], 0);

                outerRing.GetPoint(i, geoPoint);
                C2BPoint endSeg = new C2BPoint(geoPoint[0], geoPoint[1], 0);

                segments.Add(new C2BSegment(startSeg, endSeg));
            }

            return segments;
        }

        private SiteSubRegion createSiteSubRegionFromSegments(List<C2BSegment> segments, ElementId hostId)
        {
            List<Curve> revitSegmentCurves = new List<Curve>();
            foreach(var seg in segments)
            {
                if (seg.isCurve)
                {
                    var unprojectedPntStart = Calc.GeorefCalc.CalcUnprojectedPoint(seg.startPoint, false, null);
                    var revitPntStart = Revit_Build.GetRevPt(unprojectedPntStart);

                    var unprojectedPntMid = Calc.GeorefCalc.CalcUnprojectedPoint(seg.midPoint, false, null);
                    var revitPntMid = Revit_Build.GetRevPt(unprojectedPntMid);

                    var unprojectedPntEnd = Calc.GeorefCalc.CalcUnprojectedPoint(seg.endPoint, false, null);
                    var revitPntEnd = Revit_Build.GetRevPt(unprojectedPntEnd);

                    revitSegmentCurves.Add(Arc.Create(revitPntStart, revitPntEnd, revitPntMid));
                }

                else
                {
                    var unprojectedPntStart = Calc.GeorefCalc.CalcUnprojectedPoint(seg.startPoint, true, null);
                    var revitPntStart = Revit_Build.GetRevPt(unprojectedPntStart);

                    var unprojectedPntEnd = Calc.GeorefCalc.CalcUnprojectedPoint(seg.endPoint, true, null);
                    var revitPntEnd = Revit_Build.GetRevPt(unprojectedPntEnd);

                    revitSegmentCurves.Add(Line.CreateBound(revitPntStart, revitPntEnd));
                }
            }

            var boundary = new List<CurveLoop>{ CurveLoop.Create(revitSegmentCurves) };

            SiteSubRegion sitesubRegion = SiteSubRegion.Create(this.doc, boundary, hostId);

            return sitesubRegion;

        }

    }
}
