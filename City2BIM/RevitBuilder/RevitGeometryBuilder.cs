using System.Collections.Generic;
using Autodesk.Revit.DB;
using Serilog;

namespace City2BIM.RevitBuilder
{
    internal class RevitGeometryBuilder
    {
        private Document doc;
        private List<GetGeometry.Solid> buildings;

        public RevitGeometryBuilder(Document doc, List<GetGeometry.Solid> buildings)
        {
            this.doc = doc;
            this.buildings = buildings;
        }

        public PlugIn PlugIn
        {
            get => default(PlugIn);
            set
            {
            }
        }

        public void CreateBuildings()
        {
            double all = buildings.Count;
            double success = 0.0;
            double error = 0.0;

            foreach(GetGeometry.Solid building in buildings)
            {
                try
                {
                    TessellatedShapeBuilder builder = new TessellatedShapeBuilder();
                    builder.OpenConnectedFaceSet(true);

                    foreach(GetGeometry.Plane p in building.Planes.Values)
                    {
                        List<Autodesk.Revit.DB.XYZ> face = new List<XYZ>();
                        foreach(int vid in p.Vertices)
                        {
                            GetGeometry.XYZ xyz = building.Vertices[vid].Position;
                            face.Add(new XYZ(xyz.X, xyz.Y, xyz.Z));
                        }

                        builder.AddFace(new TessellatedFace(face, ElementId.InvalidElementId));
                    }

                    builder.CloseConnectedFaceSet();
                    builder.Target = TessellatedShapeBuilderTarget.Solid;
                    builder.Fallback = TessellatedShapeBuilderFallback.Abort;
                    builder.Build();

                    TessellatedShapeBuilderResult result = builder.GetBuildResult();

                    using(Transaction t = new Transaction(doc, "Create tessellated direct shape"))
                    {
                        t.Start();

                        DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                        ds.ApplicationId = "Application id";
                        ds.ApplicationDataId = "Geometry object id";

                        ds.SetShape(result.GetGeometricalObjects());
                        

                        t.Commit();
                    }

                    Log.Information("Building builder successful");
                    success += 1;
                }
                catch
                {
                    Log.Error("Revit Builder error occured.");
                    error += 1;
                    continue;
                }
            }

            double statSucc = success/all *100;
            double statErr = error / all * 100;

            Log.Information("Erfolgsquote = " + statSucc + "Prozent");
            Log.Information("Fehlerquote = " + statErr + "Prozent");
        }
    }
}