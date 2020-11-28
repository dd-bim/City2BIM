using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Architecture;

using City2BIM.Geometry;
using City2BIM.Alkis;
using City2BIM.Semantic;
using City2RVT.Calc;

namespace City2RVT.Builder
{
    class AlkisBuilder
    {
        private readonly Document doc;

        public AlkisBuilder(Document doc)
        {
            this.doc = doc;
        }

        public void buildRevitObjectsFromAlkisList(List<AX_Object> alkisList, bool drapeOnTerrain)
        {
            var queryGroups = from aaaObj in alkisList
                              group aaaObj by aaaObj.UsageType into usageGroup
                              select usageGroup;

            ElementId terrainID = utils.getHTWDDTerrainID(doc);

            foreach (var group in queryGroups)
            {
                ElementId RefPlaneId = (terrainID == null) ? null : terrainID;
                string groupName = group.Key;
                List<AX_Object> objList = group.ToList();

                //When no DTM is availabe -> alkisObjects are mapped flat
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

                //When alkisObjects should be mapped on DTM data
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
                    foreach (var alkisObj in objList)
                    {
                        var siteSubRegion = createSiteSubRegion(alkisObj, RefPlaneId);

                        //var label = LabelUtils.GetLabelFor(BuiltInParameter.ALL_MODEL_TYPE_NAME);
                        //Parameter nameParam = siteSubRegion.TopographySurface.LookupParameter(label);
                        //nameParam.Set(groupName);
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
                    }
                    trans.Commit();
                }

            }
        }

        private SiteSubRegion createSiteSubRegion(AX_Object obj, ElementId hostId)
        {
            List<C2BPoint> polyExt = obj.getOuterRing();
            List<List<C2BPoint>> polysInt = obj.getInnerRings();

            List<CurveLoop> loopList = Revit_Build.CreateExteriorCurveLoopList(polyExt, polysInt, out XYZ normal);
            SiteSubRegion siteSubRegion = SiteSubRegion.Create(doc, loopList, hostId);
            return siteSubRegion;
        }

        private static List<C2BPoint> createRefSurfacePointsForObjGroup(List<AX_Object> objList)
        {
            List<C2BPoint> pointList = new List<C2BPoint>();
            foreach (var xplanObj in objList)
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
