using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using City2BIM.GetGeometry;
using City2BIM.GetSemantics;
using City2BIM.GmlRep;
using City2BIM.RevitBuilder;
using Serilog;
using C2BPoint = City2BIM.GetGeometry.C2BPoint;
using C2BSolid = City2BIM.GetGeometry.C2BSolid;
using GmlAttribute = City2BIM.GetSemantics.GmlAttribute;

namespace City2BIM
{
    public class ReadCityGML
    {
        private C2BPoint lowerCornerPt;
        private Dictionary<string, XNamespace> allns;
        private HashSet<GmlAttribute> attributes = new HashSet<GmlAttribute>();

        // The main Execute method (inherited from IExternalCommand) must be public
        public ReadCityGML(ExternalCommandData revit, bool solid)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(@"C:\Users\goerne\Desktop\logs_revit_plugin\\log_plugin" + DateTime.UtcNow.ToFileTimeUtc() + ".txt"/*, rollingInterval: RollingInterval.Day*/)
                .CreateLogger();

            var dxf = new DxfVisualizer();

            UIApplication uiApp = revit.Application;
            Document doc = uiApp.ActiveUIDocument.Document;

            Log.Information("Start...");

            //Import via Dialog:
            FileDialog imp = new FileDialog();
            var path = imp.ImportPathCityGML();
            //-------------------------------
            Log.Information("File: " + path);

            //Hauptmethode zur Erstellung der Geometrie, Ermitteln der Attribute und Füllen der Attributwerte
            //var solidList = ReadXMLDoc(path, dxf); //ref für attributes?

            Log.Information("Start reading CityGML data...");
            //Daten einlesen mit Semantik sowie Geometrie (Rohdaten, Punktlisten)
            var gmlBuildings = ReadGmlData(path, dxf);

            Log.Information("Validate CityGML geometry data...");
            //Filter of surface points (Geometry validation)
            gmlBuildings = ImprovePolygonPoints(gmlBuildings);

            Log.Information("Calculate solids from CityGML geometry data...");
            //Creation of Solids
            gmlBuildings = CalculateSolids(gmlBuildings);

            //erstellt Revit-seitig die Geometrie und ordnet Attributwerte zu (Achtung: ReadXMLDoc muss vorher ausgeführt werden)
            RevitGeometryBuilder cityModel = new RevitGeometryBuilder(doc, gmlBuildings, this.lowerCornerPt, dxf);

            //Parameter für Revit-Kategorie erstellen
            //nach ausgewählter Methode (Solids oder Flächen) Parameter an zugehörige Kategorien übergeben

            // TO DO: Logik für Parameter File -Auswahl oder Location

            //erstellt Revit-seitig die Attribute (Achtung: ReadXMLDoc muss vorher ausgeführt werden)
            RevitSemanticBuilder citySem = new RevitSemanticBuilder(doc); //Übergabe der Methoden-Rückgaben zum Schreiben nach Revit

            citySem.CreateParameters(attributes); //erstellt Shared Parameters für Kategorie Umgebung

            if(solid)
            {
                //var entourageAttr = from a in attributes
                //                    where a.Reference == GmlAttribute.AttrHierarchy.bldg
                //                    select a;

                //Log.Information("Create Revit Parameters for Category Entorurage...");
                //citySem.CreateParameters(/*BuiltInCategory.OST_Entourage, */entourageAttr); //erstellt Shared Parameters für Kategorie Umgebung

                //citySem.AttachParametersToCategory(attributes);

                Log.Information("Calculate Revit Geometry for Building Solids...");
                cityModel.CreateBuildings(); //erstellt DirectShape-Geometrie als Kategorie Umgebung
            }
            else
            {
                //var wallClosureAttr = from a in attributes
                //                      where a.Reference == GmlAttribute.AttrHierarchy.bldg ||
                //                      a.Reference == GmlAttribute.AttrHierarchy.wall ||
                //                      a.Reference == GmlAttribute.AttrHierarchy.closure
                //                      select a;

                //Log.Information("Create Revit Parameters for Category Walls...");
                //citySem.CreateParameters(/*BuiltInCategory.OST_Walls, */wallClosureAttr);

                //var roofAttr = from a in attributes
                //               where a.Reference == GmlAttribute.AttrHierarchy.bldg ||
                //               a.Reference == GmlAttribute.AttrHierarchy.roof
                //               select a;

                //Log.Information("Create Revit Parameters for Category Roof...");
                //citySem.CreateParameters(/*BuiltInCategory.OST_Roofs, */roofAttr);

                //var groundAttr = from a in attributes
                //                 where a.Reference == GmlAttribute.AttrHierarchy.bldg ||
                //                 a.Reference == GmlAttribute.AttrHierarchy.ground
                //                 select a;

                //Log.Information("Create Revit Parameters for Category Ground...");
                //citySem.CreateParameters(/*BuiltInCategory.OST_StructuralFoundation, */groundAttr);

                //citySem.AttachParametersToCategory(attributes);

                Log.Information("Calculate Revit Geometry for Building Faces...");
                cityModel.CreateBuildingsWithFaces(); //erstellt DirectShape-Geometrien der jeweiligen Kategorie
            }

            

            string res = "";

            if(cityModel == null)
                res = "empty";
            else
                res = "not empty";

            Log.Debug("CityModel: " + res);

            //debug

            dxf.DrawDxf(path);
        }

        /// <summary>
        /// Splitting of PosList for XYZ coordinates
        /// Subtraction of lowerCorner for mathematical calculations
        /// </summary>
        /// <param name="points">posList polygon</param>
        /// <param name="lowerCorner">Coordinate XYZ lower Corner</param>
        /// <returns>List of Polygon Points (XYZ)</returns>
        private List<C2BPoint> CollectPoints(XElement points, C2BPoint lowerCorner)
        {
            string pointString = points.Value;
            var pointsSeperated = pointString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var polygonVertices = new List<C2BPoint>();

            for(int i = 0; i < pointsSeperated.Length; i = i + 3)
            {
                var coord = SplitCoordinate(new string[] { pointsSeperated[i], pointsSeperated[i + 1], pointsSeperated[i + 2] }, lowerCorner);

                polygonVertices.Add(coord);
            }

            return polygonVertices;
        }

        /// <summary>
        /// Splitting of Pos for single XYZ coordinate
        /// Subtraction of lowerCorner for mathematical calculations
        /// </summary>
        /// <param name="position">pos tag</param>
        /// <param name="lowerCorner">Coordinate XYZ lower Corner</param>
        /// <returns>Polygon Point (XYZ)</returns>
        private C2BPoint CollectPoint(XElement position, C2BPoint lowerCorner)
        {
            var pointSeperated = position.Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            return SplitCoordinate(pointSeperated, lowerCorner);
        }

        private C2BPoint SplitCoordinate(string[] xyzString, C2BPoint lowerCorner)
        {
            double x = Double.Parse(xyzString[0], CultureInfo.InvariantCulture) - lowerCorner.X;
            double y = Double.Parse(xyzString[1], CultureInfo.InvariantCulture) - lowerCorner.Y;
            double z = Double.Parse(xyzString[2], CultureInfo.InvariantCulture) - lowerCorner.Z;

            return new C2BPoint(x, y, z);
        }

        private List<GmlSurface> ReadSurfaces(XElement bldgEl, HashSet<GmlAttribute> attributes)
        {
            var bldgParts = bldgEl.Elements(this.allns["bldg"] + "consistsOfBuildingPart");
            var bldg = bldgEl.Elements().Except(bldgParts);

            var surfaces = new List<GmlSurface>();

            #region WallSurfaces

            var lod2Walls = bldg.Descendants(this.allns["bldg"] + "WallSurface");

            foreach(var wall in lod2Walls)
            {
                var polysW = wall.Descendants(this.allns["gml"] + "Polygon").ToList();

                for(var i = 0; i < polysW.Count(); i++)          //normally 1 polygon but sometimes surfaces are grouped under the surface type
                {
                    var surface = new GmlSurface();

                    var faceID = IdentifySurfaceID(wall);

                    if(faceID == "")
                    {
                        faceID = bldgEl.Attribute(allns["gml"] + "id").Value;
                    }

                    if(polysW.Count() > 1)
                        surface.SurfaceId = faceID + "_" + i;
                    else
                        surface.SurfaceId = faceID;

                    surface.Facetype = GmlSurface.FaceType.wall;
                    surface.SurfaceAttributes = new ReadSemValues().ReadAttributeValuesSurface(wall, attributes, GmlSurface.FaceType.wall);

                    var surfacePl = ReadSurfaceData(polysW[i], surface);

                    surfaces.Add(surfacePl);
                }
            }

            #endregion WallSurfaces

            #region RoofSurfaces

            var lod2Roofs = bldg.Descendants(this.allns["bldg"] + "RoofSurface");

            foreach(var roof in lod2Roofs)
            {
                var polysR = roof.Descendants(this.allns["gml"] + "Polygon").ToList();

                for(var i = 0; i < polysR.Count(); i++)          //normally 1 polygon but sometimes surfaces are grouped under the surface type
                {
                    var surface = new GmlSurface();

                    var faceID = IdentifySurfaceID(roof);

                    if(faceID == "")
                    {
                        faceID = bldgEl.Attribute(allns["gml"] + "id").Value;
                    }

                    if(polysR.Count() > 1)
                        surface.SurfaceId = faceID + "_" + i;
                    else
                        surface.SurfaceId = faceID;

                    surface.Facetype = GmlSurface.FaceType.roof;
                    surface.SurfaceAttributes = new ReadSemValues().ReadAttributeValuesSurface(roof, attributes, GmlSurface.FaceType.roof);

                    var surfacePl = ReadSurfaceData(polysR[i], surface);

                    surfaces.Add(surfacePl);
                }
            }

            #endregion RoofSurfaces

            #region GroundSurfaces

            var lod2Grounds = bldg.Descendants(this.allns["bldg"] + "GroundSurface");

            foreach(var ground in lod2Grounds)
            {
                var polysG = ground.Descendants(this.allns["gml"] + "Polygon").ToList();

                for(var i = 0; i < polysG.Count(); i++)          //normally 1 polygon but sometimes surfaces are grouped under the surface type
                {
                    var surface = new GmlSurface();

                    var faceID = IdentifySurfaceID(ground);

                    if(faceID == "")
                    {
                        faceID = bldgEl.Attribute(allns["gml"] + "id").Value;
                    }

                    if(polysG.Count() > 1)
                        surface.SurfaceId = faceID + "_" + i;
                    else
                        surface.SurfaceId = faceID;

                    surface.Facetype = GmlSurface.FaceType.ground;
                    surface.SurfaceAttributes = new ReadSemValues().ReadAttributeValuesSurface(ground, attributes, GmlSurface.FaceType.ground);

                    var surfacePl = ReadSurfaceData(polysG[i], surface);

                    surfaces.Add(surfacePl);
                }
            }

            #endregion GroundSurfaces

            #region ClosureSurfaces

            var lod2Closures = bldg.Descendants(this.allns["bldg"] + "ClosureSurface");

            foreach(var closure in lod2Closures)
            {
                var polysC = closure.Descendants(this.allns["gml"] + "Polygon").ToList();

                for(var i = 0; i < polysC.Count(); i++)          //normally 1 polygon but sometimes surfaces are grouped under the surface type
                {
                    var surface = new GmlSurface();

                    var faceID = IdentifySurfaceID(closure);

                    if(faceID == "")
                    {
                        faceID = bldgEl.Attribute(allns["gml"] + "id").Value;
                    }

                    if(polysC.Count() > 1)
                        surface.SurfaceId = faceID + "_" + i;
                    else
                        surface.SurfaceId = faceID;

                    surface.Facetype = GmlSurface.FaceType.closure;
                    surface.SurfaceAttributes = new ReadSemValues().ReadAttributeValuesSurface(closure, attributes, GmlSurface.FaceType.closure);

                    var surfacePl = ReadSurfaceData(polysC[i], surface);

                    surfaces.Add(surfacePl);
                }
            }

            #endregion ClosureSurfaces

            #region lod1Surfaces

            //one occurence per building
            var lod1Rep = bldg.Descendants(this.allns["bldg"] + "lod1Solid").FirstOrDefault();

            if(lod1Rep == null)
                lod1Rep = bldg.Descendants(this.allns["bldg"] + "lod1MultiSurface").FirstOrDefault();

            if(lod1Rep != null)
            {
                var polys = lod1Rep.Descendants(this.allns["gml"] + "Polygon").ToList();
                var elemsWithID = lod1Rep.DescendantsAndSelf().Where(a => a.Attribute(allns["gml"] + "id") != null);

                for(var i = 0; i < polys.Count(); i++)          //normally 1 polygon but sometimes surfaces are grouped under the surface type
                {
                    var surface = new GmlSurface();

                    var faceID = polys[i].Attribute(allns["gml"] + "id").Value;

                    if(faceID == null)
                    {
                        var gmlSolid = lod1Rep.Descendants(this.allns["gml"] + "Solid").FirstOrDefault();

                        faceID = gmlSolid.Attribute(allns["gml"] + "id").Value;

                        if(faceID == null)
                        {
                            faceID = bldgEl.Attribute(allns["gml"] + "id").Value + "_" + i;
                        }
                    }

                    surface.Facetype = GmlSurface.FaceType.unknown;
                    surface.SurfaceAttributes = new ReadSemValues().ReadAttributeValuesSurface(polys[i], attributes, GmlSurface.FaceType.unknown);

                    var surfacePl = ReadSurfaceData(polys[i], surface);

                    surfaces.Add(surfacePl);
                }
            }

            #endregion lod1Surfaces

            Log.Debug("Amount walls: " + lod2Walls.Count());
            Log.Debug("Amount roofs: " + lod2Roofs.Count());
            Log.Debug("Amount closures: " + lod2Closures.Count());
            Log.Debug("Amount grounds: " + lod2Grounds.Count());

            return surfaces;
        }

        private string IdentifySurfaceID(XElement surface)
        {
            var faceID = surface.Attribute(allns["gml"] + "id");

            if(faceID != null)
                return faceID.Value;

            var multiS = surface.Descendants(allns["gml"] + "MultiSurface");

            if(multiS.Count() == 1)
            {
                var multiID = multiS.Single().Attribute(allns["gml"] + "id");

                if(multiID != null)
                    return multiID.Value;
            }

            var poly = surface.Descendants(allns["gml"] + "Polygon");

            if(poly.Count() == 1)
            {
                var polyID = poly.Single().Attribute(allns["gml"] + "id");

                if(polyID != null)
                    return polyID.Value;
            }

            var ring = surface.Descendants(allns["gml"] + "LinearRing");

            if(ring.Count() == 1)
            {
                var ringID = ring.Single().Attribute(allns["gml"] + "id");

                if(ringID != null)
                    return ringID.Value;
            }

            return "";
        }

        /// <summary>
        /// Reads exterior and interior polygon data
        /// </summary>
        /// <param name="poly">GML Polygon element</param>
        /// <returns></returns>
        private GmlSurface ReadSurfaceData(XElement poly, GmlSurface rawFace)
        {
            var surface = new GmlSurface();
            surface.SurfaceId = rawFace.SurfaceId;
            surface.SurfaceAttributes = rawFace.SurfaceAttributes;
            surface.Facetype = rawFace.Facetype;

            #region ExteriorPolygon

            //only one could (should) exist

            var exteriorF = poly.Descendants(this.allns["gml"] + "exterior").FirstOrDefault();

            var posListExt = exteriorF.Descendants(allns["gml"] + "posList");
            Log.Debug("Amount exterior polygons: " + posListExt.Count());

            var planeExt = new C2BPlane(surface.SurfaceId);

            if(posListExt.Any())
            {
                planeExt.PolygonPts = CollectPoints(posListExt.FirstOrDefault(), this.lowerCornerPt);
            }
            else
            {
                var posExt = exteriorF.Descendants(allns["gml"] + "pos");

                var ptList = new List<C2BPoint>();

                foreach(var pos in posExt)
                {
                    ptList.Add(CollectPoint(pos, this.lowerCornerPt));
                }

                planeExt.PolygonPts = ptList;
            }

            surface.PlaneExt = planeExt;

            #endregion ExteriorPolygon

            #region InteriorPolygon

            //if existent, it also could have more than one hole

            var interiorF = poly.Descendants(this.allns["gml"] + "interior");

            var posListInt = interiorF.Descendants(allns["gml"] + "posList").ToList();
            Log.Debug("Amount interior polygons: " + posListInt.Count());

            var intPlanes = new List<C2BPlane>();

            if(posListInt.Any())
            {
                for(var j = 0; j < posListInt.Count(); j++)
                {
                    var planeInt = new C2BPlane(surface.SurfaceId + "_void_" + j);

                    planeInt.PolygonPts = CollectPoints(posListInt[j], this.lowerCornerPt);

                    intPlanes.Add(planeInt);
                }

                surface.PlaneInt = intPlanes;
            }
            else
            {
                var rings = interiorF.Descendants(allns["gml"] + "LinearRing").ToList();

                for(var k = 0; k < rings.Count() - 1; k++)
                {
                    var posInt = rings[k].Descendants(allns["gml"] + "pos");

                    if(posInt.Any())
                    {
                        var planeInt = new C2BPlane(surface.SurfaceId + "_void_" + k);

                        var ptList = new List<C2BPoint>();

                        foreach(var pos in posInt)
                        {
                            ptList.Add(CollectPoint(pos, this.lowerCornerPt));
                        }

                        planeInt.PolygonPts = ptList;
                    }
                }

                surface.PlaneInt = intPlanes;
            }

            #endregion InteriorPolygon

            return surface;
        }

        public List<GmlBldg> ReadGmlData(string path, DxfVisualizer dxf)
        {
            //Load XML document
            XDocument gmlDoc = XDocument.Load(path);
            //-----------------------------------------

            #region Namespaces

            Log.Debug("Read all namespaces in CityGML document...");
            //Save all namespaces in Dictionary with shortenings
            this.allns = gmlDoc.Root.Attributes().
                Where(a => a.IsNamespaceDeclaration).
                GroupBy(a => a.Name.Namespace == XNamespace.None ? String.Empty : a.Name.LocalName, a => XNamespace.Get(a.Value)).
                ToDictionary(g => g.Key, g => g.First());

            //special case:
            if(this.allns.ContainsKey(""))
            {
                if(!this.allns.ContainsKey("core"))
                {
                    this.allns.Add("core", this.allns[""]);     //if namespace has no shortener --> core namespace is used

                    Log.Debug("Replace empty namespace shorterner with core namespace");
                }
            }

            foreach(var ns in allns)
            {
                Log.Debug("Namespace: " + ns.Key);
            }
            //------------------------------------------------------------------------------------------------------------------------

            #endregion Namespaces

            #region LowerCorner

            //TO DO - Fallback, if no LowerCorner specified --> take first point occurence

            //For better calculation, Identify lower Corner
            Log.Debug("Read lowerCorner for mathematical operations (first oocurence in file)...");
            var lowerCorner = gmlDoc.Descendants(this.allns["gml"] + "lowerCorner").FirstOrDefault();
            this.lowerCornerPt = CollectPoint(lowerCorner, new C2BPoint(0, 0, 0));
            Log.Debug("Lower Corner: " + lowerCornerPt.X + " ," + lowerCornerPt.Y + " ," + lowerCornerPt.Z);

            #endregion LowerCorner

            //Read all overall building elements
            Log.Debug("Read all bldg:Building elements...");
            var gmlBuildings = gmlDoc.Descendants(this.allns["bldg"] + "Building");
            Log.Debug("Amount: " + gmlBuildings.Count());
            //--------------------------------------------------------------------------

            #region Semantics

            //Read all semantic attributes first:
            Log.Debug("Read all semantic attributes over all buildings...");
            //Loop over all buildings, parameter list in Revit needs consistent parameters for object types
            var sem = new ReadSemAttributes();

            //first of all regular schema attributes (inherited by parsing of XML schema for core and bldg, standard specific)
            this.attributes = sem.GetSchemaAttributes();

            //secondly add generic attributes (file specific)
            var genAttr = sem.ReadGenericAttributes(gmlBuildings, this.allns);

            //union for consistent attribute list
            this.attributes.UnionWith(genAttr);
            Log.Debug("Amount: " + this.attributes.Count);
            foreach(var attr in attributes)
            {
                Log.Debug("Attribute: " + attr.Name + " for category " + attr.Reference);
            }
            //--------------------------------------------------------------------------------------------------------------------------

            #endregion Semantics

            #region BuildingInstances

            //set up of individual building elements for overall list
            var gmlBldgs = new List<GmlBldg>();

            Log.Debug("Loop over all buildings...");
            foreach(var bldg in gmlBuildings)
            {
                //create instance of GmlBldg
                var gmlBldg = new GmlBldg();
                //use gml_id as id for building
                gmlBldg.BldgId = bldg.Attribute(allns["gml"] + "id").Value;
                Log.Debug("Get building id: " + gmlBldg.BldgId);

                //read attributes for building (first level, no parts are handled internally in method)
                Log.Debug("Read all attribute values...");
                gmlBldg.BldgAttributes = new ReadSemValues().ReadAttributeValuesBldg(bldg, attributes, allns);
                Log.Debug("Attribute reading finished!");

                Log.Debug("Read all surfaces...");
                var surfaces = new List<GmlSurface>();

                surfaces = ReadSurfaces(bldg, attributes);

                gmlBldg.BldgSurfaces = surfaces;

                //investigation of building parts
                var gmlBuildingParts = bldg.Descendants(this.allns["bldg"] + "BuildingPart");    //in LOD1 und LOD2 possible

                var parts = new List<GmlBldgPart>();

                foreach(var part in gmlBuildingParts)
                {
                    //create instace for building part
                    var gmlBldgPart = new GmlBldgPart();

                    //use gml_id for building part
                    gmlBldgPart.BldgPartId = part.Attribute(allns["gml"] + "id").Value;

                    //read attributes for building part
                    gmlBldgPart.BldgPartAttributes = new ReadSemValues().ReadAttributeValuesBldg(part, attributes, allns);

                    var partSurfaces = new List<GmlSurface>();

                    Log.Debug("Read all part surfaces...");
                    partSurfaces = ReadSurfaces(part, attributes);

                    gmlBldgPart.PartSurfaces = partSurfaces;

                    parts.Add(gmlBldgPart);
                }
                //-------------------------------------------

                gmlBldg.Parts = parts;

                gmlBldgs.Add(gmlBldg);
            }

            #endregion BuildingInstances

            return gmlBldgs;
        }

        public List<GmlBldg> ImprovePolygonPoints(List<GmlBldg> bldgs)
        {
            var newBldgs = new List<GmlBldg>();

            foreach(var bldg in bldgs)
            {
                var surfaces = bldg.BldgSurfaces;

                var validation = new ValidateGeometry();

                var filteredSurfaces = validation.FilterUnneccessaryPoints(surfaces);

                //var flattenedSurfaces = validation.FlatteningSurfaces(filteredSurfaces);

                bldg.BldgSurfaces = filteredSurfaces;

                newBldgs.Add(bldg);
            }

            return newBldgs;
        }

        public List<GmlBldg> CalculateSolids(List<GmlBldg> bldgs)
        {
            var newBldgs = new List<GmlBldg>();

            Log.Debug("Calculate solid for each building...");
            foreach(var bldg in bldgs)
            {
                bldg.BldgSolid = new C2BSolid();

                var surfaces = bldg.BldgSurfaces;

                foreach(var surface in surfaces)
                {
                    bldg.BldgSolid.AddPlane(surface.SurfaceId, surface.PlaneExt.PolygonPts);
                    Log.Debug("Exterior plane created: " + surface.SurfaceId);

                    if(surface.PlaneInt.Count > 0)
                    {
                        foreach(var pl in surface.PlaneInt)

                            bldg.BldgSolid.AddPlane(pl.ID, pl.PolygonPts);
                        Log.Debug("Interior plane created: " + surface.SurfaceId + "_void");
                    }
                }

                var parts = bldg.Parts;

                var newParts = new List<GmlBldgPart>();

                foreach(var part in parts)
                {
                    var newPart = part;

                    var partSolid = new C2BSolid();

                    foreach(var partSurface in newPart.PartSurfaces)
                    {
                        partSolid.AddPlane(partSurface.SurfaceId, partSurface.PlaneExt.PolygonPts);
                        Log.Debug("Exterior plane for part created: " + partSurface.SurfaceId);

                        if(partSurface.PlaneInt.Count > 0)
                        {
                            foreach(var pl in partSurface.PlaneInt)

                                partSolid.AddPlane(pl.ID, pl.PolygonPts);
                            Log.Debug("Interior plane for part created: " + partSurface.SurfaceId + "_void");
                        }
                    }

                    partSolid.CalculatePositions();

                    newPart.PartSolid = partSolid; //.CalculatePositions();

                    newParts.Add(newPart);
                }

                bldg.Parts = newParts;

                //var abL = new List<double[]>();

                //foreach(var v in bldg.BldgSolid.Vertices)
                //{
                //    var ab = new double[] { v.Position.X, v.Position.Y, v.Position.Z };
                //    abL.Add(ab);
                //}

                Log.Debug("Calculate new vertex positions via level cut...");
                bldg.BldgSolid.CalculatePositions();

                //var abcL = new List<double[]>();

                //foreach(var v in bldg.BldgSolid.Vertices)
                //{
                //    var abc = new double[] { v.Position.X, v.Position.Y, v.Position.Z };
                //    abcL.Add(abc);
                //}

                //for(int i = 0; i < abcL.Count; i++)
                //{
                //    Log.Debug("Difference = " + (abcL[i][0] - abL[i][0]) + " / " + (abcL[i][1] - abL[i][1]) + " / " + (abcL[i][2] - abL[i][2]));
                //}

                newBldgs.Add(bldg);
            }

            return newBldgs;
        }
    }
}