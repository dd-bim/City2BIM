using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using City2BIM.GetGeometry;
using City2BIM.GetSemantics;
using City2BIM.GmlRep;
using Serilog;

namespace City2BIM.RevitBuilder
{
    internal class RevitGeometryBuilder
    {
        private DxfVisualizer dxf;
        private City2BIM.GetGeometry.C2BPoint gmlCorner;
        private Transform revitPBP;
        private Document doc;

        //private Dictionary<GetGeometry.C2BSolid, Dictionary<GetSemantics.GmlAttribute, string>> buildings;
        private List<GmlRep.GmlBldg> buildings;

        private Dictionary<GmlRep.GmlSurface.FaceType, ElementId> colors;
        //private Dictionary<GetSemantics.Attribute, string> attributes;

        public RevitGeometryBuilder(Document doc, List<GmlRep.GmlBldg> buildings, GetGeometry.C2BPoint gmlCorner, DxfVisualizer dxf)
        {
            this.doc = doc;
            this.buildings = buildings;
            this.gmlCorner = gmlCorner;
            this.revitPBP = GetRevitProjectLocation(doc);
            this.dxf = dxf;
            this.colors = CreateColorAsMaterial();
        }

        public PlugIn PlugIn
        {
            get => default(PlugIn);
            set
            {
            }
        }

        private Transform GetRevitProjectLocation(Document doc)
        {
            ProjectLocation proj = doc.ActiveProjectLocation;
            ProjectPosition projPos = proj.GetProjectPosition(Autodesk.Revit.DB.XYZ.Zero);

            double angle = projPos.Angle;
            double elevation = projPos.Elevation;
            double easting = projPos.EastWest;
            double northing = projPos.NorthSouth;

            Transform trot = Transform.CreateRotation(Autodesk.Revit.DB.XYZ.BasisZ, -angle);
            var vector = new Autodesk.Revit.DB.XYZ(easting, northing, elevation);
            Transform ttrans = Transform.CreateTranslation(-vector);
            Transform transf = trot.Multiply(ttrans);

            return transf;
        }

        private XYZ TransformPointForRevit(C2BPoint gmlLocalPt)
        {
            //At first add lowerCorner from gml
            var xGlobal = gmlLocalPt.X + gmlCorner.X;
            var yGlobal = gmlLocalPt.Y + gmlCorner.Y;
            var zGlobal = gmlLocalPt.Z + gmlCorner.Z;

            //Muiltiplication with feet factor (neccessary because of feet in Revit database)
            var xFeet = xGlobal * 3.28084;
            var yFeet = yGlobal * 3.28084;
            var zFeet = zGlobal * 3.28084;

            //Creation of Revit point
            var revitXYZ = new XYZ(xFeet, yFeet, zFeet);

            //Transform global coordinate to Revit project coordinate system (system of project base point)
            var revTransXYZ = revitPBP.OfPoint(revitXYZ);

            return revTransXYZ;
        }

        public void CreateBuildings()
        {
            double all = buildings.Count;
            double success = 0.0;
            double error = 0.0;

            var i = 0;

            var bldgs = this.buildings;

            var solids = buildings.Select(s => s.BldgSolid);

            foreach(var building in buildings)
            {
                try
                {
                    TessellatedShapeBuilder builder = new TessellatedShapeBuilder();
                    builder.OpenConnectedFaceSet(true);

                    ElementId colorMat = ElementId.InvalidElementId;

                    foreach(var plane in building.BldgSolid.Planes)
                    {
                        IList<IList<Autodesk.Revit.DB.XYZ>> faceList = new List<IList<XYZ>>(); //neccessary if also interior face will occure

                        if(plane.Key.Contains("void)"))         //interior planes will be handled separately
                            continue;

                        var p = plane.Value;

                        IList<Autodesk.Revit.DB.XYZ> face = new List<XYZ>();

                        foreach(int vid in p.Vertices)
                        {
                            var verts = building.BldgSolid.Vertices;

                            if(verts.Contains(verts[vid]))
                            {
                                //Transformation for revit
                                var revTransXYZ = TransformPointForRevit(verts[vid].Position);

                                face.Add(revTransXYZ);
                            }
                            else
                            {
                                Log.Error("id nicht vorhanden");
                            }
                        }

                        //Identify GmlSurface with current plane

                        var surface = (from pl in building.BldgSurfaces
                                       where pl.SurfaceId == plane.Key
                                       select pl).SingleOrDefault();

                        colorMat = colors[surface.Facetype];

                        //löschen!----
                        if(building.BldgId == "DEBY_LOD2_4445177")
                        {
                            var ab = "vbla";
                        }
                        //-----------------

                        //Case: interior plane is applicable
                        //---------------------------------------
                        //Interior faces needs special consideration because of suffix _void in Id
                        var interiors = from plInt in building.BldgSolid.Planes
                                        where plInt.Key.Contains("_void")
                                        select plInt;

                        if(interiors.Any())
                        {
                            var idInt = interiors.First().Key.Split('_')[0];

                            if(surface.SurfaceId == idInt)      //if current exterior face has same id like interior main id part
                            {
                                faceList.Add(face);             //if interior face is applicable, added to facelist
                            }
                        }

                        faceList.Insert(0, face);       //"normal" exterior faces on first place (Insert important if interior faces are added before)

                        var faceT = new TessellatedFace(faceList, colorMat);
                        //var faceT = new TessellatedFace(face, colorMat);

                        builder.AddFace(faceT);
                    }

                    builder.CloseConnectedFaceSet();

                    builder.Target = TessellatedShapeBuilderTarget.Solid;

                    builder.Fallback = TessellatedShapeBuilderFallback.Abort;

                    builder.Build();

                    TessellatedShapeBuilderResult result = builder.GetBuildResult();

                    using(Transaction t = new Transaction(doc, "Create tessellated direct shape"))
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
                }
                catch(System.Exception ex)
                {
                    try
                    {
                        CreateLOD1Building(building);
                    }
                    catch
                    {
                        continue;
                    }

                    error += 1;
                    continue;
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

        public void CreateBuildingsWithFaces()
        {
            foreach(var building in buildings)
            {
                foreach(var plane in building.BldgSolid.Planes)
                {
                    var attributes = new Dictionary<GmlAttribute, string>();

                    foreach(var attr in building.BldgAttributes)
                    {
                        attributes.Add(attr.Key, attr.Value);
                    }

                    //Identify GmlSurface with current plane
                    var surface = (from pl in building.BldgSurfaces
                                   where pl.SurfaceId == plane.Key
                                   select pl).SingleOrDefault();

                    try
                    {
                        var poly = new List<XYZ>();

                        foreach(int vid in plane.Value.Vertices)
                        {
                            var verts = building.BldgSolid.Vertices;

                            if(verts.Contains(verts[vid]))
                            {
                                var revTransXYZ = TransformPointForRevit(verts[vid].Position);

                                poly.Add(revTransXYZ);
                            }
                        }
                        List<CurveLoop> loopList = new List<CurveLoop>();
                        List<Curve> edges = new List<Curve>();

                        for(var c = 1; c < poly.Count; c++)
                        {
                            Line edge = Line.CreateBound(poly[c - 1], poly[c]);

                            edges.Add(edge);
                        }

                        edges.Add(Line.CreateBound(poly[poly.Count - 1], poly[0]));

                        CurveLoop baseLoop = CurveLoop.Create(edges);
                        loopList.Add(baseLoop);

                        double height = 0.01 * 3.28084;

                        XYZ normal = new XYZ(plane.Value.Normal.X, plane.Value.Normal.Y, plane.Value.Normal.Z);

                        SolidOptions opt = new SolidOptions(colors[surface.Facetype], ElementId.InvalidElementId);

                        Solid bldgFaceSolid = GeometryCreationUtilities.CreateExtrusionGeometry(loopList, normal, height, opt);

                        using(Transaction t = new Transaction(doc, "Create face extrusion"))
                        {
                            t.Start();
                            // create direct shape and assign the sphere shape

                            ElementId elem = new ElementId(BuiltInCategory.OST_GenericModel);

                            switch(surface.Facetype)
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
                                    elem = new ElementId(BuiltInCategory.OST_Walls);
                                    break;

                                default:
                                    break;
                            }
                            DirectShape ds = DirectShape.CreateElement(doc, elem);

                            ds.SetShape(new GeometryObject[] { bldgFaceSolid });

                            var commAttrLabel = LabelUtils.GetLabelFor(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);

                            var commAttr = ds.LookupParameter(commAttrLabel);

                            commAttr.Set("Face-Solid (LOD2)");

                            foreach(var attr in surface.SurfaceAttributes)
                            {
                                attributes.Add(attr.Key, attr.Value);
                            }

                            ds = SetAttributeValues(ds, attributes);

                            ds.Pinned = true;

                            t.Commit();
                        }
                    }

                    catch(System.Exception ex)
                    {
                        try
                        {
                            CreateSurfaceWithOriginalPoints(surface, attributes);
                            Log.Warning("Face-Fallback used, because of: " + ex.Message);
                        }
                        catch(Exception exX)
                        {
                            Log.Error("Face-Fallback not possible: " + exX.Message);

                            continue;
                        }
                    }
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

            for(var c = 1; c < pts.Count; c++)
            {
                normalVc += C2BPoint.CrossProduct(pts[c - 1], pts[c]);

                centroidPl += pts[c];
            }

            var centroid = centroidPl / (pts.Count - 1);
            var normalizedVc = C2BPoint.Normalized(normalVc);

            var projectedVerts = new List<XYZ>();

            foreach(var pt in pts)
            {
                var vecPtCent = pt - centroid;
                var d = C2BPoint.ScalarProduct(vecPtCent, normalizedVc);

                var vecLotCent = new C2BPoint(d * normalizedVc.X, d * normalizedVc.Y, d * normalizedVc.Z);
                var vertNew = pt - vecLotCent;
                var vertRevXYZ = TransformPointForRevit(vertNew);

                projectedVerts.Add(vertRevXYZ);
            }

            for(var c = 1; c < projectedVerts.Count; c++)
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

            using(Transaction t = new Transaction(doc, "Create face extrusion"))
            {
                t.Start();
                // create direct shape and assign the sphere shape

                ElementId elem = new ElementId(BuiltInCategory.OST_GenericModel);

                switch(surface.Facetype)
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
                        elem = new ElementId(BuiltInCategory.OST_Walls);
                        break;

                    default:
                        break;
                }

                DirectShape ds = DirectShape.CreateElement(doc, elem);

                ds.SetShape(new GeometryObject[] { bldgFaceSolid });

                var commAttrLabel = LabelUtils.GetLabelFor(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);

                var commAttr = ds.LookupParameter(commAttrLabel);

                commAttr.Set("Face-Solid (LOD2) Fallback");

                foreach(var attr in surface.SurfaceAttributes)
                {
                    attributes.Add(attr.Key, attr.Value);
                }

                ds = SetAttributeValues(ds, attributes);

                ds.Pinned = true;

                t.Commit();
            }
        }

        private DirectShape SetAttributeValues(DirectShape ds, Dictionary<GmlAttribute, string> attributes)
        {
            var attr = attributes.Keys;

            foreach(var aName in attr)
            {
                var p = ds.LookupParameter(aName.GmlNamespace + ": " + aName.Name);
                attributes.TryGetValue(aName, out var val);

                try
                {
                    if(val != null)
                    {
                        switch(aName.GmlType)
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
                catch
                {
                    Log.Error("Semantik-Fehler bei " + aName.Name);
                    continue;
                }
            }

            return ds;
        }

        private void CreateLOD1Building(GmlBldg building)
        {
            var ordByHeight = from v in building.BldgSolid.Vertices
                              orderby v.Position.Z
                              select v.Position.Z;

            var height = ordByHeight.LastOrDefault() - ordByHeight.FirstOrDefault();

            var groundSurface = (from p in building.BldgSurfaces
                                 where p.Facetype.HasFlag(GmlSurface.FaceType.ground)
                                 select p).SingleOrDefault();

            var groundPlane = (from p in building.BldgSolid.Planes
                               where p.Key == groundSurface.SurfaceId
                               select p.Value).SingleOrDefault();

            var poly = new List<XYZ>();

            foreach(int vid in groundPlane.Vertices)
            {
                var verts = building.BldgSolid.Vertices;

                if(verts.Contains(verts[vid]))
                {
                    var revTransXYZ = TransformPointForRevit(verts[vid].Position);

                    poly.Add(revTransXYZ);
                }
            }

            //List<Curve> edges = new List<Curve>();

            List<CurveLoop> loopList = new List<CurveLoop>();

            List<Curve> edges = new List<Curve>();

            for(var c = 1; c < poly.Count; c++)
            {
                Line edge = Line.CreateBound(poly[c - 1], poly[c]);

                edges.Add(edge);
            }

            edges.Add(Line.CreateBound(poly[poly.Count - 1], poly[0]));

            CurveLoop baseLoop = CurveLoop.Create(edges);
            loopList.Add(baseLoop);

            height = height * 3.28084;

            Solid lod1bldg = GeometryCreationUtilities.CreateExtrusionGeometry(loopList, XYZ.BasisZ, height);

            using(Transaction t = new Transaction(doc, "Create lod1 extrusion"))
            {
                t.Start();
                // create direct shape and assign the sphere shape
                DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_Entourage));

                ds.SetShape(new GeometryObject[] { lod1bldg });

                ds = SetAttributeValues(ds, building.BldgAttributes);
                ds.Pinned = true;

                var commAttrLabel = LabelUtils.GetLabelFor(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);

                var commAttr = ds.LookupParameter(commAttrLabel);

                commAttr.Set("LOD1 (simplified from LOD2)");

                t.Commit();
            }
        }

        private Dictionary<GmlRep.GmlSurface.FaceType, ElementId> CreateColorAsMaterial()
        {
            ElementId roofCol = ElementId.InvalidElementId;
            ElementId wallCol = ElementId.InvalidElementId;
            ElementId groundCol = ElementId.InvalidElementId;
            ElementId closureCol = ElementId.InvalidElementId;

            using(Transaction t = new Transaction(doc, "Create material"))
            {
                t.Start();

                var coll = new FilteredElementCollector(doc).OfClass(typeof(Material));
                IEnumerable<Material> materialsEnum = coll.ToElements().Cast<Material>();

                var roofCols
                  = from materialElement in materialsEnum
                    where materialElement.Name == "CityGML_Roof"
                    select materialElement.Id;

                if(roofCols.Count() == 0)
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

                if(wallCols.Count() == 0)
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

                if(groundCols.Count() == 0)
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

                if(groundCols.Count() == 0)
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
}