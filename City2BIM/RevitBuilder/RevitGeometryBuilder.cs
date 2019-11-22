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
        private City2BIM.GetGeometry.C2BPoint gmlCorner;
        private Transform revitPBPTrafo;
        //private bool swapNE;
        private XYZ vectorPBP;
        private double scale;
        private Document doc;

        private List<GmlRep.GmlBldg> buildings;
        private Dictionary<GmlRep.GmlSurface.FaceType, ElementId> colors;


        public RevitGeometryBuilder(Document doc, List<GmlRep.GmlBldg> buildings, GetGeometry.C2BPoint gmlCorner/*, bool swapNE*/)
        {
            this.doc = doc;
            this.buildings = buildings;
            //this.swapNE = swapNE;
            this.gmlCorner = gmlCorner;
            this.revitPBPTrafo = GetRevitProjectLocation(doc);
            this.colors = CreateColorAsMaterial();
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

            double angle = GeoRefSettings.ProjAngle / Prop.radToDeg; //  projPos.Angle;
            double elevation = GeoRefSettings.ProjElevation / Prop.feetToM; // projPos.Elevation;
            double easting = GeoRefSettings.ProjCoord[1] / Prop.feetToM; // projPos.EastWest;
            double northing = GeoRefSettings.ProjCoord[0] / Prop.feetToM; // projPos.NorthSouth;

            Transform trot = Transform.CreateRotation(Autodesk.Revit.DB.XYZ.BasisZ, -angle);
            this.vectorPBP = new Autodesk.Revit.DB.XYZ(easting, northing, elevation);
            Transform ttrans = Transform.CreateTranslation(-vectorPBP);
            Transform transf = trot.Multiply(ttrans);

            ////scale because of projected input data?
            //var projInfo = doc.ProjectInformation.LookupParameter("Scale");
            //if (projInfo == null || !projInfo.HasValue)
            //    this.scale = 1;
            //else
            //    this.scale = projInfo.AsDouble();

            //if (double.IsNaN(scale))
            //    this.scale = 1;

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

            var deltaX = xGlobalProj - (vectorPBP.X * 0.3048);
            var deltaY = yGlobalProj - (vectorPBP.Y * 0.3048);

            if (City2BIM.RevitCommands.City2BIM.City2BIM_prop.IsGeodeticSystem)
            {
                xGlobalProj = gmlLocalPt.Y + gmlCorner.Y;
                yGlobalProj = gmlLocalPt.X + gmlCorner.X;

                deltaX = xGlobalProj - (vectorPBP.Y * 0.3048);
                deltaY = yGlobalProj - (vectorPBP.X * 0.3048);
            }

            



            var deltaXUnproj = deltaX / GeoRefSettings.ProjScale; // scale;
            var deltaYUnproj = deltaY / GeoRefSettings.ProjScale; // scale;

            var xGlobalUnproj = xGlobalProj - deltaX + deltaXUnproj;
            var yGlobalUnproj = yGlobalProj - deltaY + deltaYUnproj;

            var zGlobal = gmlLocalPt.Z + gmlCorner.Z;

            //Multiplication with feet factor (neccessary because of feet in Revit database)
            var xFeet = xGlobalUnproj / 0.3048;
            var yFeet = yGlobalUnproj / 0.3048;
            var zFeet = zGlobal / 0.3048;

            //Creation of Revit point
            var revitXYZ = new XYZ(yFeet, xFeet, zFeet);

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
            double success = 0.0;
            double error = 0.0;
            double fatalError = 0.0;

            var results = new LoggerConfiguration()
               //.MinimumLevel.Debug()
               .WriteTo.File(@"C:\Users\goerne\Desktop\logs_revit_plugin\\RevitErrors_Solids.txt", rollingInterval: RollingInterval.Hour)
               .CreateLogger();

            double all = 0.0;

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
                            error++;
                            WriteRevitBuildErrorsToLog(results, ex.Message, bldg.BldgId, part.BldgPartId);

                            try
                            {
                                CreateLOD1Building(part.PartSolid.InternalID.ToString(), part.PartSolid, part.PartSurfaces, bldg.BldgAttributes, part.BldgPartAttributes);
                            }
                            catch (Exception exx)
                            {
                                fatalError++;
                                WriteRevitBuildErrorsToLog(results, exx.Message, "fatal" + bldg.BldgId, part.BldgPartId);
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
                        error++;
                        WriteRevitBuildErrorsToLog(results, ex.Message, bldg.BldgId);

                        try
                        {
                            CreateLOD1Building(bldg.BldgSolid.InternalID.ToString(), bldg.BldgSolid, bldg.BldgSurfaces, bldg.BldgAttributes);
                        }
                        catch (Exception exx)
                        {
                            fatalError++;
                            WriteRevitBuildErrorsToLog(results, exx.Message, "fatal" + bldg.BldgId);
                            continue;
                        }
                        continue;
                    }
                }
            }

            //-------internal statistic file------------------------

            WriteStatisticToLog(all, success, error, fatalError);

            //-------internal statistic file------------------------
        }

        private void WriteRevitBuildErrorsToLog(Serilog.Core.Logger results, string ex, string buildingID, string partID = null, string surfaceID = null, string surfaceType = null)
        {
            if (partID != null)
                results.Error("At buildingPart: " + ex);
            else
                results.Error("At building: " + ex);

            results.Error(buildingID);
            if (partID != null)
                results.Error(partID);
            if (surfaceID != null)
                results.Error(surfaceID);
            if (surfaceType != null)
                results.Error(surfaceType);
            results.Error("--------------------------------------------------");
        }

        private void WriteStatisticToLog(double all, double success, double? error, double? fatalError)
        {
            var results = new LoggerConfiguration()
               //.MinimumLevel.Debug()
               .WriteTo.File(@"C:\Users\goerne\Desktop\logs_revit_plugin\\Statistic.txt"/*, rollingInterval: RollingInterval.Day*/)
               .CreateLogger();

            double statSucc = success / all * 100;

            results.Information("Amount of Solids or Surfaces: " + all);
            results.Information("Success rate = " + statSucc + " procent = " + success + " objects");

            if (error.HasValue)
            {
                double statErr = (double)error / all * 100;
                results.Warning("Failure rate (LOD1 Fallback) = " + statErr + " procent = " + error + " objects");
            }

            if (fatalError.HasValue)
            {
                double fatStatErr = (double)fatalError / all * 100;
                results.Error("Fatal error: no geometry at = " + fatStatErr + " procent = " + fatalError + " objects");
            }
            results.Information("------------------------------------------------------------------");
        }

        private void CreateRevitRepresentation(string internalID, C2BSolid solid, List<GmlSurface> surfaces, Dictionary<GmlAttribute, string> bldgAttributes, string lod, Dictionary<GmlAttribute, string> partAttributes = null)
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

        private void CreateLOD1Building(string internalID, C2BSolid solid, List<GmlSurface> surfaces, Dictionary<GmlAttribute, string> attributes, Dictionary<GmlAttribute, string> partAttributes = null)
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
                //try
                //{
                var polyGround = groundSurface.ExteriorPts;

                C2BPoint normalizedVc = new C2BPoint(0, 0, 0);
                var poly = CalcPlanarFace(groundSurface.ExteriorPts, ref normalizedVc);

                List<CurveLoop> loopList = new List<CurveLoop>();
                List<Curve> edges = new List<Curve>();

                for (var c = 1; c < poly.Count; c++)
                {
                    Line edge = Line.CreateBound(poly[c - 1], poly[c]);

                    edges.Add(edge);
                }

                CurveLoop baseLoop = CurveLoop.Create(edges);
                loopList.Add(baseLoop);

                var extrHeight = height * 3.28084;

                XYZ normalRev = new XYZ(normalizedVc.X, normalizedVc.Y, normalizedVc.Z);

                Solid lod1bldg = GeometryCreationUtilities.CreateExtrusionGeometry(loopList, normalRev, extrHeight);

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

                    SetRevitInternalParameters(internalID, "LOD1 (Fallback from LOD2)", ds);

                    t.Commit();
                }
                //}
                //catch (Exception ex)
                //{
                //    fatalError++;
                //    WriteRevitBuildErrorsToLog(results, exx.Message, "fatal" + bldg.BldgId);
                //    continue;
                //}
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
                    var revTransXYZ = TransformPointForRevit(verticesXYZ[vid].Position);

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

        #region Surfaces to Revit
        public void CreateBuildingsWithFaces()
        {
            double success = 0.0;
            double error = 0.0;
            double all = 0.0;

            var results = new LoggerConfiguration()
               //.MinimumLevel.Debug()
               .WriteTo.File(@"C:\Users\goerne\Desktop\logs_revit_plugin\\RevitErrors.txt", rollingInterval: RollingInterval.Hour)
               .CreateLogger();

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
                            WriteRevitBuildErrorsToLog(results, ex.Message, building.BldgId, part.BldgPartId, surface.SurfaceId, surface.Facetype.ToString());
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
                        WriteRevitBuildErrorsToLog(results, ex.Message, building.BldgId, null, surface.SurfaceId, surface.Facetype.ToString());
                        continue;
                    }
                }
            }
            //-------------------

            WriteStatisticToLog(all, success, null, error);

            //-----------------------

        }

        private List<XYZ> CalcPlanarFace(List<C2BPoint> pts, ref C2BPoint normalizedVc)
        {
            C2BPoint centroidPl = new C2BPoint(0, 0, 0);
            C2BPoint normalVc = new C2BPoint(0, 0, 0);

            for (var c = 1; c < pts.Count; c++)
            {
                normalVc += C2BPoint.CrossProduct(pts[c - 1], pts[c]);

                centroidPl += pts[c];
            }

            var centroid = centroidPl / (pts.Count - 1);
            normalizedVc = C2BPoint.Normalized(normalVc);

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

            return projectedVerts;
        }

        private void CreateRevitFaceRepresentation(GmlSurface surface, Dictionary<GmlAttribute, string> bldgAttributes, string lod, Dictionary<GmlAttribute, string> partAttributes = null)
        {
            C2BPoint normalizedVc = new C2BPoint(0, 0, 0);

            var projectedVerts = CalcPlanarFace(surface.ExteriorPts, ref normalizedVc);

            List<CurveLoop> loopList = new List<CurveLoop>();
            List<Curve> edges = new List<Curve>();

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