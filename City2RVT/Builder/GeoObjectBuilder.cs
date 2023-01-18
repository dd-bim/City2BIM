using System;
using System.Collections.Generic;
using System.Linq;

using Serilog;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;
using OSGeo.OGR;

using BIMGISInteropLibs.OGR;
using BIMGISInteropLibs.Geometry;
using CityBIM.Calc;

namespace CityBIM.Builder
{
    class GeoObjectBuilder
    {
        private readonly Document doc;
        private readonly View3D view3D;

        public GeoObjectBuilder(Document doc)
        {
            this.doc = doc;
            //get default 3d view --> required for reference intersector between dem and gis-data 
            this.view3D = new FilteredElementCollector(doc).OfClass(typeof(View3D)).ToElements()
                                .Cast<View3D>().FirstOrDefault(v => v != null && !v.IsTemplate && v.Name.Equals("{3D}"));
        }

        public void buildGeoObjectsFromList(List<GeoObject> geoObjects, bool drapeOnTerrain, List<string> fieldList)
        {
            
            var queryGroups = from geoObject in geoObjects
                              group geoObject by geoObject.UsageType into usageGroup
                              select usageGroup;

            ElementId terrainID = utils.getHTWDDTerrainID(doc);

            var refPlaneDataStorage = utils.getRefPlaneDataStorageObject(doc);

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

                        var entity = refPlaneDataStorage.GetEntity(utils.getSchemaByName("HTWDD_RefPlaneSchema"));
                        IDictionary<ElementId, string> value = entity.Get <IDictionary<ElementId, string>>("RefPlaneElementIdToString");
                        value.Add(refSurf.Id, groupName);
                        entity.Set<IDictionary<ElementId, string>>("RefPlaneElementIdToString", value);
                        refPlaneDataStorage.SetEntity(entity);

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

                        var entity = refPlaneDataStorage.GetEntity(utils.getSchemaByName("HTWDD_RefPlaneSchema"));
                        IDictionary<ElementId, string> value = entity.Get<IDictionary<ElementId, string>>("RefPlaneElementIdToString");
                        value.Add(copiedRefSurf.Id, groupName);
                        entity.Set("RefPlaneElementIdToString", value);
                        refPlaneDataStorage.SetEntity(entity);

                        RefPlaneId = copiedRefSurf.Id;
                        trans.Commit();
                    }
                }

                using (Transaction trans = new Transaction(doc, "Create alkisObjs"))
                {
                    trans.Start();
                    
                    Schema currentSchema = utils.getSchemaByName(groupName);

                    if (currentSchema == null)
                    {
                        currentSchema = addSchemaForUsageType(groupName, fieldList);
                    }

                    bool processingErrors = false;
                    foreach (var GeoObj in objList)
                    {
                        
                        Entity ent = new Entity(currentSchema);
                        foreach(KeyValuePair<string, string> prop in GeoObj.Properties)
                        {
                            ent.Set<string>(prop.Key, prop.Value);
                        }
                        

                        switch (GeoObj.GeomType)
                        {
                            case wkbGeometryType.wkbPolygon:
                                { 
                                    List<C2BSegment> outerSegments = getOuterRingSegmentsFromPolygon(GeoObj.Geom);
                                    var siteSubRegion = createSiteSubRegionFromSegments(outerSegments, RefPlaneId);
                                    siteSubRegion.TopographySurface.SetEntity(ent);
                                    break;
                                }

                            case wkbGeometryType.wkbMultiPolygon:
                                { 
                                    List<List<C2BSegment>> outerSegmentsMultiPoly = getOuterRingSegmentsFromMultiPolygon(GeoObj.Geom);
                                    foreach(var poly in outerSegmentsMultiPoly)
                                    {
                                        var currentSiteSubRegion = createSiteSubRegionFromSegments(poly, RefPlaneId);
                                        currentSiteSubRegion.TopographySurface.SetEntity(ent);
                                    }
                                    break;
                                }
                            
                            case wkbGeometryType.wkbCurvePolygon:
                                { 
                                    if (GeoObj.Geom.HasCurveGeometry(1) == 0)
                                    {
                                        List<C2BSegment> outerSegments = getOuterRingSegmentsFromPolygon(GeoObj.Geom);
                                        var siteSubRegion = createSiteSubRegionFromSegments(outerSegments, RefPlaneId);
                                        siteSubRegion.TopographySurface.SetEntity(ent);
                                        break;
                                    }

                                    List<C2BSegment> outerSegmentsCurvePoly = getOuterRingSegmentsFromCurvePolygon(GeoObj.Geom);
                                    var siteSubRegionCurvePoly = createSiteSubRegionFromSegments(outerSegmentsCurvePoly, RefPlaneId);
                                    siteSubRegionCurvePoly.TopographySurface.SetEntity(ent);
                                    break;
                                }
                            case wkbGeometryType.wkbMultiSurface:
                                {
                                    for (int i=0; i<GeoObj.Geom.GetGeometryCount(); i++)
                                    {
                                        var subGeom = GeoObj.Geom.GetGeometryRef(i);
                                        
                                        switch (subGeom.GetGeometryType())
                                        {
                                            case wkbGeometryType.wkbPolygon:
                                                {
                                                    List<C2BSegment> outerSegments = getOuterRingSegmentsFromPolygon(subGeom);
                                                    var siteSubRegion = createSiteSubRegionFromSegments(outerSegments, RefPlaneId);
                                                    siteSubRegion.TopographySurface.SetEntity(ent);
                                                    break;
                                                }
                                            case wkbGeometryType.wkbMultiPolygon:
                                                {
                                                    List<List<C2BSegment>> outerSegmentsMultiPoly = getOuterRingSegmentsFromMultiPolygon(subGeom);
                                                    foreach (var poly in outerSegmentsMultiPoly)
                                                    {
                                                        var currentSiteSubRegion = createSiteSubRegionFromSegments(poly, RefPlaneId);
                                                        currentSiteSubRegion.TopographySurface.SetEntity(ent);
                                                    }
                                                    break;
                                                }

                                            case wkbGeometryType.wkbCurvePolygon:
                                                {
                                                    List<C2BSegment> outerSegmentsCurvePoly = getOuterRingSegmentsFromCurvePolygon(subGeom);
                                                    var siteSubRegionCurvePoly = createSiteSubRegionFromSegments(outerSegmentsCurvePoly, RefPlaneId);
                                                    siteSubRegionCurvePoly.TopographySurface.SetEntity(ent);
                                                    break;
                                                }
                                            default:
                                                Log.Error("Could not create GeoObject from MultiSurface. GeometryType is: " + GeoObj.GeomType.ToString() + " gmlId: " + GeoObj.GmlID);
                                                processingErrors = true;
                                                break;
                                        }

                                    }

                                    break;
                                }

                            case wkbGeometryType.wkbCompoundCurve:
                                {
                                    List<C2BSegment> lineSegs = getSegmentsFromCompoundCurve(GeoObj.Geom);
                                    var modelCurveList = createModelCurveFromSegments(lineSegs, drapeOnTerrain, terrainID);
                                    foreach (var modelCurve in modelCurveList)
                                    {
                                        modelCurve.SetEntity(ent);
                                    }
                                    
                                    break;
                                }

                            case wkbGeometryType.wkbLineString:
                                {
                                    if (GeoObj.Geom.HasCurveGeometry(1) == 1)
                                    {
                                        Log.Error("Could not create GeoObject. Object has curve geometry in wkbLineString. This is not implemented");
                                    }
                                    var segments = getSegmentsFromLinearLineString(GeoObj.Geom);
                                    var modelCurveList = createModelCurveFromSegments(segments, drapeOnTerrain, terrainID);
                                    foreach (var modelCurve in modelCurveList)
                                    {
                                        modelCurve.SetEntity(ent);
                                    }

                                    break;
                                }

                            default:
                                Log.Error("Could not create GeoObject. GeometryType is: " + GeoObj.GeomType.ToString() + " gmlId: " + GeoObj.GmlID);
                                processingErrors = true;
                                break;
                        }                        
                    }

                    // delete refPlane if for object type only line geometrys are added --> modeld as model lines --> no topography surfaces needed
                    var refTopoSurface = this.doc.GetElement(RefPlaneId) as TopographySurface;
                    if (refTopoSurface.GetHostedSubRegionIds().Count < 1)
                    {
                        refTopoSurface.Pinned = false;
                        var deletedElementIds = this.doc.Delete(RefPlaneId);
                    }

                    trans.Commit();
                    if (processingErrors)
                    {
                        TaskDialog.Show("Warning", string.Format("Some Objects of layer {0} could not be imported into Revit. See log-file for further information", groupName));
                    }
                }

            }
        }

        private static List<C2BPoint> createRefSurfacePointsForObjGroup(List<GeoObject> objList)
        {
            List<Calc.Point> pointList = new List<Calc.Point>();
            var geometryCollection = new OSGeo.OGR.Geometry(wkbGeometryType.wkbGeometryCollection);

            foreach (var geoObject in objList)
            {
                geometryCollection.AddGeometry(geoObject.Geom);
            }

            var convexHull = geometryCollection.ConvexHull();

            if (convexHull.HasCurveGeometry(0) != 0)
            {
                convexHull = convexHull.GetLinearGeometry(0, null);
            }

            var outerRing = convexHull.GetGeometryRef(0);

            // TODO: Buffer convex hull geometry to be on the save side for intersections

            double[] geoPoint = { 0, 0 };

            //count()-1 needed since no duplicate points are allowed in revit topo surface creation
            for (int i = 0; i < outerRing.GetPointCount()-1; i++)
            {
                outerRing.GetPoint_2D(i, geoPoint);
                pointList.Add(new Calc.Point(geoPoint[0], geoPoint[1]));
            }

            var C2BPtList = pointList.Select(p => new C2BPoint(p.x, p.y, 0));
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

        private static List<C2BSegment> getOuterRingSegmentsFromCurvePolygon(OSGeo.OGR.Geometry geom)
        {
            List<C2BSegment> segments = new List<C2BSegment>();

            var outerRing = geom.GetGeometryRef(0);

            double[] geoPoint = { 0, 0, 0 };

            for (int i=0; i<outerRing.GetGeometryCount(); i++)
            {
                var sectionGeom = outerRing.GetGeometryRef(i);
                
                switch (sectionGeom.GetGeometryType())
                {
                    case wkbGeometryType.wkbCircularString:

                        //The number of control points in the arc string must be 2 * numArc + 1.
                        int nrOfPoints = sectionGeom.GetPointCount();
                        int nrOfArcSegments = (nrOfPoints - 1) / 2;

                        for (int j = 0, l = 0; j < nrOfArcSegments; j++, l += 2)
                        {
                            sectionGeom.GetPoint(l, geoPoint);
                            var startPoint = new C2BPoint(geoPoint[0], geoPoint[1], 0);

                            sectionGeom.GetPoint(l + 1, geoPoint);
                            var midPoint = new C2BPoint(geoPoint[0], geoPoint[1], 0);

                            sectionGeom.GetPoint(l + 2, geoPoint);
                            var endPoint = new C2BPoint(geoPoint[0], geoPoint[1], 0);

                            segments.Add(new C2BSegment(startPoint, endPoint, midPoint));
                        }

                        break;
                    case wkbGeometryType.wkbLineString:

                        for (var j = 1; j < sectionGeom.GetPointCount(); j++)
                        {
                            sectionGeom.GetPoint(j - 1, geoPoint);
                            C2BPoint startSeg = new C2BPoint(geoPoint[0], geoPoint[1], 0);

                            sectionGeom.GetPoint(j, geoPoint);
                            C2BPoint endSeg = new C2BPoint(geoPoint[0], geoPoint[1], 0);

                            segments.Add(new C2BSegment(startSeg, endSeg));
                        }

                        break;

                    default:
                        Log.Error(string.Format("Could not process geometry within CurvePolygon! Geometry is of type {0}", sectionGeom.GetGeometryType()));
                        break;
                }
            }

            return segments;
        }

        private static List<C2BSegment> getSegmentsFromCompoundCurve(OSGeo.OGR.Geometry geom)
        {
            List<C2BSegment> segments = new List<C2BSegment>();
            double[] geoPoint = { 0, 0 };

            List<C2BSegment> segs = new List<C2BSegment>();
            for (int i=0; i<geom.GetGeometryCount(); i++)
            {
                var sectionGeom = geom.GetGeometryRef(i);

                switch (sectionGeom.GetGeometryType())
                {
                    case wkbGeometryType.wkbCircularString:
                        segs = getSegmentsFromCircularLineString(sectionGeom);
                        break;

                    case wkbGeometryType.wkbLineString:
                        segs = getSegmentsFromLinearLineString(sectionGeom);
                        break;

                    default:
                        Log.Error(string.Format("Could not process geometry from Compound Curve!"));
                        break;
                }
                segments.AddRange(segs);
            }
            return segments;
        }

        private static List<C2BSegment> getSegmentsFromLinearLineString(OSGeo.OGR.Geometry geom)
        {
            List<C2BSegment> segments = new List<C2BSegment>();
            double[] geoPoint = { 0, 0 };

            for (var j = 1; j < geom.GetPointCount(); j++)
            {
                geom.GetPoint(j - 1, geoPoint);
                C2BPoint startSeg = new C2BPoint(geoPoint[0], geoPoint[1], 0);

                geom.GetPoint(j, geoPoint);
                C2BPoint endSeg = new C2BPoint(geoPoint[0], geoPoint[1], 0);

                segments.Add(new C2BSegment(startSeg, endSeg));
            }

            return segments;
        }

        private static List<C2BSegment> getSegmentsFromCircularLineString (OSGeo.OGR.Geometry geom)
        {
            List<C2BSegment> segments = new List<C2BSegment>();
            double[] geoPoint = { 0, 0 };

            //The number of control points in the arc string must be 2 * numArc + 1.
            int nrOfPoints = geom.GetPointCount();
            int nrOfArcSegments = (nrOfPoints - 1) / 2;

            for (int j = 0, l = 0; j < nrOfArcSegments; j++, l += 2)
            {
                geom.GetPoint(l, geoPoint);
                var startPoint = new C2BPoint(geoPoint[0], geoPoint[1], 0);

                geom.GetPoint(l + 1, geoPoint);
                var midPoint = new C2BPoint(geoPoint[0], geoPoint[1], 0);

                geom.GetPoint(l + 2, geoPoint);
                var endPoint = new C2BPoint(geoPoint[0], geoPoint[1], 0);

                segments.Add(new C2BSegment(startPoint, endPoint, midPoint));
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
                    var unprojectedPntStart = Calc.GeorefCalc.CalcUnprojectedPoint(seg.startPoint, true, null);
                    var revitPntStart = Revit_Build.GetRevPt(unprojectedPntStart);

                    var unprojectedPntMid = Calc.GeorefCalc.CalcUnprojectedPoint(seg.midPoint, true, null);
                    var revitPntMid = Revit_Build.GetRevPt(unprojectedPntMid);

                    var unprojectedPntEnd = Calc.GeorefCalc.CalcUnprojectedPoint(seg.endPoint, true, null);
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

        private List<ModelCurve> createModelCurveFromSegments(List<C2BSegment> segments, bool drapeOnTerrain, ElementId terrainId)
        {
            List<ModelCurve> curveList = new List<ModelCurve>();
            if (drapeOnTerrain)
            {
                foreach (var seg in segments)
                {
                    if (seg.isCurve)
                    {
                        var unprojectedPntStart = CityBIM.Calc.GeorefCalc.CalcUnprojectedPoint(seg.startPoint, true, null);
                        var revitPntStart = Revit_Build.GetRevPt(unprojectedPntStart);

                        var unprojectedPntEnd = CityBIM.Calc.GeorefCalc.CalcUnprojectedPoint(seg.endPoint, true, null);
                        var revitPntEnd = Revit_Build.GetRevPt(unprojectedPntEnd);

                        var unprojectedPntMid = CityBIM.Calc.GeorefCalc.CalcUnprojectedPoint(seg.midPoint, true, null);
                        var revitPntMid = Revit_Build.GetRevPt(unprojectedPntMid);

                        XYZ upDirection = new XYZ(0.0, 0.0, 1.0);

                        ReferenceIntersector rfi = new ReferenceIntersector(terrainId, FindReferenceTarget.All, this.view3D);
                        ReferenceWithContext nearestIntersection = rfi.FindNearest(revitPntStart, upDirection);

                        Reference reference = nearestIntersection.GetReference();
                        var intersectionPointStart = reference.GlobalPoint;

                        nearestIntersection = rfi.FindNearest(revitPntEnd, upDirection);
                        reference = nearestIntersection.GetReference();
                        var intersectionPointEnd = reference.GlobalPoint;

                        nearestIntersection = rfi.FindNearest(revitPntMid, upDirection);
                        reference = nearestIntersection.GetReference();
                        var intersectionPointMid = reference.GlobalPoint;

                        Arc result = Arc.Create(intersectionPointStart, intersectionPointEnd, intersectionPointMid);

                        Plane plane = Plane.CreateByThreePoints(intersectionPointMid, intersectionPointEnd, intersectionPointStart);
                        SketchPlane skPlane = SketchPlane.Create(doc, plane);
                        ModelArc arc = doc.Create.NewModelCurve(result, skPlane) as ModelArc;

                        curveList.Add(arc);
                    }
                    else
                    {
                        var unprojectedPntStart = CityBIM.Calc.GeorefCalc.CalcUnprojectedPoint(seg.startPoint, true, null);
                        var revitPntStart = Revit_Build.GetRevPt(unprojectedPntStart);

                        var unprojectedPntEnd = CityBIM.Calc.GeorefCalc.CalcUnprojectedPoint(seg.endPoint, true, null);
                        var revitPntEnd = Revit_Build.GetRevPt(unprojectedPntEnd);

                        XYZ upDirection = new XYZ(0.0, 0.0, 1.0);

                        ReferenceIntersector rfi = new ReferenceIntersector(terrainId, FindReferenceTarget.All, this.view3D);
                        ReferenceWithContext nearestIntersection = rfi.FindNearest(revitPntStart, upDirection);

                        Reference reference = nearestIntersection.GetReference();
                        var intersectionPointStart = reference.GlobalPoint;

                        nearestIntersection = rfi.FindNearest(revitPntEnd, upDirection);
                        reference = nearestIntersection.GetReference();
                        var intersectionPointEnd = reference.GlobalPoint;

                        Line result = Line.CreateBound(intersectionPointStart, intersectionPointEnd);

                        XYZ normal = intersectionPointStart.CrossProduct(intersectionPointEnd);
                        XYZ origin = intersectionPointStart;

                        Plane plane = Plane.CreateByNormalAndOrigin(normal, origin);
                        SketchPlane skPlane = SketchPlane.Create(doc, plane);
                        ModelLine line = doc.Create.NewModelCurve(result, skPlane) as ModelLine;

                        curveList.Add(line);
                    }
                }
            }
            else
            {
                foreach(var seg in segments)
                {
                    if (seg.isCurve)
                    {
                        var unprojectedPntStart = CityBIM.Calc.GeorefCalc.CalcUnprojectedPoint(seg.startPoint, true, null);
                        var revitPntStart = Revit_Build.GetRevPt(unprojectedPntStart);

                        var unprojectedPntEnd = CityBIM.Calc.GeorefCalc.CalcUnprojectedPoint(seg.endPoint, true, null);
                        var revitPntEnd = Revit_Build.GetRevPt(unprojectedPntEnd);

                        var unprojectedPntMid = CityBIM.Calc.GeorefCalc.CalcUnprojectedPoint(seg.midPoint, true, null);
                        var revitPntMid = Revit_Build.GetRevPt(unprojectedPntMid);

                        Arc result = Arc.Create(revitPntStart, revitPntEnd, revitPntMid);

                        Plane plane = Plane.CreateByThreePoints(revitPntStart, revitPntMid, revitPntEnd);
                        SketchPlane skPlane = SketchPlane.Create(doc, plane);
                        ModelArc arc = doc.Create.NewModelCurve(result, skPlane) as ModelArc;

                        curveList.Add(arc);
                    }
                    else
                    {
                        var unprojectedPntStart = CityBIM.Calc.GeorefCalc.CalcUnprojectedPoint(seg.startPoint, true, null);
                        var revitPntStart = Revit_Build.GetRevPt(unprojectedPntStart);

                        var unprojectedPntEnd = CityBIM.Calc.GeorefCalc.CalcUnprojectedPoint(seg.endPoint, true, null);
                        var revitPntEnd = Revit_Build.GetRevPt(unprojectedPntEnd);

                        Line result = Line.CreateBound(revitPntStart, revitPntEnd);

                        XYZ normal = revitPntEnd.CrossProduct(revitPntStart);
                        XYZ origin = revitPntEnd;

                        Plane plane = Plane.CreateByNormalAndOrigin(normal, origin);
                        SketchPlane skPlane = SketchPlane.Create(doc, plane);
                        ModelLine line = doc.Create.NewModelCurve(result, skPlane) as ModelLine;

                        curveList.Add(line);
                    }
                }
            }
            return curveList;
        }

        private Schema addSchemaForUsageType(string schemaName, List<string> fieldList)
        {
            SchemaBuilder sb = new SchemaBuilder(Guid.NewGuid());
            sb.SetSchemaName(schemaName);
            sb.SetReadAccessLevel(AccessLevel.Public);
            sb.SetWriteAccessLevel(AccessLevel.Public);
            sb.SetVendorId("HTWDresden");

            foreach (var entry in fieldList)
            {
                if (entry.Contains("|"))
                {
                    string firstPart = entry.Split('|')[0];
                    sb.AddSimpleField(firstPart, typeof(string));
                    continue;
                }
                sb.AddSimpleField(entry, typeof(string));
            }

            return sb.Finish();
        }
 
    }
}
