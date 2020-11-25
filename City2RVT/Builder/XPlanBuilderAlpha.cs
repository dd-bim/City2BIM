using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Fabrication;
using Autodesk.Revit.UI;

using City2BIM.Alkis;
using City2BIM.Geometry;
using City2BIM.Semantic;
using City2BIM.XPlanung;
using City2RVT.Calc;

namespace City2RVT.Builder
{
    public class XPlanBuilderAlpha
    {
        ExternalCommandData commandData;
        private readonly Document doc;
        //private readonly Dictionary<ColorType, ElementId> colors;

        public XPlanBuilderAlpha(Document doc, ExternalCommandData cData)
        {
            commandData = cData;
            this.doc = doc;
            //this.colors = CreateColorAsMaterial();
        }

        public void buildRevitObjects(List<XPlanungObject> xPlanungList)
        {
            var queryGroups = from xObj in xPlanungList
                              group xObj by xObj.UsageType into usageGroup
                              select usageGroup;

            foreach(var group in queryGroups)
            {
                ElementId RefPlaneId = new ElementId(BuiltInCategory.OST_TopographySurface);
                string groupName = group.Key;
                List<XPlanungObject> objList = group.ToList();

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

                Schema currentSchema = utils.getSchemaByName(groupName);
                if (objList[0].Geom == XPlanungObject.geomType.Polygon)
                {
                    using (Transaction trans = new Transaction(doc, "Creaet XPlanObjs"))
                    {
                        trans.Start();
                        foreach (var xObj in objList)
                        {
                            var siteSubRegion = createSiteSubRegion(xObj, RefPlaneId);

                            //Add Attributes
                            Entity ent = new Entity(currentSchema);
                            var fieldList = currentSchema.ListFields();
                            var fieldNameList = new List<string>();

                            foreach (var field in fieldList)
                            {
                                fieldNameList.Add(field.FieldName);
                            }

                            Field gmlid = currentSchema.GetField("gmlid");
                            ent.Set<string>(gmlid, xObj.Gmlid);

                            foreach (KeyValuePair<string, string> entry in xObj.Attributes)
                            {
                                if (fieldNameList.Contains(entry.Key))
                                {
                                    Field currentField = currentSchema.GetField(entry.Key);
                                    ent.Set<string>(currentField, entry.Value);
                                }
                            }
                            TopographySurface topoSurface = siteSubRegion.TopographySurface;
                            topoSurface.SetEntity(ent);
                        }
                        trans.Commit();
                    }
                }
            }                            
        }

        private SiteSubRegion createSiteSubRegion(XPlanungObject obj, ElementId hostId)
        {
            List<C2BPoint> polyExt = obj.getOuterRing();

            //obj.Segments.Select(j => j[0]).ToList();
            //polyExt.Add(obj.Segments[0][0]);                                    //convert Segments to LinearRing

            List<List<C2BPoint>> polysInt = obj.getInnerRings();
                //new List<List<C2BPoint>>();

            /*if (obj.InnerSegments != null)
            {
                foreach (var segInt in obj.InnerSegments)
                {
                    List<C2BPoint> polyInt = segInt.Select(j => j[0]).ToList();
                    polyInt.Add(segInt[0][0]);                                    //convert Segments to LinearRing

                    polysInt.Add(polyInt);
                }
            }*/

            List<CurveLoop> loopList = Revit_Build.CreateExteriorCurveLoopList(polyExt, polysInt, out XYZ normal);
            SiteSubRegion siteSubRegion = SiteSubRegion.Create(doc, loopList, hostId);
            return siteSubRegion;
        }

        private static List<C2BPoint> createRefSurfacePointsForObjGroup(List<XPlanungObject> objList)
        {
            List<C2BPoint> pointList = new List<C2BPoint>();
            foreach(var xplanObj in objList)
            {
                foreach (var segment in xplanObj.Segments)
                {
                    foreach (var point in segment)
                    {
                        pointList.Add(point);
                    }
                }
            }

            var ptList = pointList.ConvertAll<Calc.Point>(p => new Calc.Point(p.X, p.Y));
            var convexHull = Calc.ConvexHull.MakeHull(ptList);

            var C2BPtList = convexHull.Select(p => new C2BPoint(p.x, p.y, 0));

            return C2BPtList.ToList();
        }
    }
}
