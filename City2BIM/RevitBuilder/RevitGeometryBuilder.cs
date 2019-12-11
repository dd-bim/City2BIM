using Autodesk.Revit.DB;
using City2BIM.GetGeometry;
using City2BIM.GetSemantics;
using City2BIM.GmlRep;
using City2BIM.RevitCommands.City2BIM;
using System;
using System.Collections.Generic;
using System.Linq;

namespace City2BIM.RevitBuilder
{
    internal class RevitGeometryBuilder
    {
        private City2BIM.GetGeometry.C2BPoint gmlCorner;
        private Document doc;

        private List<GmlRep.GmlBldg> buildings;
        private Dictionary<GmlRep.GmlSurface.FaceType, ElementId> colors;


        public RevitGeometryBuilder(Document doc, List<GmlRep.GmlBldg> buildings, GetGeometry.C2BPoint gmlCorner)
        {
            this.doc = doc;
            this.buildings = buildings;
            this.gmlCorner = gmlCorner;
            this.colors = CreateColorAsMaterial();
        }

        #region Solid to Revit incl. LOD1-Fallback

        /// <summary>
        /// Transforms c2b solids to Revit solid (closed volume)
        /// </summary>
        public void CreateBuildings()
        {
            double success = 0.0;
            double error = 0.0;
            double errorLod1 = 0.0;
            double fatalError = 0.0;
            double all = 0.0;

            List<BldgLog> allLogs = new List<BldgLog>();

            foreach (var bldg in buildings)
            {
                foreach (var part in bldg.Parts)
                {
                    if (part.PartSolid.Planes.Count > 0)
                    {
                        all++;

                        try
                        {
                            CreateRevitRepresentation(part.PartSolid.InternalID.ToString(), part.PartSolid, part.PartSurfaces, bldg.BldgAttributes, part.Lod, part.BldgPartAttributes);
                            success++;
                        }
                        catch (System.Exception ex)
                        {
                            bldg.LogEntries.Add(new BldgLog(Logging.LogType.warning, "LOD2-Calculation not successful."));
                            bldg.LogEntries.Add(new BldgLog(Logging.LogType.warning, ex.Message));

                            try
                            {
                                List<BldgLog> lod1Messages = new List<BldgLog>();

                                CreateLOD1Building(part.PartSolid.InternalID.ToString(), part.PartSolid, part.PartSurfaces, bldg.BldgAttributes, ref lod1Messages, ref error, ref errorLod1, part.BldgPartAttributes);
                                bldg.LogEntries.AddRange(lod1Messages);
                                bldg.LogEntries.Add(new BldgLog(Logging.LogType.info, "LOD1-Representation used."));
                            }
                            catch (Exception exx)
                            {
                                bldg.LogEntries.Add(new BldgLog(Logging.LogType.error, "Fatal error: could not calculate LOD1-representation. No geometry transfered."));
                                bldg.LogEntries.Add(new BldgLog(Logging.LogType.error, exx.Message));

                                fatalError++;

                                continue;
                            }
                        }
                    }
                }

                if (bldg.BldgSolid.Planes.Count > 0)
                {
                    try
                    {
                        all++;
                        CreateRevitRepresentation(bldg.BldgSolid.InternalID.ToString(), bldg.BldgSolid, bldg.BldgSurfaces, bldg.BldgAttributes, bldg.Lod);
                        success++;
                    }

                    catch (System.Exception ex)
                    {
                        bldg.LogEntries.Add(new BldgLog(Logging.LogType.warning, "LOD2-Calculation not successful."));
                        bldg.LogEntries.Add(new BldgLog(Logging.LogType.warning, ex.Message));

                        try
                        {
                            List<BldgLog> lod1Messages = new List<BldgLog>();

                            CreateLOD1Building(bldg.BldgSolid.InternalID.ToString(), bldg.BldgSolid, bldg.BldgSurfaces, bldg.BldgAttributes, ref lod1Messages, ref error, ref errorLod1);
                            bldg.LogEntries.AddRange(lod1Messages);
                            bldg.LogEntries.Add(new BldgLog(Logging.LogType.info, "LOD1-Representation used."));
                        }
                        catch (Exception exx)
                        {
                            bldg.LogEntries.Add(new BldgLog(Logging.LogType.error, "Fatal error: could not calculate LOD1-representation. No geometry transfered."));
                            bldg.LogEntries.Add(new BldgLog(Logging.LogType.error, exx.Message));

                            fatalError++;

                            continue;
                        }
                    }
                    allLogs.AddRange(bldg.LogEntries);
                }
            }
            Logging.WriteLogFile(allLogs, all, success, error, errorLod1, fatalError);          
        }

        private void CreateRevitRepresentation(string internalID, C2BSolid solid, List<GmlSurface> surfaces, Dictionary<XmlAttribute, string> bldgAttributes, string lod, Dictionary<XmlAttribute, string> partAttributes = null)
        {
            TessellatedShapeBuilder builder = new TessellatedShapeBuilder();
            builder.OpenConnectedFaceSet(true);

            var tesselatedFaces = CreateRevitFaceList(solid, surfaces);

            foreach (var faceT in tesselatedFaces)
                builder.AddFace(faceT);

            builder.CloseConnectedFaceSet();

            builder.Target = TessellatedShapeBuilderTarget.Solid;

            builder.Fallback = TessellatedShapeBuilderFallback.Abort;

            builder.Build();

            TessellatedShapeBuilderResult result = builder.GetBuildResult();

            using (Transaction t = new Transaction(doc, "Create tessellated direct shape"))
            {
                t.Start();

                DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_Entourage));

                ds.ApplicationId = "Application id";
                ds.ApplicationDataId = "Geometry object id";

                ds.SetShape(result.GetGeometricalObjects());

                ds = SetAttributeValues(ds, bldgAttributes);

                if (partAttributes != null)
                    ds = SetAttributeValues(ds, partAttributes);
                ds.Pinned = true;

                SetRevitInternalParameters(internalID, lod, ds);

                t.Commit();
            }
        }

        private void SetRevitInternalParameters(string guid, string comment, DirectShape ds)
        {
            string commAttrLabel = LabelUtils.GetLabelFor(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
            Parameter commAttr = ds.LookupParameter(commAttrLabel);
            commAttr.Set(comment);

            string idAttrLabel = LabelUtils.GetLabelFor(BuiltInParameter.ALL_MODEL_MARK);
            Parameter idAttr = ds.LookupParameter(idAttrLabel);
            idAttr.Set(guid);
        }

        private void CreateLOD1Building(string internalID, C2BSolid solid, List<GmlSurface> surfaces, Dictionary<XmlAttribute, string> attributes, ref List<BldgLog> logEntries, ref double error, ref double errorLod1, Dictionary<XmlAttribute, string> partAttributes = null)
        {
            var ordByHeight = from v in solid.Vertices
                              where v.Position != null
                              orderby v.Position.Z
                              select v.Position.Z;

            var height = ordByHeight.LastOrDefault() - ordByHeight.FirstOrDefault();

            var groundSurfaces = (from p in surfaces
                                  where p.Facetype == GmlSurface.FaceType.ground
                                  select p).ToList();

            var outerCeilingSurfaces = (from p in surfaces
                                        where p.Facetype == GmlSurface.FaceType.outerCeiling
                                        select p).ToList();

            if (outerCeilingSurfaces.Count > 0)
            {
                groundSurfaces.AddRange(outerCeilingSurfaces);
            }

            int i = 0;  //counter for ID (if more than one groundSurface, ID gets counter suffix: "_i")

            foreach (var groundSurface in groundSurfaces)
            {
                string id = "";

                if (groundSurfaces.Count > 1)
                {
                    id = internalID + "_" + i;
                }
                else
                    id = internalID;

                List<CurveLoop> loopList = Revit_Build.CreateCurveLoopList(groundSurface.ExteriorPts, groundSurface.InteriorPts, out XYZ normal, gmlCorner);

                if (groundSurface.Facetype == GmlSurface.FaceType.outerCeiling)
                {
                    height = ordByHeight.LastOrDefault() - groundSurface.ExteriorPts.First().Z;     //other height than groundSurface because OuterCeiling - ground face is upper than GroundSurface
                }

                var extrHeight = height / Prop.feetToM;

                try
                {
                    Solid lod1bldg = GeometryCreationUtilities.CreateExtrusionGeometry(loopList, -normal, extrHeight);

                    using (Transaction t = new Transaction(doc, "Create lod1 extrusion"))
                    {
                        t.Start();
                        // create direct shape and assign the sphere shape
                        DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_Entourage));

                        ds.SetShape(new GeometryObject[] { lod1bldg });

                        ds = SetAttributeValues(ds, attributes);
                        if (partAttributes != null)
                            ds = SetAttributeValues(ds, partAttributes);
                        ds.Pinned = true;

                        SetRevitInternalParameters(id, "LOD1 (Fallback from LOD2)", ds);

                        t.Commit();
                    }

                    error++;
                }
                catch (Exception ex)
                {
                    logEntries.Add(new BldgLog(Logging.LogType.warning, "Could not calculate LOD1-representation with GroundSurface. Try to calculate Convex Hull instead."));
                    logEntries.Add(new BldgLog(Logging.LogType.warning, ex.Message));


                    //if calculation failed the looplist does not fullfil the requirements for a polygon
                    //the reason is a wrong stored GroundSurface in the CityGml data
                    //this is often the case when inner rings are not stored as inner rings but as part of exterior rings with one or more same edges
                    //in this case the fallback here is to compute the convex hull as an rough approximation of the polygon extent

                    var origExtPts = groundSurface.ExteriorPts;

                    var ptList = new List<Point>();

                    foreach (var pt in origExtPts)
                    {
                        var p = new Point(pt.X, pt.Y);
                        ptList.Add(p);
                    }

                    var groundHeight = origExtPts.First().Z;

                    var convexHull = ConvexHull.MakeHull(ptList);

                    var exteriorHull = new List<C2BPoint>();
                    var interiorHull = new List<List<C2BPoint>>();

                    foreach (var vtx in convexHull)
                    {
                        exteriorHull.Add(new C2BPoint(vtx.x, vtx.y, groundHeight));
                    }
                    exteriorHull.Add(new C2BPoint(convexHull.First().x, convexHull.First().y, groundHeight)); //for closed ring

                    List<CurveLoop> loopListHull = Revit_Build.CreateCurveLoopList(exteriorHull, interiorHull, out XYZ normalHull, gmlCorner);

                    Solid lod1bldg = GeometryCreationUtilities.CreateExtrusionGeometry(loopListHull, -normalHull, extrHeight);

                    using (Transaction t = new Transaction(doc, "Create convex hull extrusion"))
                    {
                        t.Start();
                        // create direct shape and assign the sphere shape
                        DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_Entourage));

                        ds.SetShape(new GeometryObject[] { lod1bldg });

                        ds = SetAttributeValues(ds, attributes);
                        if (partAttributes != null)
                            ds = SetAttributeValues(ds, partAttributes);
                        ds.Pinned = true;

                        SetRevitInternalParameters(internalID, "LOD1 Convex Hull (Fallback from LOD1)", ds);

                        t.Commit();
                    }

                    errorLod1++;
                }
                i++;
            }
        }

        private List<TessellatedFace> CreateRevitFaceList(C2BSolid solid, List<GmlSurface> surfaces)
        {
            List<TessellatedFace> faceListT = new List<TessellatedFace>();

            ElementId colorMat = ElementId.InvalidElementId;

            foreach (var plane in solid.Planes) //Planes
            {
                IList<IList<Autodesk.Revit.DB.XYZ>> faceList = new List<IList<XYZ>>(); //neccessary if also interior face will occure

                //var p = plane.Value;

                IList<XYZ> faceExterior = IdentifyFacePoints(plane.Value.Vertices, solid);

                foreach (int[] intFace in plane.Value.InnerVertices)
                {
                    IList<XYZ> faceInterior = IdentifyFacePoints(intFace, solid);
                    faceList.Add(faceInterior);
                }

                //Identify GmlSurface with current plane

                var involvedSurfaces = from pl in surfaces
                                       where plane.Key.Contains(pl.InternalID.ToString())
                                       select pl;

                var faceTypes = involvedSurfaces.Select(f => f.Facetype).Distinct();

                if (faceTypes.Count() == 1)
                    colorMat = colors[faceTypes.Single()];
                else if (faceTypes.Count() > 1)
                {
                    colorMat = colors[faceTypes.First()];
                }

                faceList.Insert(0, faceExterior);       //"normal" exterior faces on first place (Insert important if interior faces are added before)

                var faceT = new TessellatedFace(faceList, colorMat);

                faceListT.Add(faceT);

                //var faceT = new TessellatedFace(face, colorMat);
            }
            return faceListT;
        }

        public IList<XYZ> IdentifyFacePoints(int[] vertsPlane, C2BSolid solid)
        {
            IList<Autodesk.Revit.DB.XYZ> facePoints = new List<XYZ>();

            foreach (int vid in vertsPlane)
            {
                var verticesXYZ = solid.Vertices;

                if (verticesXYZ.Contains(verticesXYZ[vid]))
                {
                    //Transformation for revit
                    var unprojectedPt = GeorefCalc.CalcUnprojectedPoint(verticesXYZ[vid].Position, City2BIM_prop.IsGeodeticSystem, gmlCorner);

                    var revPt = Revit_Build.GetRevPt(unprojectedPt);

                    facePoints.Add(revPt);
                }
            }

            return facePoints;
        }

        #endregion Solid to Revit incl. LOD1-Fallback

        #region Surfaces to Revit
        public void CreateBuildingsWithFaces()
        {
            double success = 0.0;
            double error = 0.0;
            double all = 0.0;

            foreach (var building in buildings)
            {
                foreach (var part in building.Parts)
                {
                    foreach (var surface in part.PartSurfaces)
                    {
                        all++;

                        try
                        {
                            CreateRevitFaceRepresentation(surface, building.BldgAttributes, part.Lod, part.BldgPartAttributes);
                            success++;

                        }
                        catch (Exception ex)
                        {
                            error++;
                            //WriteRevitBuildErrorsToLog(results, ex.Message, building.BldgId, part.BldgPartId, surface.SurfaceId, surface.Facetype.ToString());
                            continue;
                        }
                    }
                }

                foreach (var surface in building.BldgSurfaces)
                {
                    all++;

                    try
                    {
                        CreateRevitFaceRepresentation(surface, building.BldgAttributes, building.Lod);
                        success++;
                    }
                    catch (Exception ex)
                    {
                        error++;
                        //WriteRevitBuildErrorsToLog(results, ex.Message, building.BldgId, null, surface.SurfaceId, surface.Facetype.ToString());
                        continue;
                    }
                }
            }
            //-------------------

            //WriteStatisticToLog(all, success, null, error);

            //-----------------------

        }

        private void CreateRevitFaceRepresentation(GmlSurface surface, Dictionary<XmlAttribute, string> bldgAttributes, string lod, Dictionary<XmlAttribute, string> partAttributes = null)
        {
            List<CurveLoop> loopList = Revit_Build.CreateCurveLoopList(surface.ExteriorPts, surface.InteriorPts, out XYZ normal, gmlCorner);

            double height = 0.01 / Prop.feetToM;

            SolidOptions opt = new SolidOptions(colors[surface.Facetype], ElementId.InvalidElementId);

            Solid bldgFaceSolid = GeometryCreationUtilities.CreateExtrusionGeometry(loopList, normal, height, opt);

            using (Transaction t = new Transaction(doc, "Create face extrusion"))
            {
                t.Start();
                // create direct shape and assign the sphere shape

                ElementId elem = new ElementId(BuiltInCategory.OST_GenericModel);

                switch (surface.Facetype)
                {
                    case (GmlSurface.FaceType.roof):
                        elem = new ElementId(BuiltInCategory.OST_Roofs);

                        break;

                    case (GmlSurface.FaceType.wall):
                        elem = new ElementId(BuiltInCategory.OST_Walls);
                        break;

                    case (GmlSurface.FaceType.ground):
                        elem = new ElementId(BuiltInCategory.OST_StructuralFoundation);
                        break;

                    case (GmlSurface.FaceType.closure):
                        elem = new ElementId(BuiltInCategory.OST_GenericModel);
                        break;

                    case (GmlSurface.FaceType.outerCeiling):
                        elem = new ElementId(BuiltInCategory.OST_StructuralFoundation);
                        break;

                    case (GmlSurface.FaceType.outerFloor):
                        elem = new ElementId(BuiltInCategory.OST_StructuralFoundation);
                        break;

                    default:
                        break;
                }

                DirectShape ds = DirectShape.CreateElement(doc, elem);

                ds.SetShape(new GeometryObject[] { bldgFaceSolid });

                SetRevitInternalParameters(surface.InternalID.ToString(), lod, ds);

                ds = SetAttributeValues(ds, bldgAttributes);
                if (partAttributes != null)
                    ds = SetAttributeValues(ds, partAttributes);
                ds = SetAttributeValues(ds, surface.SurfaceAttributes);

                ds.Pinned = true;

                t.Commit();
            }
        }
        #endregion Surfaces to Revit incl. Fallback

        #region Attributes and Colors to Revit

        private DirectShape SetAttributeValues(DirectShape ds, Dictionary<XmlAttribute, string> attributes)
        {
            var attr = attributes.Keys;

            foreach (var aName in attr)
            {
                var p = ds.LookupParameter(aName.XmlNamespace + ": " + aName.Name);
                attributes.TryGetValue(aName, out var val);

                try
                {
                    if (val != null)
                    {
                        switch (aName.XmlType)
                        {
                            case (XmlAttribute.AttrType.intAttribute):
                                p.Set(int.Parse(val));
                                break;

                            case (XmlAttribute.AttrType.doubleAttribute):
                                p.Set(double.Parse(val, System.Globalization.CultureInfo.InvariantCulture));
                                break;

                            case (XmlAttribute.AttrType.measureAttribute):
                                var valNew = double.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
                                p.Set(valNew * 3.28084);    //Revit-DB speichert alle Längenmaße in Fuß, hier hart kodierte Umerechnung, Annahme: CityGML speichert Meter
                                break;

                            case (XmlAttribute.AttrType.stringAttribute):
                                p.Set(CheckForCodeTranslation(aName.Name, val, City2BIM_prop.CodelistName));
                                break;

                            default:
                                p.Set(val);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    //Log.Error("Semantik-Fehler bei " + aName.Name + " Error: " + ex.Message);
                    continue;
                }
            }

            return ds;
        }

        private string CheckForCodeTranslation(string gmlAttr, string rawValue, Codelist codeType)
        {
            if (codeType == Codelist.none)
                return rawValue;

            if (codeType == Codelist.adv)
            {
                if (gmlAttr != "function" &&
                    gmlAttr != "roofType" &&
                    gmlAttr != "DatenquelleDachhoehe" &&
                    gmlAttr != "DatenquelleLage" &&
                    gmlAttr != "DatenquelleBodenhoehe" &&
                    gmlAttr != "BezugspunktDach")
                {
                    return rawValue;
                }
                else
                {
                    Dictionary<string, string> dict = new Dictionary<string, string>();

                    if (gmlAttr == "function")
                        dict = GmlCodelist.adv_function;

                    if (gmlAttr == "roofType")
                        dict = GmlCodelist.adv_roofType;

                    if (gmlAttr == "DatenquelleDachhoehe")
                        dict = GmlCodelist.adv_dataRoof;

                    if (gmlAttr == "DatenquelleLage")
                        dict = GmlCodelist.adv_dataLocation;

                    if (gmlAttr == "DatenquelleBodenhoehe")
                        dict = GmlCodelist.adv_dataHeightGround;

                    if (gmlAttr == "BezugspunktDach")
                        dict = GmlCodelist.adv_refPointHeightRoof;

                    dict.TryGetValue(rawValue, out string transValue);

                    if (transValue == "")
                        return rawValue;
                    else
                        return transValue;
                }
            }

            if (codeType == Codelist.sig3d)
            {
                if (gmlAttr != "function" &&
                    gmlAttr != "roofType" &&
                    gmlAttr != "class" &&
                    gmlAttr != "usage")
                {
                    return rawValue;
                }
                else
                {
                    Dictionary<string, string> dict = new Dictionary<string, string>();

                    if (gmlAttr == "function")
                        dict = GmlCodelist.sig3d_function;

                    if (gmlAttr == "roofType")
                        dict = GmlCodelist.sig3d_roofType;

                    if (gmlAttr == "usage")
                        dict = GmlCodelist.sig3d_usage;

                    if (gmlAttr == "class")
                        dict = GmlCodelist.sig3d_class;

                    dict.TryGetValue(rawValue, out string transValue);

                    if (transValue == "")
                        return rawValue;
                    else
                        return transValue;
                }
            }
            return rawValue;
        }

        private Dictionary<GmlRep.GmlSurface.FaceType, ElementId> CreateColorAsMaterial()
        {
            ElementId roofCol = ElementId.InvalidElementId;
            ElementId wallCol = ElementId.InvalidElementId;
            ElementId groundCol = ElementId.InvalidElementId;
            ElementId closureCol = ElementId.InvalidElementId;

            using (Transaction t = new Transaction(doc, "Create material"))
            {
                t.Start();

                var coll = new FilteredElementCollector(doc).OfClass(typeof(Material));
                IEnumerable<Material> materialsEnum = coll.ToElements().Cast<Material>();

                var roofCols
                  = from materialElement in materialsEnum
                    where materialElement.Name == "CityGML_Roof"
                    select materialElement.Id;

                if (roofCols.Count() == 0)
                {
                    roofCol = Material.Create(doc, "CityGML_Roof");
                    Material matRoof = doc.GetElement(roofCol) as Material;
                    matRoof.Color = new Color(255, 0, 0);
                }
                else
                    roofCol = roofCols.First();

                var wallCols
                  = from materialElement in materialsEnum
                    where materialElement.Name == "CityGML_Wall"
                    select materialElement.Id;

                if (wallCols.Count() == 0)
                {
                    wallCol = Material.Create(doc, "CityGML_Wall");
                    Material matWall = doc.GetElement(wallCol) as Material;
                    matWall.Color = new Color(80, 80, 80);
                }
                else
                    wallCol = wallCols.First();

                var groundCols
              = from materialElement in materialsEnum
                where materialElement.Name == "CityGML_Ground"
                select materialElement.Id;

                if (groundCols.Count() == 0)
                {
                    groundCol = Material.Create(doc, "CityGML_Ground");
                    Material matGround = doc.GetElement(groundCol) as Material;
                    matGround.Color = new Color(0, 0, 0);
                }
                else
                    groundCol = groundCols.First();

                var closureCols
                      = from materialElement in materialsEnum
                        where materialElement.Name == "CityGML_Closure"
                        select materialElement.Id;

                if (closureCols.Count() == 0)
                {
                    closureCol = Material.Create(doc, "CityGML_Closure");
                    Material matClosure = doc.GetElement(closureCol) as Material;
                    matClosure.Color = new Color(245, 245, 245);
                }
                else
                    closureCol = closureCols.First();

                t.Commit();
            }

            var colorList = new Dictionary<GmlRep.GmlSurface.FaceType, ElementId>
            {
                { GmlRep.GmlSurface.FaceType.roof, roofCol },
                { GmlRep.GmlSurface.FaceType.wall, wallCol },
                { GmlRep.GmlSurface.FaceType.ground, groundCol },
                { GmlRep.GmlSurface.FaceType.outerCeiling, groundCol },
                { GmlRep.GmlSurface.FaceType.outerFloor, groundCol },
                { GmlRep.GmlSurface.FaceType.closure, closureCol },
                { GmlRep.GmlSurface.FaceType.unknown, wallCol }
            };

            return colorList;
        }
    }

    #endregion Attributes and Colors to Revit
}