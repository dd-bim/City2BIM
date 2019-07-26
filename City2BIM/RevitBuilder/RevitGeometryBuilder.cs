using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using City2BIM.GetSemantics;
using Serilog;

namespace City2BIM.RevitBuilder
{
    internal class RevitGeometryBuilder
    {
        private DxfVisualizer dxf;

        private Document doc;
        private Dictionary<GetGeometry.Solid, Dictionary<GetSemantics.Attribute, string>> buildings;
        //private Dictionary<GetSemantics.Attribute, string> attributes;

        public RevitGeometryBuilder(Document doc, Dictionary<GetGeometry.Solid, Dictionary<GetSemantics.Attribute, string>> buildings, DxfVisualizer dxf)
        {
            this.doc = doc;
            this.buildings = buildings;
            this.dxf = dxf;
        }

        public PlugIn PlugIn
        {
            get => default(PlugIn);
            set
            {
            }
        }

        public void CreateBuildings(string path, Transform rvtTransf)
        {
            double all = buildings.Count;
            double success = 0.0;
            double error = 0.0;

            var i = 0;

            ElementId roofCol = null;
            ElementId wallCol = null;
            ElementId groundCol = null;
            ElementId closureCol = null;

            CreateColorAsMaterial(ref roofCol, ref wallCol, ref groundCol, ref closureCol);

            foreach(var building in buildings)
            {
                var attributes = building.Value;

                try
                {
                    TessellatedShapeBuilder builder = new TessellatedShapeBuilder();
                    builder.OpenConnectedFaceSet(true);

                    foreach(GetGeometry.Plane p in building.Key.Planes.Values)
                    {
                        List<Autodesk.Revit.DB.XYZ> face = new List<XYZ>();
                        foreach(int vid in p.Vertices)
                        {
                            //GetGeometry.XYZ xyz = building.Key.Vertices[vid].Position;

                            var verts = building.Key.Vertices;

                            if(verts.Contains(verts[vid]))
                            {
                                GetGeometry.XYZ xyz = verts[vid].Position;

                                var xF = xyz.X * 3.28084;
                                var yF = xyz.Y * 3.28084;
                                var zF = xyz.Z * 3.28084;

                                var revitXYZ = new XYZ(xF, yF, zF);

                                var revTransXYZ = rvtTransf.OfPoint(revitXYZ);

                                face.Add(revTransXYZ); //Revit feet Problem

                                dxf.DrawPoint(xF / 3.28084, yF / 3.28084, zF / 3.28084, "revitFaceVertex", new int[] { 0, 0, 255 });
                            }
                            else
                            {
                                Log.Error("id nicht vorhanden");
                            }
                        }

                        for(int m = 0; m < face.Count - 1; m++)
                        {
                            dxf.DrawLine(face[m].X / 3.28084, face[m].Y / 3.28084, face[m].Z / 3.28084, face[m + 1].X / 3.28084, face[m + 1].Y / 3.28084, face[m + 1].Z / 3.28084, "revitFaceLines", new int[] { 0, 0, 255 });
                        }

                        int ind = p.ID.LastIndexOf("_");
                        var e = p.ID.Substring(ind + 1);

                        TessellatedFace faceT;

                        switch(e)
                        {
                            case ("RoofSurface"):
                                faceT = new TessellatedFace(face, roofCol);
                                break;

                            case ("WallSurface"):
                                faceT = new TessellatedFace(face, wallCol);
                                break;

                            case ("GroundSurface"):
                                faceT = new TessellatedFace(face, groundCol);
                                break;

                            case ("ClosureSurface"):
                                faceT = new TessellatedFace(face, closureCol);
                                break;

                            default:
                                faceT = new TessellatedFace(face, ElementId.InvalidElementId);
                                break;
                        }

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

                        ds = SetAttributeValues(ds, attributes);

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
                        var maxHeight = (from v in building.Key.Vertices
                                         orderby v.Position.Z descending
                                         select v.Position.Z).FirstOrDefault();

                        var groundPlane = (from p in building.Key.Planes
                                           where p.Key.Contains("Ground")
                                           select p.Value).SingleOrDefault();

                        var poly = new List<XYZ>();

                        foreach(int vid in groundPlane.Vertices)
                        {
                            var verts = building.Key.Vertices;

                            if(verts.Contains(verts[vid]))
                            {
                                GetGeometry.XYZ xyz = verts[vid].Position;

                                var xF = xyz.X * 3.28084;
                                var yF = xyz.Y * 3.28084;
                                var zF = xyz.Z * 3.28084;

                                var revitXYZ = new XYZ(xF, yF, zF);

                                var revTransXYZ = rvtTransf.OfPoint(revitXYZ);

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

                        double height = maxHeight * 3.28084 - poly[0].Z;

                        Solid lod1bldg = GeometryCreationUtilities.CreateExtrusionGeometry(loopList, XYZ.BasisZ, height);

                        using(Transaction t = new Transaction(doc, "Create lod1 extrusion"))
                        {
                            t.Start();
                            // create direct shape and assign the sphere shape
                            DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_Entourage));

                            ds.SetShape(new GeometryObject[] { lod1bldg });

                            ds = SetAttributeValues(ds, attributes);

                            var commAttrLabel = LabelUtils.GetLabelFor(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);

                            var commAttr = ds.LookupParameter(commAttrLabel);

                            commAttr.Set("LOD1 (simplified from LOD2)");

                            t.Commit();
                        }
                    }
                    catch(System.Exception exc)
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

            results.Information(path);
            results.Information("Erfolgsquote = " + statSucc + "Prozent = " + success + "Gebäude");
            results.Information("Fehlerquote = " + statErr + "Prozent = " + error + "Gebäude");
            results.Information("------------------------------------------------------------------");

            Log.Information("Erfolgsquote = " + statSucc + "Prozent = " + success + "Gebäude");
            Log.Information("Fehlerquote = " + statErr + "Prozent = " + error + "Gebäude");

            try
            {
                CreateBuildingsWithFaces(rvtTransf);
            }
            catch { }
        }

        public void CreateBuildingsWithFaces(Transform rvtTransf)
        {
            foreach(var building in buildings)
            {
                //var attributes = building.Value;

                foreach(var plane in building.Key.Planes)
                {
                    try
                    {
                        var poly = new List<XYZ>();

                        foreach(int vid in plane.Value.Vertices)
                        {
                            var verts = building.Key.Vertices;

                            if(verts.Contains(verts[vid]))
                            {
                                GetGeometry.XYZ xyz = verts[vid].Position;

                                var xF = xyz.X * 3.28084;
                                var yF = xyz.Y * 3.28084;
                                var zF = xyz.Z * 3.28084;

                                var revitXYZ = new XYZ(xF, yF, zF);

                                var revTransXYZ = rvtTransf.OfPoint(revitXYZ);

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

                        Solid bldgFaceSolid = GeometryCreationUtilities.CreateExtrusionGeometry(loopList, normal, height);

                        using(Transaction t = new Transaction(doc, "Create face extrusion"))
                        {
                            t.Start();
                            // create direct shape and assign the sphere shape

                            int ind = plane.Key.LastIndexOf("_");
                            var e = plane.Key.Substring(ind + 1);

                            Color color = new Color(0, 0, 0);
                            ElementId elem = new ElementId(BuiltInCategory.OST_GenericModel);

                            switch(e)
                            {
                                case ("RoofSurface"):
                                    color = new Color(255, 0, 0);
                                    elem = new ElementId(BuiltInCategory.OST_Roofs);
                                    break;

                                case ("WallSurface"):
                                    color = new Color(80, 80, 80);
                                    elem = new ElementId(BuiltInCategory.OST_Walls);
                                    break;

                                case ("GroundSurface"):
                                    color = new Color(0, 0, 0);
                                    elem = new ElementId(BuiltInCategory.OST_Floors);
                                    break;

                                case ("ClosureSurface"):
                                    color = new Color(245, 245, 245);
                                    elem = new ElementId(BuiltInCategory.OST_Walls);
                                    break;

                                default:
                                    color = new Color(120, 120, 120);
                                    break;
                            }

                            //OverrideGraphicSettings ogs = new OverrideGraphicSettings();
                            //ogs.SetProjectionLineColor(color);
                            //ogs.SetProjectionFillColor(color);
                            //ogs.SetCutFillColor(color);
                            //ogs.SetCutLineColor(color);

                            DirectShape ds = DirectShape.CreateElement(doc, elem);

                            //doc.ActiveView.SetElementOverrides(ds.Id, ogs);

                            ds.SetShape(new GeometryObject[] { bldgFaceSolid });

                            var commAttrLabel = LabelUtils.GetLabelFor(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);

                            var commAttr = ds.LookupParameter(commAttrLabel);

                            commAttr.Set("Face-Solid (LOD2)");

                            SetAttributeValues(ds, building.Value);

                            t.Commit();
                        }
                    }
                    catch
                    {
                        Log.Information("Error at " + plane.Key);
                        continue;
                    }
                }
            }
        }

        private DirectShape SetAttributeValues(DirectShape ds, Dictionary<Attribute, string> attributes)
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
                            case (Attribute.AttrType.intAttribute):
                                p.Set(int.Parse(val));
                                break;

                            case (Attribute.AttrType.doubleAttribute):
                                p.Set(double.Parse(val, System.Globalization.CultureInfo.InvariantCulture));
                                break;

                            case (Attribute.AttrType.measureAttribute):
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

        private void CreateColorAsMaterial(ref ElementId roofCol, ref ElementId wallCol, ref ElementId groundCol, ref ElementId closureCol)
        {
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
        }
    }
}