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

        public void CreateBuildings(string path)
        {
            double all = buildings.Count;
            double success = 0.0;
            double error = 0.0;

            var i = 0;

            ElementId roofCol, wallCol, groundCol, closureCol;

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

                                //var xy = verts. from v in verts
                                //         where v

                                //[vid].Position;

                                //face.Add(new XYZ(xyz.X, xyz.Y, xyz.Z));

                                //dirty hack: revit api feet umrechnung, zu erweitern: einheit im projekt abfragen

                                var xF = xyz.X * 3.28084;
                                var yF = xyz.Y * 3.28084;
                                var zF = xyz.Z * 3.28084;

                                face.Add(new XYZ(xF, yF, zF)); //Revit feet Problem

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

                    // builder.Target = TessellatedShapeBuilderTarget.

                    builder.Target = TessellatedShapeBuilderTarget.Solid;

                    builder.Fallback = TessellatedShapeBuilderFallback.Abort;
                    builder.Build();

                    TessellatedShapeBuilderResult result = builder.GetBuildResult();

                    //Test für appendshape

                    using(Transaction t = new Transaction(doc, "Create tessellated direct shape"))
                    {
                        t.Start();

                        DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_Entourage));

                        ds.ApplicationId = "Application id";
                        ds.ApplicationDataId = "Geometry object id";

                        ds.SetShape(result.GetGeometricalObjects());

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

                        t.Commit();
                    }

                    i++;

                    //Log.Information("Building builder successful");
                    success += 1;
                }
                catch(System.Exception ex)
                {
                    //var blSeman = building.Value.Values;

                    //Log.Error("Revit Builder error occured: " + ex.Message + ", " + ex.StackTrace);

                    //foreach(var v in blSeman)
                    //{
                    //    Log.Debug(v);
                    //}

                    //Log.Error(ex.Message);

                    var id = (from a in attributes
                              where a.Key.Name.Equals("Building_ID")
                              select a.Value).FirstOrDefault();

                    Log.Error("Error at " + id);

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
        }
    }
}