using System.Collections.Generic;
using Autodesk.Revit.DB;
using City2BIM.GetSemantics;
using Serilog;

namespace City2BIM.RevitBuilder
{
    internal class RevitGeometryBuilder
    {
        private Document doc;
        private Dictionary<GetGeometry.Solid, Dictionary<GetSemantics.Attribute, string>> buildings;
        //private Dictionary<GetSemantics.Attribute, string> attributes;

        public RevitGeometryBuilder(Document doc, Dictionary<GetGeometry.Solid, Dictionary<GetSemantics.Attribute, string>> buildings)
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

            var i = 0;

            foreach(var building in buildings)
            {
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
                            }

                            else
                            {
                                Log.Error("id nicht vorhanden");
                            }
                        }

                        builder.AddFace(new TessellatedFace(face, ElementId.InvalidElementId));
                    }

                    builder.CloseConnectedFaceSet();
                    builder.Target = TessellatedShapeBuilderTarget.Solid;
                    builder.Fallback = TessellatedShapeBuilderFallback.Abort;
                    builder.Build();

                    TessellatedShapeBuilderResult result = builder.GetBuildResult();

                    var attributes = building.Value;

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

                    //Log.Error("Revit Builder error occured: " + ex.Message);

                    //foreach(var v in blSeman)
                    //{
                    //    Log.Debug(v);
                    //}

                    //Log.Error(ex.Message);

                    error += 1;
                    continue;
                }
            }
            double statSucc = success / all * 100;
            double statErr = error / all * 100;

            Log.Information("Erfolgsquote = " + statSucc + "Prozent = " + success + "Gebäude");
            Log.Information("Fehlerquote = " + statErr + "Prozent = " + error + "Gebäude");
        }
    }
}