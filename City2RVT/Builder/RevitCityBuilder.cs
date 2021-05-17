using Autodesk.Revit.DB;
using BIMGISInteropLibs.Geometry;
using BIMGISInteropLibs.Semantic;
using BIMGISInteropLibs.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Collections.ObjectModel;
using City2RVT.GUI;
using Autodesk.Revit.DB.ExtensibleStorage;
using BIMGISInteropLibs.CityGML;

namespace City2RVT.Builder
{
    internal class RevitCityBuilder
    {
        private readonly C2BPoint gmlCorner;
        private readonly Document doc;

        private readonly List<CityGml_Bldg> buildings;
        private readonly Dictionary<CityGml_Surface.FaceType, ElementId> colors;

        private readonly CityGml_Codelist.Codelist CodeTranslation;

        public RevitCityBuilder(Document doc, List<CityGml_Bldg> buildings, C2BPoint gmlCorner, HashSet<BIMGISInteropLibs.Semantic.Xml_AttrRep> attributes, CityGml_Codelist.Codelist codeType = CityGml_Codelist.Codelist.none)
        {
            this.doc = doc;
            this.buildings = buildings;
            this.gmlCorner = gmlCorner;
            this.CodeTranslation = codeType;
            this.colors = CreateColorAsMaterial();

            createCityGMLSchema(this.doc, attributes);

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

            List<LogPair> allLogs = new List<LogPair>();

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
                            bldg.LogEntries.Add(new LogPair(LogType.warning, "LOD2-Calculation not successful."));
                            bldg.LogEntries.Add(new LogPair(LogType.warning, ex.Message));

                            try
                            {
                                List<LogPair> lod1Messages = new List<LogPair>();

                                //___________________________________hier_____________________________________________________
                                CreateLOD1Building(part.PartSolid.InternalID.ToString(), part.PartSolid, part.PartSurfaces, bldg.BldgAttributes, ref lod1Messages, ref error, ref errorLod1, part.BldgPartAttributes);
                                bldg.LogEntries.AddRange(lod1Messages);
                                bldg.LogEntries.Add(new LogPair(LogType.info, "LOD1-Representation used."));
                            }
                            catch (Exception exx)
                            {
                                bldg.LogEntries.Add(new LogPair(LogType.error, "Fatal error: could not calculate LOD1-representation. No geometry transfered."));
                                bldg.LogEntries.Add(new LogPair(LogType.error, exx.Message));

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
                        bldg.LogEntries.Add(new LogPair(LogType.warning, "LOD2-Calculation not successful."));
                        bldg.LogEntries.Add(new LogPair(LogType.warning, ex.Message));

                        try
                        {
                            List<LogPair> lod1Messages = new List<LogPair>();

                            CreateLOD1Building(bldg.BldgSolid.InternalID.ToString(), bldg.BldgSolid, bldg.BldgSurfaces, bldg.BldgAttributes, ref lod1Messages, ref error, ref errorLod1);
                            bldg.LogEntries.AddRange(lod1Messages);
                            bldg.LogEntries.Add(new LogPair(LogType.info, "LOD1-Representation used."));
                        }
                        catch (Exception exx)
                        {
                            bldg.LogEntries.Add(new LogPair(LogType.error, "Fatal error: could not calculate LOD1-representation. No geometry transfered."));
                            bldg.LogEntries.Add(new LogPair(LogType.error, exx.Message));

                            fatalError++;

                            continue;
                        }
                    }
                }
                allLogs.AddRange(bldg.LogEntries);
            }
            LogWriter.WriteLogFile(allLogs, true, all, success, error, errorLod1, fatalError);          
        }

        private void CreateRevitRepresentation(string internalID, C2BSolid solid, List<CityGml_Surface> surfaces, Dictionary<Xml_AttrRep, string> bldgAttributes, string lod, Dictionary<Xml_AttrRep, string> partAttributes = null)
        {
            using (TessellatedShapeBuilder builder = new TessellatedShapeBuilder())
            {
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

                    //add citygml attributes
                    var citySchema = utils.getSchemaByName("CityGMLImportSchema");
                    Entity entity = new Entity(citySchema);

                    if (this.CodeTranslation == CityGml_Codelist.Codelist.none)
                    {
                        foreach (var attribute in bldgAttributes)
                        {
                            Field currentField = citySchema.GetField(attribute.Key.Name);
                            entity.Set<string>(currentField, attribute.Value);
                        }
                    }
                    else
                    {
                        foreach (var attribute in bldgAttributes)
                        {
                            string value = CheckForCodeTranslation(attribute.Key.Name, attribute.Value, this.CodeTranslation);
                            Field currentField = citySchema.GetField(attribute.Key.Name);
                            entity.Set<string>(currentField, value);
                        }
                    }

                    ds.SetEntity(entity);
                    SetRevitInternalParameters(internalID, lod, ds);
                   

                    t.Commit();
                }
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

        private void CreateLOD1Building(string internalID, C2BSolid solid, List<CityGml_Surface> surfaces, Dictionary<Xml_AttrRep, string> attributes, ref List<LogPair> logEntries, ref double error, ref double errorLod1, Dictionary<Xml_AttrRep, string> partAttributes = null)
        {
            var ordByHeight = from v in solid.Vertices
                              where v.Position != null
                              orderby v.Position.Z
                              select v.Position.Z;

            //var ordByHeight = ordByHeight2.ToList();
            //var avg = ordByHeight.Average();

            //foreach (var o in ordByHeight)
            //{
            //    if (o > 2*avg)
            //    {
            //        ordByHeight.Remove(o);
            //    }
            //}

            var height = ordByHeight.LastOrDefault() - ordByHeight.FirstOrDefault();

            var groundSurfaces = (from p in surfaces
                                  where p.Facetype == CityGml_Surface.FaceType.ground
                                  select p).ToList();

            var outerCeilingSurfaces = (from p in surfaces
                                        where p.Facetype == CityGml_Surface.FaceType.outerCeiling
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

                List<CurveLoop> loopList = Revit_Build.CreateExteriorCurveLoopList(groundSurface.ExteriorPts, groundSurface.InteriorPts, out XYZ normal, gmlCorner);

                if (groundSurface.Facetype == CityGml_Surface.FaceType.outerCeiling)
                {
                    height = ordByHeight.LastOrDefault() - groundSurface.ExteriorPts.First().Z;     //other height than groundSurface because OuterCeiling - ground face is upper than GroundSurface
                }

                var extrHeight = height / Prop_Revit.feetToM;

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

                        if (height > 20)
                        {
                            var sv = solid.Vertices;
                            var obh = ordByHeight;
                            var g = groundSurface.ExteriorPts.First().Z;
                            var ol = ordByHeight.LastOrDefault();
                            var of = ordByHeight.FirstOrDefault();
                            var h = height;
                        }

                        //add citygml attributes
                        var citySchema = utils.getSchemaByName("CityGMLImportSchema");
                        Entity entity = new Entity(citySchema);

                        foreach (var attribute in attributes)
                        {
                            Field currentField = citySchema.GetField(attribute.Key.Name);
                            entity.Set<string>(currentField, attribute.Value);
                        }

                        ds.SetEntity(entity);
                        SetRevitInternalParameters(id, "LOD1 (Fallback from LOD2)", ds);
                        

                        t.Commit();
                    }

                    error++;
                }
                catch (Exception ex)
                {
                    logEntries.Add(new LogPair(LogType.warning, "Could not calculate LOD1-representation with GroundSurface. Try to calculate Convex Hull instead."));
                    logEntries.Add(new LogPair(LogType.warning, ex.Message));


                    //if calculation failed the looplist does not fullfil the requirements for a polygon
                    //the reason is a wrong stored GroundSurface in the CityGml data
                    //this is often the case when inner rings are not stored as inner rings but as part of exterior rings with one or more same edges
                    //in this case the fallback here is to compute the convex hull as an rough approximation of the polygon extent

                    var origExtPts = groundSurface.ExteriorPts;

                    var ptList = new List<Calc.Point>();

                    foreach (var pt in origExtPts)
                    {
                        var p = new Calc.Point(pt.X, pt.Y);
                        ptList.Add(p);
                    }

                    var groundHeight = origExtPts.First().Z;

                    var convexHull = Calc.ConvexHull.MakeHull(ptList);

                    var exteriorHull = new List<C2BPoint>();
                    var interiorHull = new List<List<C2BPoint>>();

                    foreach (var vtx in convexHull)
                    {
                        exteriorHull.Add(new C2BPoint(vtx.x, vtx.y, groundHeight));
                    }
                    exteriorHull.Add(new C2BPoint(convexHull.First().x, convexHull.First().y, groundHeight)); //for closed ring

                    List<CurveLoop> loopListHull = Revit_Build.CreateExteriorCurveLoopList(exteriorHull, interiorHull, out XYZ normalHull, gmlCorner);

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

                        //add citygml attributes
                        var citySchema = utils.getSchemaByName("CityGMLImportSchema");
                        Entity entity = new Entity(citySchema);

                        foreach (var attribute in attributes)
                        {
                            Field currentField = citySchema.GetField(attribute.Key.Name);
                            entity.Set<string>(currentField, attribute.Value);
                        }

                        ds.SetEntity(entity);

                        SetRevitInternalParameters(internalID, "LOD1 Convex Hull (Fallback from LOD1)", ds);

                        t.Commit();
                    }

                    errorLod1++;
                }
                i++;
            }
        }

        private List<TessellatedFace> CreateRevitFaceList(C2BSolid solid, List<CityGml_Surface> surfaces)
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
                    var unprojectedPt = Calc.GeorefCalc.CalcUnprojectedPoint(verticesXYZ[vid].Position, Prop_CityGML_settings.IsGeodeticSystem, gmlCorner);

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

            List<LogPair> allLogs = new List<LogPair>();

            using (Transaction t = new Transaction(doc, "Create face extrusion"))
            {
                t.Start();

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
                                building.LogEntries.Add(new LogPair(LogType.error, "Surface transfer not successful at Building Part" + part.BldgPartId));
                                building.LogEntries.Add(new LogPair(LogType.error, "Surface info: Id = " + surface.SurfaceId + ", Type = " + surface.Facetype));
                                building.LogEntries.Add(new LogPair(LogType.error, "Error message: = " + ex.Message));
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
                            building.LogEntries.Add(new LogPair(LogType.error, "Surface info: Id = " + surface.SurfaceId + ", Type = " + surface.Facetype));
                            building.LogEntries.Add(new LogPair(LogType.error, "Error message: = " + ex.Message));
                            continue;
                        }
                    }
                    allLogs.AddRange(building.LogEntries);
                }

                t.Commit();
            }
            LogWriter.WriteLogFile(allLogs, false, all, success, null, null, error);
        }

        private void CreateRevitFaceRepresentation(CityGml_Surface surface, Dictionary<Xml_AttrRep, string> bldgAttributes, string lod, Dictionary<Xml_AttrRep, string> partAttributes = null)
        {
            List<CurveLoop> loopList = Revit_Build.CreateExteriorCurveLoopList(surface.ExteriorPts, surface.InteriorPts, out XYZ normal, gmlCorner);

            double height = 0.01 / Prop_Revit.feetToM;

            SolidOptions opt = new SolidOptions(colors[surface.Facetype], ElementId.InvalidElementId);

            Solid bldgFaceSolid = GeometryCreationUtilities.CreateExtrusionGeometry(loopList, normal, height, opt);

            ElementId elem = new ElementId(BuiltInCategory.OST_GenericModel);

            switch (surface.Facetype)
            {
                case (CityGml_Surface.FaceType.roof):
                    elem = new ElementId(BuiltInCategory.OST_Roofs);

                    break;

                case (CityGml_Surface.FaceType.wall):
                    elem = new ElementId(BuiltInCategory.OST_Walls);
                    break;

                case (CityGml_Surface.FaceType.ground):
                    elem = new ElementId(BuiltInCategory.OST_StructuralFoundation);
                    break;

                case (CityGml_Surface.FaceType.closure):
                    elem = new ElementId(BuiltInCategory.OST_GenericModel);
                    break;

                case (CityGml_Surface.FaceType.outerCeiling):
                    elem = new ElementId(BuiltInCategory.OST_StructuralFoundation);
                    break;

                case (CityGml_Surface.FaceType.outerFloor):
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

            //add citygml attributes
            var citySchema = utils.getSchemaByName("CityGMLImportSchema");
            Entity entity = new Entity(citySchema);

            foreach (var attribute in bldgAttributes)
            {
                Field currentField = citySchema.GetField(attribute.Key.Name);
                entity.Set<string>(currentField, attribute.Value);
            }

            ds.SetEntity(entity);
        }
        #endregion Surfaces to Revit incl. Fallback

        #region Attributes and Colors to Revit

        private DirectShape SetAttributeValues(DirectShape ds, Dictionary<Xml_AttrRep, string> attributes)
        {
            var attr = attributes.Keys;

            foreach (var aName in attr)
            {
                var p = ds.LookupParameter(aName.XmlNamespace + ": " + aName.Name);
                attributes.TryGetValue(aName, out var val);

                if (p != null)
                {
                    try
                    {
                        if (val != null)
                        {
                            switch (aName.XmlType)
                            {
                                case (Xml_AttrRep.AttrType.intAttribute):
                                    p.Set(int.Parse(val));
                                    break;

                                case (Xml_AttrRep.AttrType.doubleAttribute):
                                    p.Set(double.Parse(val, System.Globalization.CultureInfo.InvariantCulture));
                                    break;

                                case (Xml_AttrRep.AttrType.measureAttribute):
                                    var valNew = double.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
                                    p.Set(valNew * 3.28084);    //Revit-DB speichert alle Längenmaße in Fuß, hier hart kodierte Umerechnung, Annahme: CityGML speichert Meter
                                    break;

                                case (Xml_AttrRep.AttrType.stringAttribute):
                                    p.Set(CheckForCodeTranslation(aName.Name, val, this.CodeTranslation));
                                    break;

                                default:
                                    p.Set(val);
                                    break;
                            }
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                //try
                //{
                //    if (val != null)
                //    {
                //        switch (aName.XmlType)
                //        {
                //            case (Xml_AttrRep.AttrType.intAttribute):
                //                p.Set(int.Parse(val));
                //                break;

                //            case (Xml_AttrRep.AttrType.doubleAttribute):
                //                p.Set(double.Parse(val, System.Globalization.CultureInfo.InvariantCulture));
                //                break;

                //            case (Xml_AttrRep.AttrType.measureAttribute):
                //                var valNew = double.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
                //                p.Set(valNew * 3.28084);    //Revit-DB speichert alle Längenmaße in Fuß, hier hart kodierte Umerechnung, Annahme: CityGML speichert Meter
                //                break;

                //            case (Xml_AttrRep.AttrType.stringAttribute):
                //                p.Set(CheckForCodeTranslation(aName.Name, val, Prop_CityGML_settings.CodelistName));
                //                break;

                //            default:
                //                p.Set(val);
                //                break;
                //        }
                //    }
                //}
                //catch
                //{
                //    continue;
                //}
            }

            return ds;
        }

        private string CheckForCodeTranslation(string gmlAttr, string rawValue, CityGml_Codelist.Codelist codeType)
        {
            if (codeType == CityGml_Codelist.Codelist.none)
                return rawValue;

            if (codeType == CityGml_Codelist.Codelist.adv)
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
                        dict = CityGml_Codelist.adv_function;

                    if (gmlAttr == "roofType")
                        dict = CityGml_Codelist.adv_roofType;

                    if (gmlAttr == "DatenquelleDachhoehe")
                        dict = CityGml_Codelist.adv_dataRoof;

                    if (gmlAttr == "DatenquelleLage")
                        dict = CityGml_Codelist.adv_dataLocation;

                    if (gmlAttr == "DatenquelleBodenhoehe")
                        dict = CityGml_Codelist.adv_dataHeightGround;

                    if (gmlAttr == "BezugspunktDach")
                        dict = CityGml_Codelist.adv_refPointHeightRoof;

                    dict.TryGetValue(rawValue, out string transValue);

                    if (transValue == "")
                        return rawValue;
                    else
                        return transValue;
                }
            }

            if (codeType == CityGml_Codelist.Codelist.sig3d)
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
                        dict = CityGml_Codelist.sig3d_function;

                    if (gmlAttr == "roofType")
                        dict = CityGml_Codelist.sig3d_roofType;

                    if (gmlAttr == "usage")
                        dict = CityGml_Codelist.sig3d_usage;

                    if (gmlAttr == "class")
                        dict = CityGml_Codelist.sig3d_class;

                    dict.TryGetValue(rawValue, out string transValue);

                    if (transValue == "")
                        return rawValue;
                    else
                        return transValue;
                }
            }
            return rawValue;
        }

        private Dictionary<CityGml_Surface.FaceType, ElementId> CreateColorAsMaterial()
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

            var colorList = new Dictionary<CityGml_Surface.FaceType, ElementId>
            {
                { CityGml_Surface.FaceType.roof, roofCol },
                { CityGml_Surface.FaceType.wall, wallCol },
                { CityGml_Surface.FaceType.ground, groundCol },
                { CityGml_Surface.FaceType.outerCeiling, groundCol },
                { CityGml_Surface.FaceType.outerFloor, groundCol },
                { CityGml_Surface.FaceType.closure, closureCol },
                { CityGml_Surface.FaceType.unknown, wallCol }
            };

            return colorList;
        }

        private bool createCityGMLSchema(Document doc, HashSet<BIMGISInteropLibs.Semantic.Xml_AttrRep> attributes)
        {
            var citySchema = utils.getSchemaByName("CityGMLImportSchema");

            if (citySchema == null) {

                //Create internal revit extensible storage scheme "CityGMLImportSchema" or check if already exists
                using (Transaction trans = new Transaction(doc, "CityGML Schema Creation"))
                {
                    trans.Start();

                    var existingSchemaList = Schema.ListSchemas();

                    //check if schema already exists
                    foreach (var schema in existingSchemaList)
                    {
                        if (schema.SchemaName == "CityGMLImportSchema")
                        {
                            trans.RollBack();
                            return true;
                        }
                    }

                    HashSet<string> attrNames = new HashSet<string>();

                    //make sure no duplicates are in attribute name list
                    foreach (var attribute in attributes)
                    {
                        attrNames.Add(attribute.Name);
                    }

                    SchemaBuilder sb = new SchemaBuilder(Guid.NewGuid());
                    sb.SetSchemaName("CityGMLImportSchema");
                    sb.SetReadAccessLevel(AccessLevel.Public);
                    sb.SetWriteAccessLevel(AccessLevel.Public);

                    foreach (var attribute in attrNames)
                    {
                        FieldBuilder fb = sb.AddSimpleField(attribute, typeof(string));
                    }

                    //finish schema creation and commit transaction
                    sb.Finish();
                    trans.Commit();

                }
            }
            return false;  
        }
    }

    #endregion Attributes and Colors to Revit

    public class CityGMLImportSettings
    {
        public CitySource ImportSource;
        public CityGeometry ImportGeomType;
        public CoordOrder CoordOrder;
        public CityGml_Codelist.Codelist CodeTranslate;
        public XDocument XDoc;
        public double[] CenterCoords;
        public double Extent;
        public bool saveResponse;
        public string FilePath;
        public string serverURL;
        public string FolderPath;


        public CityGMLImportSettings()
        {

        }
    }

    public enum CitySource { File, Server}
    public enum CityGeometry { Solid, Faces}
    public enum CoordOrder { ENH, NEH}
}