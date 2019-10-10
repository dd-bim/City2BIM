using Autodesk.Revit.DB;
using City2BIM.GetGeometry;
using City2BIM.GetSemantics;
using City2BIM.GmlRep;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace City2BIM.RevitBuilder
{
    internal class RevitGeometryBuilder
    {
        //private DxfVisualizer dxf;
        private City2BIM.GetGeometry.C2BPoint gmlCorner;
        private Transform revitPBPTrafo;
        private bool swapNE;
        private XYZ vectorPBP;
        private double scale;
        private Document doc;

        //private Dictionary<GetGeometry.C2BSolid, Dictionary<GetSemantics.GmlAttribute, string>> buildings;
        private List<GmlRep.GmlBldg> buildings;

        private Dictionary<GmlRep.GmlSurface.FaceType, ElementId> colors;
        //private Dictionary<GetSemantics.Attribute, string> attributes;

        public RevitGeometryBuilder(Document doc, List<GmlRep.GmlBldg> buildings, GetGeometry.C2BPoint gmlCorner, bool swapNE/*, DxfVisualizer dxf*/)
        {
            this.doc = doc;
            this.buildings = buildings;
            this.swapNE = swapNE;
            this.gmlCorner = gmlCorner;
            this.revitPBPTrafo = GetRevitProjectLocation(doc);
            //this.dxf = dxf;
            this.colors = CreateColorAsMaterial();

        }

        public PlugIn PlugIn
        {
            get => default(PlugIn);
            set
            {
            }
        }

        #region coordinate transformation

        /// <summary>
        /// Reads the Revit project location (PBP)
        /// </summary>
        /// <param name="doc">Revit document</param>
        /// <returns>Revit transformation matrix</returns>
        private Transform GetRevitProjectLocation(Document doc)
        {
            ProjectLocation proj = doc.ActiveProjectLocation;
            ProjectPosition projPos = proj.GetProjectPosition(Autodesk.Revit.DB.XYZ.Zero);

            double angle = projPos.Angle;
            double elevation = projPos.Elevation;
            double easting = projPos.EastWest;
            double northing = projPos.NorthSouth;

            Transform trot = Transform.CreateRotation(Autodesk.Revit.DB.XYZ.BasisZ, -angle);
            this.vectorPBP = new Autodesk.Revit.DB.XYZ(easting, northing, elevation);
            Transform ttrans = Transform.CreateTranslation(-vectorPBP);
            Transform transf = trot.Multiply(ttrans);

            //scale because of projected input data?
            var projInfo = doc.ProjectInformation.LookupParameter("Scale");
            if (projInfo == null || !projInfo.HasValue)
                this.scale = 1;
            else
                this.scale = projInfo.AsDouble();

            if (double.IsNaN(scale))
                this.scale = 1;

            return transf;
        }

        /// <summary>
        /// Transforms gml point coordinates to Revit coordinates
        /// </summary>
        /// <param name="gmlLocalPt">local gml point (reduced with lower corner or so)</param>
        /// <returns>Revit XYZ coordinate</returns>
        private XYZ TransformPointForRevit(C2BPoint gmlLocalPt)
        {
            //At first add lowerCorner from gml
            double xGlobalProj = gmlLocalPt.X + gmlCorner.X;
            double yGlobalProj = gmlLocalPt.Y + gmlCorner.Y;

            if (swapNE)
            {
                xGlobalProj = gmlLocalPt.Y + gmlCorner.Y;
                yGlobalProj = gmlLocalPt.X + gmlCorner.X;
            }

            var zGlobal = gmlLocalPt.Z + gmlCorner.Z;

            var deltaX = xGlobalProj - (vectorPBP.X * 0.3048);
            var deltaY = yGlobalProj - (vectorPBP.Y * 0.3048);

            var deltaXUnproj = deltaX / scale;
            var deltaYUnproj = deltaY / scale;

            var xGlobalUnproj = xGlobalProj - deltaX + deltaXUnproj;
            var yGlobalUnproj = yGlobalProj - deltaY + deltaYUnproj;

            //Multiplication with feet factor (neccessary because of feet in Revit database)
            var xFeet = xGlobalUnproj / 0.3048;
            var yFeet = yGlobalUnproj / 0.3048;
            var zFeet = zGlobal / 0.3048;

            //Creation of Revit point
            var revitXYZ = new XYZ(xFeet, yFeet, zFeet);

            //Transform global coordinate to Revit project coordinate system (system of project base point)
            var revTransXYZ = revitPBPTrafo.OfPoint(revitXYZ);

            return revTransXYZ;
        }

        #endregion coordinate transformation

        #region Solid to Revit incl. LOD1-Fallback

        /// <summary>
        /// Transforms c2b solids to Revit solid (closed volume)
        /// </summary>
        public void CreateBuildings()
        {
            double all = buildings.Count;
            double success = 0.0;
            double error = 0.0;

            var i = 0;

            foreach (var building in buildings)
            {
                try
                {
                    TessellatedShapeBuilder builder = new TessellatedShapeBuilder();
                    builder.OpenConnectedFaceSet(true);

                    var tesselatedFaces = CreateRevitFaceList(building.BldgSolid, building.BldgSurfaces);

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

                        ds = SetAttributeValues(ds, building.BldgAttributes);
                        ds.Pinned = true;

                        var commAttrLabel = LabelUtils.GetLabelFor(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);

                        var commAttr = ds.LookupParameter(commAttrLabel);

                        commAttr.Set("LOD2");

                        t.Commit();
                    }

                    i++;

                    //Log.Information("Building builder successful");
                    success += 1;

                    Log.Information("Calculation completed at " + building.BldgId);
                }
                catch (System.Exception ex)
                {
                    Log.Warning("No solid calculation possbile at " + building.BldgId + " !: " + ex.Message);


                    try
                    {
                        CreateLOD1Building(building.BldgSolid, building.BldgSurfaces, building.BldgAttributes);
                        Log.Information("Fallback completed at " + building.BldgId);
                    }
                    catch (Exception exx)
                    {
                        Log.Error("No bldg at " + building.BldgId + " !: " + exx.Message);
                        continue;
                    }

                    error += 1;
                    continue;
                }
            }

            foreach (var bldg in buildings)
            {
                foreach (var building in bldg.Parts)
                {
                    try
                    {
                        TessellatedShapeBuilder builder = new TessellatedShapeBuilder();
                        builder.OpenConnectedFaceSet(true);

                        var tesselatedFaces = CreateRevitFaceList(building.PartSolid, building.PartSurfaces);

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

                            ds = SetAttributeValues(ds, bldg.BldgAttributes);
                            ds = SetAttributeValues(ds, building.BldgPartAttributes);
                            ds.Pinned = true;

                            var commAttrLabel = LabelUtils.GetLabelFor(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);

                            var commAttr = ds.LookupParameter(commAttrLabel);

                            commAttr.Set("LOD2 (Part)");

                            t.Commit();
                        }

                        i++;

                        //Log.Information("Building builder successful");
                        success += 1;
                    }
                    catch (System.Exception ex)
                    {
                        try
                        {
                            foreach (var d in bldg.BldgAttributes)
                            {
                                if (!building.BldgPartAttributes.ContainsKey(d.Key))
                                    building.BldgPartAttributes.Add(d.Key, d.Value);
                            }

                            CreateLOD1Building(building.PartSolid, building.PartSurfaces, bldg.BldgAttributes);
                        }
                        catch
                        {
                            continue;
                        }

                        error += 1;
                        continue;
                    }
                }
            }

            var results = new LoggerConfiguration()
               //.MinimumLevel.Debug()
               .WriteTo.File(@"C:\Users\goerne\Desktop\logs_revit_plugin\\Results_04062019_1qmm.txt"/*, rollingInterval: RollingInterval.Day*/)
               .CreateLogger();

            double statSucc = success / all * 100;
            double statErr = error / all * 100;

            results.Information(@"C:\Users\goerne\Desktop\logs_revit_plugin");
            results.Information("Erfolgsquote = " + statSucc + "Prozent = " + success + "Gebäude");
            results.Information("Fehlerquote = " + statErr + "Prozent = " + error + "Gebäude");
            results.Information("------------------------------------------------------------------");

            Log.Information("Erfolgsquote = " + statSucc + "Prozent = " + success + "Gebäude");
            Log.Information("Fehlerquote = " + statErr + "Prozent = " + error + "Gebäude");
        }

        private void CreateLOD1Building(C2BSolid solid, List<GmlSurface> surfaces, Dictionary<GmlAttribute, string> attributes)
        {
            var ordByHeight = from v in solid.Vertices
                              orderby v.Position.Z
                              select v.Position.Z;

            var height = ordByHeight.LastOrDefault() - ordByHeight.FirstOrDefault();

            var groundSurfaces = from p in surfaces
                                 where p.Facetype == GmlSurface.FaceType.ground
                                 select p;

            foreach (var groundSurface in groundSurfaces)
            {
                try
                {
                    var groundPlane = (from p in solid.Planes
                                       where p.Key == groundSurface.SurfaceId
                                       select p.Value).SingleOrDefault();

                    var poly = new List<XYZ>();

                    foreach (int vid in groundPlane.Vertices)
                    {
                        var verts = solid.Vertices;

                        if (verts.Contains(verts[vid]))
                        {
                            var revTransXYZ = TransformPointForRevit(verts[vid].Position);

                            poly.Add(revTransXYZ);
                        }
                    }

                    //List<Curve> edges = new List<Curve>();

                    List<CurveLoop> loopList = new List<CurveLoop>();

                    List<Curve> edges = new List<Curve>();

                    for (var c = 1; c < poly.Count; c++)
                    {
                        Line edge = Line.CreateBound(poly[c - 1], poly[c]);

                        edges.Add(edge);
                    }

                    edges.Add(Line.CreateBound(poly[poly.Count - 1], poly[0]));

                    CurveLoop baseLoop = CurveLoop.Create(edges);
                    loopList.Add(baseLoop);

                    var extrHeight = height * 3.28084;

                    Solid lod1bldg = GeometryCreationUtilities.CreateExtrusionGeometry(loopList, XYZ.BasisZ, extrHeight);

                    using (Transaction t = new Transaction(doc, "Create lod1 extrusion"))
                    {
                        t.Start();
                        // create direct shape and assign the sphere shape
                        DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_Entourage));

                        ds.SetShape(new GeometryObject[] { lod1bldg });

                        ds = SetAttributeValues(ds, attributes);
                        ds.Pinned = true;

                        var commAttrLabel = LabelUtils.GetLabelFor(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);

                        var commAttr = ds.LookupParameter(commAttrLabel);

                        commAttr.Set("LOD1 (simplified from LOD2)");

                        t.Commit();
                    }
                }
                catch
                {
                    continue;
                }
            }
        }

        private List<TessellatedFace> CreateRevitFaceList(C2BSolid solid, List<GmlSurface> surfaces)
        {
            List<TessellatedFace> faceListT = new List<TessellatedFace>();

            ElementId colorMat = ElementId.InvalidElementId;

            //PlanesCopy wirklich reduziert?

            Log.Debug("CT Planes____:" + solid.Planes.Count);
            Log.Debug("CT PlanesCopy:" + solid.PlanesCopy.Count);

            foreach (var plane in solid.PlanesCopy) //Planes
            {
                IList<IList<Autodesk.Revit.DB.XYZ>> faceList = new List<IList<XYZ>>(); //neccessary if also interior face will occure

                if (plane.Key.Contains("void)"))         //interior planes will be handled separately
                    continue;

                //var p = plane.Value;

                var faceExterior = IdentifyFacePoints(plane.Value, solid);

                //Identify GmlSurface with current plane

                //var surface = (from pl in surfaces
                //               where pl.SurfaceId == plane.Key
                //               select pl).SingleOrDefault();

                //colorMat = colors[surface.Facetype];

                //Case: interior plane is applicable
                //---------------------------------------
                //Interior faces needs special consideration because of suffix _void in Id
                //var interiors = from plInt in solid.Planes
                //                where plInt.Key.Contains("_void")
                //                select plInt;

                //if(interiors.Any())
                //{
                //    foreach(var interior in interiors)
                //    {
                //        if(interior.Key.Contains(surface.SurfaceId))
                //        {
                //            var faceInterior = IdentifyFacePoints(interior.Value, solid);
                //            faceList.Add(faceInterior);                                     //if interior face is applicable, added to facelist
                //        }
                //    }
                //}

                faceList.Insert(0, faceExterior);       //"normal" exterior faces on first place (Insert important if interior faces are added before)

                var faceT = new TessellatedFace(faceList, colorMat);

                faceListT.Add(faceT);

                //var faceT = new TessellatedFace(face, colorMat);
            }
            return faceListT;
        }

        public IList<XYZ> IdentifyFacePoints(C2BPlane plane, C2BSolid solid)
        {
            IList<Autodesk.Revit.DB.XYZ> facePoints = new List<XYZ>();

            foreach (int vid in plane.Vertices)
            {
                var verts = solid.Vertices;

                if (verts.Contains(verts[vid]))
                {
                    //Transformation for revit
                    var revTransXYZ = TransformPointForRevit(verts[vid].Position);

                    facePoints.Add(revTransXYZ);
                }
                else
                {
                    Log.Error("id nicht vorhanden");
                }
            }

            return facePoints;
        }

        #endregion Solid to Revit incl. LOD1-Fallback

        #region Surfaces to Revit incl. Fallback

        public void CreateBuildingsWithFaces()
        {
            foreach (var building in buildings)
            {
                CreateSurfaceSolid(building.BldgSolid, building.BldgSurfaces, building.BldgAttributes);

                foreach (var part in building.Parts)
                {
                    foreach (var d in building.BldgAttributes)
                    {
                        if (!part.BldgPartAttributes.ContainsKey(d.Key))
                            part.BldgPartAttributes.Add(d.Key, d.Value);
                    }

                    CreateSurfaceSolid(part.PartSolid, part.PartSurfaces, part.BldgPartAttributes);
                }
            }
        }

        private void CreateSurfaceWithOriginalPoints(GmlSurface surface, Dictionary<GmlAttribute, string> attributes)
        {
            var pts = surface.PlaneExt.PolygonPts;

            C2BPoint normalVc = new C2BPoint(0, 0, 0);
            C2BPoint centroidPl = new C2BPoint(0, 0, 0);

            List<CurveLoop> loopList = new List<CurveLoop>();
            List<Curve> edges = new List<Curve>();

            for (var c = 1; c < pts.Count; c++)
            {
                normalVc += C2BPoint.CrossProduct(pts[c - 1], pts[c]);

                centroidPl += pts[c];
            }

            var centroid = centroidPl / (pts.Count - 1);
            var normalizedVc = C2BPoint.Normalized(normalVc);

            var projectedVerts = new List<XYZ>();

            foreach (var pt in pts)
            {
                var vecPtCent = pt - centroid;
                var d = C2BPoint.ScalarProduct(vecPtCent, normalizedVc);

                var vecLotCent = new C2BPoint(d * normalizedVc.X, d * normalizedVc.Y, d * normalizedVc.Z);
                var vertNew = pt - vecLotCent;
                var vertRevXYZ = TransformPointForRevit(vertNew);

                projectedVerts.Add(vertRevXYZ);
            }

            for (var c = 1; c < projectedVerts.Count; c++)
            {
                Line edge = Line.CreateBound(projectedVerts[c - 1], projectedVerts[c]);

                edges.Add(edge);
            }

            CurveLoop baseLoop = CurveLoop.Create(edges);
            loopList.Add(baseLoop);

            double height = 0.01 * 3.28084;

            XYZ normal = new XYZ(normalizedVc.X, normalizedVc.Y, normalizedVc.Z);

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

                    default:
                        break;
                }

                DirectShape ds = DirectShape.CreateElement(doc, elem);

                ds.SetShape(new GeometryObject[] { bldgFaceSolid });

                var commAttrLabel = LabelUtils.GetLabelFor(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);

                var commAttr = ds.LookupParameter(commAttrLabel);

                commAttr.Set("Face-Solid (LOD2) Fallback");

                foreach (var attr in surface.SurfaceAttributes)
                {
                    attributes.Add(attr.Key, attr.Value);
                }

                ds = SetAttributeValues(ds, attributes);

                ds.Pinned = true;

                t.Commit();
            }
        }

        private void CreateSurfaceSolid(C2BSolid solid, List<GmlSurface> surfaces, Dictionary<GmlAttribute, string> bldgAttributes)
        {
            foreach (var plane in solid.PlanesCopy)
            {
                Log.Debug("plane: " + plane.Key);
                foreach (var v in plane.Value.Vertices)
                {
                    Log.Debug("Plane-verts: " + v);
                }
            }

            int i = 0;

            foreach (var vert in solid.Vertices)
            {
                Log.Debug("Vertex: " + i);
                i++;
                foreach (var v in vert.Planes)
                {
                    Log.Debug("Vertex-Planes: " + v);
                }
            }


            foreach (var plane in solid.PlanesCopy)
            {
                var attributes = new Dictionary<GmlAttribute, string>();

                foreach (var attr in bldgAttributes)
                {
                    attributes.Add(attr.Key, attr.Value);
                }

                //Identify GmlSurface with current plane
                //var surface = (from pl in surfaces
                //               where pl.SurfaceId == plane.Key
                //               select pl).SingleOrDefault();

                try
                {
                    var poly = new List<XYZ>();

                    foreach (int vid in plane.Value.Vertices)
                    {
                        var verts = solid.Vertices;

                        if (verts.Contains(verts[vid]))
                        {
                            var revTransXYZ = TransformPointForRevit(verts[vid].Position);

                            poly.Add(revTransXYZ);
                        }
                    }
                    List<CurveLoop> loopList = new List<CurveLoop>();
                    List<Curve> edges = new List<Curve>();

                    for (var c = 1; c < poly.Count; c++)
                    {
                        Line edge = Line.CreateBound(poly[c - 1], poly[c]);

                        edges.Add(edge);
                    }

                    edges.Add(Line.CreateBound(poly[poly.Count - 1], poly[0]));

                    CurveLoop baseLoop = CurveLoop.Create(edges);
                    loopList.Add(baseLoop);

                    double height = 0.01 * 3.28084;

                    XYZ normal = new XYZ(plane.Value.Normal.X, plane.Value.Normal.Y, plane.Value.Normal.Z);

                    SolidOptions opt = new SolidOptions(/*colors[surface.Facetype]*/ ElementId.InvalidElementId, ElementId.InvalidElementId);

                    Solid bldgFaceSolid = GeometryCreationUtilities.CreateExtrusionGeometry(loopList, normal, height, opt);

                    using (Transaction t = new Transaction(doc, "Create face extrusion"))
                    {
                        t.Start();
                        // create direct shape and assign the sphere shape

                        ElementId elem = new ElementId(BuiltInCategory.OST_GenericModel);

                        //switch (surface.Facetype)
                        //{
                        //    case (GmlSurface.FaceType.roof):
                        //        elem = new ElementId(BuiltInCategory.OST_Roofs);

                        //        break;

                        //    case (GmlSurface.FaceType.wall):
                        //        elem = new ElementId(BuiltInCategory.OST_Walls);
                        //        break;

                        //    case (GmlSurface.FaceType.ground):
                        //        elem = new ElementId(BuiltInCategory.OST_StructuralFoundation);
                        //        break;

                        //    case (GmlSurface.FaceType.closure):
                        //        elem = new ElementId(BuiltInCategory.OST_Walls);
                        //        break;

                        //    default:
                        //        break;
                        //}
                        DirectShape ds = DirectShape.CreateElement(doc, elem);

                        ds.SetShape(new GeometryObject[] { bldgFaceSolid });

                        var commAttrLabel = LabelUtils.GetLabelFor(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);

                        var commAttr = ds.LookupParameter(commAttrLabel);

                        commAttr.Set("Face-Solid (LOD2): " + plane.Key);

                        //foreach (var attr in surface.SurfaceAttributes)
                        //{
                        //    attributes.Add(attr.Key, attr.Value);
                        //}

                        ds = SetAttributeValues(ds, attributes);

                        ds.Pinned = true;

                        t.Commit();
                    }

                    Log.Information("Creation of Face-Solid successful!");
                }

                catch (System.Exception ex)
                {
                    try
                    {
                        //CreateSurfaceWithOriginalPoints(surface, attributes);
                        Log.Warning("Face-Fallback used at " + plane.Key + " , because of: " + ex.Message);
                        Log.Information("Fallback successful!");
                    }
                    catch (Exception exX)
                    {
                        //Log.Error("Face-Fallback not possible: at " + plane.Key + " + exX.Message);
                        //Log.Information("Could not create Geometry!");

                        continue;
                    }
                }
            }
        }

        #endregion Surfaces to Revit incl. Fallback

        #region Attributes and Colors to Revit

        private DirectShape SetAttributeValues(DirectShape ds, Dictionary<GmlAttribute, string> attributes)
        {
            var attr = attributes.Keys;

            foreach (var aName in attr)
            {
                var p = ds.LookupParameter(aName.GmlNamespace + ": " + aName.Name);
                attributes.TryGetValue(aName, out var val);

                try
                {
                    if (val != null)
                    {
                        switch (aName.GmlType)
                        {
                            case (GmlAttribute.AttrType.intAttribute):
                                p.Set(int.Parse(val));
                                break;

                            case (GmlAttribute.AttrType.doubleAttribute):
                                p.Set(double.Parse(val, System.Globalization.CultureInfo.InvariantCulture));
                                break;

                            case (GmlAttribute.AttrType.measureAttribute):
                                var valNew = double.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
                                p.Set(valNew * 3.28084);    //Revit-DB speichert alle Längenmaße in Fuß, hier hart kodierte Umerechnung, Annahme: CityGML speichert Meter
                                break;

                            default:
                                p.Set(val);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Semantik-Fehler bei " + aName.Name + " Error: " + ex.Message);
                    continue;
                }
            }

            return ds;
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

                if (groundCols.Count() == 0)
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
                { GmlRep.GmlSurface.FaceType.closure, closureCol }
            };

            return colorList;
        }
    }

    #endregion Attributes and Colors to Revit
}