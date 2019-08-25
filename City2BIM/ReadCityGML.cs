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
            Class1imp imp = new Class1imp();
            var path = imp.ImportPath();
            //-------------------------------
            Log.Information("File: " + path);

            //Hauptmethode zur Erstellung der Geometrie, Ermitteln der Attribute und Füllen der Attributwerte
            //var solidList = ReadXMLDoc(path, dxf); //ref für attributes?

            Log.Information("Start reading CityGML data...");
            //Daten einlesen mit Semantik sowie Geometrie (Rohdaten, Punktlisten)
            var gmlBuildings = ReadGmlData(path, dxf);

            Log.Information("Validate CityGML geometry data...");
            //Filter of surface points (Geometry validation)
            gmlBuildings = FilterPolygonPoints(gmlBuildings);

            Log.Information("Calculate solids from CityGML geometry data...");
            //Creation of Solids
            gmlBuildings = CalculateSolids(gmlBuildings);

            //erstellt Revit-seitig die Geometrie und ordnet Attributwerte zu (Achtung: ReadXMLDoc muss vorher ausgeführt werden)
            RevitGeometryBuilder cityModel = new RevitGeometryBuilder(doc, gmlBuildings, this.lowerCornerPt, dxf);

            //Parameter für Revit-Kategorie erstellen
            //nach ausgewählter Methode (Solids oder Flächen) Parameter an zugehörige Kategorien übergeben

            //erstellt Revit-seitig die Attribute (Achtung: ReadXMLDoc muss vorher ausgeführt werden)
            RevitSemanticBuilder citySem = new RevitSemanticBuilder(doc); //Übergabe der Methoden-Rückgaben zum Schreiben nach Revit

            if(solid)
            {
                var entourageAttr = from a in attributes
                                    where a.Reference == GmlAttribute.AttrHierarchy.bldg
                                    select a;

                Log.Information("Create Revit Parameters for Category Entorurage...");
                citySem.CreateParameters(BuiltInCategory.OST_Entourage, entourageAttr); //erstellt Shared Parameters für Kategorie Umgebung

                Log.Information("Calculate Revit Geometry for Building Solids...");
                cityModel.CreateBuildings(); //erstellt DirectShape-Geometrie als Kategorie Umgebung
            }
            else
            {
                var wallClosureAttr = from a in attributes
                                      where a.Reference == GmlAttribute.AttrHierarchy.bldg ||
                                      a.Reference == GmlAttribute.AttrHierarchy.wall ||
                                      a.Reference == GmlAttribute.AttrHierarchy.closure
                                      select a;

                Log.Information("Create Revit Parameters for Category Walls...");
                citySem.CreateParameters(BuiltInCategory.OST_Walls, wallClosureAttr);

                var roofAttr = from a in attributes
                               where a.Reference == GmlAttribute.AttrHierarchy.bldg ||
                               a.Reference == GmlAttribute.AttrHierarchy.roof
                               select a;

                Log.Information("Create Revit Parameters for Category Roof...");
                citySem.CreateParameters(BuiltInCategory.OST_Roofs, roofAttr);

                var groundAttr = from a in attributes
                                 where a.Reference == GmlAttribute.AttrHierarchy.bldg ||
                                 a.Reference == GmlAttribute.AttrHierarchy.ground
                                 select a;

                Log.Information("Create Revit Parameters for Category Ground...");
                citySem.CreateParameters(BuiltInCategory.OST_StructuralFoundation, groundAttr);

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

        //private bool GetGlobalLC(XDocument xmlDoc, ref C2BPoint lowerCorner)
        //{
        //    var gml = this.allns["gml"];

        //    var globalBoundedBy = xmlDoc.Elements(allns["core"] + "CityModel").Single().Elements(gml + "boundedBy").FirstOrDefault();

        //    //var globalBoundedBy = xmlDoc.Elements(gml + "boundedBy").FirstOrDefault(); //sollte nur einmal vorkommen

        //    if(globalBoundedBy != null)
        //    {
        //        var LC = globalBoundedBy.Descendants(gml + "lowerCorner").FirstOrDefault();

        //        CalcLowerCorner(LC, ref lowerCorner);

        //        return true;
        //    }
        //    else
        //        return false;
        //}

        //private bool GetBuildingLC(XElement bldg, ref C2BPoint lowerCorner)
        //{
        //    var gml = this.allns["gml"];

        //    var LC = bldg.Elements(gml + "boundedBy").Single().Descendants(gml + "lowerCorner");

        //    if(LC.Count() == 1)
        //    {
        //        CalcLowerCorner(LC.Single(), ref lowerCorner);
        //        return true;
        //    }
        //    else
        //        return false;
        //}

        //private void CalcLowerCorner(XElement gmlCorner, ref C2BPoint lowerCorner)
        //{
        //    var pointSeperated = gmlCorner.Value.Split(new[] { (' ') }, StringSplitOptions.RemoveEmptyEntries);
        //    lowerCorner.X = Double.Parse(pointSeperated[0], CultureInfo.InvariantCulture);
        //    lowerCorner.Y = Double.Parse(pointSeperated[1], CultureInfo.InvariantCulture);
        //    lowerCorner.Z = Double.Parse(pointSeperated[2], CultureInfo.InvariantCulture);

        //    //Log.Information(String.Format("Eine lower Corner: {0,5:N3} {1,5:N3} {2,5:N3}  ", lowerCorner.X, lowerCorner.Y, lowerCorner.Z));
        //}

        //public Dictionary<C2BSolid, Dictionary<GmlAttribute, string>> ReadXMLDoc(string path, DxfVisualizer dxf)
        //{
        //    var solids = new Dictionary<C2BSolid, Dictionary<GmlAttribute, string>>();

        //    XDocument xmlDoc = XDocument.Load(path);

        //    this.allns = xmlDoc.Root.Attributes().
        //        Where(a => a.IsNamespaceDeclaration).
        //        GroupBy(a => a.Name.Namespace == XNamespace.None ? String.Empty : a.Name.LocalName,
        //        a => XNamespace.Get(a.Value)).
        //        ToDictionary(g => g.Key,
        //             g => g.First());

        //    foreach(var ns in allns)
        //    {
        //        Log.Debug("Namespaces: " + ns.Key);
        //    }

        //    if(this.allns.ContainsKey(""))
        //    {
        //        if(!this.allns.ContainsKey("core"))
        //            this.allns.Add("core", this.allns[""]);     //wenn Namespace ohne prefix vorhanden ist, ist das das core-Modul
        //    }

        //    double offsetX = 0.0;
        //    double offsetY = 0.0;
        //    double offsetZ = 0.0;

        //    C2BPoint lowerCorner = new C2BPoint(offsetX, offsetY, offsetZ);

        //    var globLC = GetGlobalLC(xmlDoc, ref lowerCorner);
        //    Log.Debug("Global LC?: " + globLC);

        //    var gmlBuildings = xmlDoc.Descendants(this.allns["bldg"] + "Building"); //alle Bldg-Elemente samt Kindern und Kindeskindern
        //    Log.Debug("Anzahl von Buildings: " + gmlBuildings.Count());

        //    //Semantik:____________________________________________________________________

        //    //Anlegen der semantischen Attribute (ohne Werte):
        //    //zunächst Parsen der XML-Schema-Dateien für festgelegte (möglich vorkommende) Attribute, die im bldg- und core-Modul definiert sind
        //    //Auslesen nur einmal (!)

        //    var sem = new ReadSemAttributes();

        //    //vorgegebene Semantik aus CityGML-Schema:

        //    this.attributes = sem.GetSchemaAttributes();

        //    //Anlegen der semantischen Attribute (ohne Werte):
        //    //Parsen der generischen Attribute
        //    //Auslesen pro Bldg

        //    var genAttr = sem.ReadGenericAttributes(gmlBuildings, this.allns["gen"]);

        //    this.attributes.UnionWith(genAttr);

        //    Log.Debug("Gefundene Attribute:");

        //    foreach(var attr in attributes)
        //    {
        //        Log.Debug(" - " + attr.GmlNamespace + " , " + attr.Name);
        //    }

        //    //Geometrie:____________________________________________________________________

        //    int i = 0, j = 0;

        //    Log.Information("Auslesen der einzelnen Gebäude:");

        //    foreach(XElement xmlBuildingNode in gmlBuildings)
        //    {
        //        Log.Information("---------------------------------------------------------------------------------------");
        //        Log.Information("Bldg-Id: " + xmlBuildingNode.FirstAttribute.Value);

        //        //lokale BoundingBox, wenn nötig (gibts das auch bei parts?)
        //        //---------------------------------
        //        if(!globLC)
        //        {
        //            var bldgLC = GetBuildingLC(xmlBuildingNode, ref lowerCorner);

        //            Log.Debug("- LocalLC?: " + bldgLC);

        //            if(!globLC && !bldgLC)
        //                Log.Debug("- No global and local lower corner was found!");
        //        }
        //        //---------------------------------

        //        var semVal = new ReadSemValues();
        //        var geom = new ReadGeomData(this.allns, dxf);

        //        //Spezialfall: BuildingParts
        //        //- Geometrie liegt unterhalb Part
        //        //- Semantik liegt teilweise unterhalb Part (spezifische Attribute, wie Dachform)
        //        //- Semantik liegt teilweise unterhalb Building (selbe Ebene wie Parts, allgemeine Semantik für alle Parts, Bsp. Adresse)

        //        var xmlParts = xmlBuildingNode.Elements(this.allns["bldg"] + "consistsOfBuildingPart");

        //        var partsCt = xmlParts.Count();

        //        Log.Information("- BuildingParts?: " + partsCt);

        //        //allgemeine Semantik (direkt in Building)
        //        var bldgSemAllg = semVal.ReadAttributeValuesBldg(xmlBuildingNode, attributes, allns); //in Methode regeln, dass Parts nicht gelesen werden

        //        Log.Information("- Allgemeine Semantik?: " + bldgSemAllg.Count);

        //        var bldgSemSpez = new Dictionary<GmlAttribute, string>();

        //        var bldgGeom = new C2BSolid();

        //        //Geometrie auslesen hängt davon ab, ob es Parts gibt (geometriebildend sind die Parts, jeder Part wird eigenständiges Revit-Objekt)
        //        //Beispieldatensätze zeigen unterschiedliche Implementierungen
        //        //Fall a: keine Parts implementiert -> Geometrie immer direkt unterhalb als Kinder von Building (zB Berlin)
        //        //Fall b: Parts implementiert -> Bldg, die Parts enthalten, speichern Geometrie nur als Kinder von BuildingPart (zB TH)
        //        //Fall c: Parts implementiert -> Bldg, die Parts enthalten, speichern Geometrie gemischt -> tlw. als Kinder von Bldg, tlw als Kinder von Part (zB BB)

        //        if(xmlParts.Any())     //Fall b und c (Gebäude, die Parts haben)
        //        {
        //            if(xmlBuildingNode.Elements(this.allns["bldg"] + "boundedBy").Any() ||
        //                xmlBuildingNode.Elements(this.allns["bldg"] + "lod1Solid").Any() ||
        //                xmlBuildingNode.Elements(this.allns["bldg"] + "lod1MultiSurface").Any())   //Fall c (LOD2 und LOD1)
        //            {
        //                bldgGeom = geom.CollectBuilding(xmlBuildingNode, lowerCorner);

        //                solids.Add(bldgGeom, bldgSemAllg);
        //            }

        //            var xmlBuildingParts = xmlParts.Elements(this.allns["bldg"] + "BuildingPart");    //in LOD1 und LOD2 möglich

        //            foreach(var part in xmlBuildingParts)       //Fall b und c
        //            {
        //                //zusätzlich spezifische Semantik je Part auslesen

        //                Log.Information("BldgPart-Id: " + part.FirstAttribute.Value);

        //                bldgGeom = geom.CollectBuilding(part, lowerCorner);

        //                bldgSemSpez = semVal.ReadAttributeValuesBldg(part, attributes, allns);

        //                Log.Information("- Spezifische Semantik?: " + bldgSemSpez.Count);

        //                Dictionary<GmlAttribute, string> bldgSem = new Dictionary<GmlAttribute, string>(bldgSemAllg);

        //                foreach(KeyValuePair<GmlAttribute, String> kvp in bldgSemSpez)
        //                {
        //                    Log.Debug("- Parts-Dictionary lesen...");

        //                    if(!bldgSem.ContainsKey(kvp.Key))
        //                    {
        //                        bldgSem.Add(kvp.Key, kvp.Value);

        //                        Log.Debug("- Key nicht allgemein vorhanden --> wird hinzugefügt: " + kvp.Key.Name);
        //                    }
        //                    else
        //                    {
        //                        Log.Debug("- Key allgemein schon vorhanden --> wird ersetzt: " + kvp.Key.Name);
        //                        Log.Debug("- alter Wert: " + bldgSem[kvp.Key]);

        //                        bldgSem[kvp.Key] = kvp.Value;       //Überschreiben des Attributes, falls es in Part wieder vorkommt

        //                        Log.Debug("- neuer Wert: " + bldgSem[kvp.Key]);
        //                    }
        //                }

        //                //SolidList benötigt schließlich geometrisch gebildete Solids aus bldg(parts) sowie die dazugehörige Semantik (Attribute+Wert) je bldg(part)
        //                solids.Add(bldgGeom, bldgSem);
        //            }
        //        }
        //        else
        //        {   //Fall a (und alle Gebäude für Fall b und c, die keine Parts haben)
        //            //Geometrie des Buildings
        //            bldgGeom = geom.CollectBuilding(xmlBuildingNode, lowerCorner);

        //            solids.Add(bldgGeom, bldgSemAllg);
        //        }
        //    }

        //    return solids;
        //}

        //-----
        //----
        //------

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

        private List<GmlSurface> ReadSurfaceData(IEnumerable<XElement> faces, C2BPoint lowerCorner, GmlSurface.FaceType type, HashSet<GmlAttribute> attributes)
        {
            var surfaces = new List<GmlSurface>();

            foreach(var face in faces)
            {
                var polygons = face.Descendants(allns["gml"] + "Polygon").ToList();

                foreach(var poly in polygons)
                {
                    var surface = new GmlSurface();

                    if(polygons.Count == 1)
                        surface.SurfaceId = face.Attribute(allns["gml"] + "id").Value;
                    else
                        surface.SurfaceId = poly.Attribute(allns["gml"] + "id").Value;

                    surface.Facetype = type;
                    surface.SurfaceAttributes = new ReadSemValues().ReadAttributeValuesSurface(face, attributes, type);

                    var posListExt = poly.Descendants(allns["gml"] + "posList").Where(x => x.Ancestors(allns["gml"] + "exterior").Count() > 0);
                    Log.Debug("Amount exterior polygons: " + posListExt.Count());

                    if(posListExt.Any())
                        surface.Exterior = CollectPoints(posListExt.FirstOrDefault(), lowerCorner);

                    var posListInt = poly.Descendants(allns["gml"] + "posList").Where(x => x.Ancestors(allns["gml"] + "interior").Count() > 0);
                    Log.Debug("Amount interior polygons: " + posListInt.Count());

                    if(posListInt.Any())
                        surface.Interior = CollectPoints(posListInt.FirstOrDefault(), lowerCorner);

                    surfaces.Add(surface);
                }
            }

            return surfaces.ToList();
        }

        public List<GmlBldg> ReadGmlData(string path, DxfVisualizer dxf)
        {
            //Load XML document
            XDocument gmlDoc = XDocument.Load(path);
            //-----------------------------------------

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

            //For better calculation, Identify lower Corner
            Log.Debug("Read lowerCorner for mathematical operations (first oocurence in file)...");
            var lowerCorner = gmlDoc.Descendants(this.allns["gml"] + "lowerCorner").FirstOrDefault();
            this.lowerCornerPt = CollectPoint(lowerCorner, new C2BPoint(0, 0, 0));
            Log.Debug("Lower Corner: " + lowerCornerPt.X + " ," + lowerCornerPt.Y + " ," + lowerCornerPt.Z);

            //Read all overall building elements
            Log.Debug("Read all bldg:Building elements...");
            var gmlBuildings = gmlDoc.Descendants(this.allns["bldg"] + "Building");
            Log.Debug("Amount: " + gmlBuildings.Count());
            //--------------------------------------------------------------------------

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

                var walls = ReadSurfaceData(bldg.Descendants(this.allns["bldg"] + "WallSurface"), lowerCornerPt, GmlSurface.FaceType.wall, attributes);
                var roofs = ReadSurfaceData(bldg.Descendants(this.allns["bldg"] + "RoofSurface"), lowerCornerPt, GmlSurface.FaceType.roof, attributes);
                var closures = ReadSurfaceData(bldg.Descendants(this.allns["bldg"] + "ClosureSurface"), lowerCornerPt, GmlSurface.FaceType.closure, attributes);
                var grounds = ReadSurfaceData(bldg.Descendants(this.allns["bldg"] + "GroundSurface"), lowerCornerPt, GmlSurface.FaceType.ground, attributes);

                Log.Debug("Amount walls: " + walls.Count);
                Log.Debug("Amount roofs: " + roofs.Count);
                Log.Debug("Amount closures: " + closures.Count);
                Log.Debug("Amount grounds: " + grounds.Count);

                surfaces.AddRange(walls);
                surfaces.AddRange(roofs);
                surfaces.AddRange(closures);
                surfaces.AddRange(grounds);

                gmlBldg.BldgSurfaces = surfaces;


                //TO DO: EINBINDEN DER PARTS + LOGGING

                //investigation of building parts
                var gmlBuildingParts = bldg.Elements(this.allns["bldg"] + "BuildingPart");    //in LOD1 und LOD2 possible

                foreach(var part in gmlBuildingParts)
                {
                    //create instace for building part
                    var gmlBldgPart = new GmlBldgPart();

                    //use gml_id for building part
                    gmlBldgPart.BldgPartId = part.Attribute(allns["gml"] + "id").Value;

                    //read attributes for building part
                    gmlBldgPart.BldgPartAttributes = new ReadSemValues().ReadAttributeValuesBldg(part, attributes, allns);

                    //add parts to GmlBldg list property
                    gmlBldg.Parts.Add(gmlBldgPart);
                }
                //-------------------------------------------

                gmlBldgs.Add(gmlBldg);
            }

            return gmlBldgs;
        }

        public List<GmlBldg> FilterPolygonPoints(List<GmlBldg> bldgs)
        {
            var newBldgs = new List<GmlBldg>();

            foreach(var bldg in bldgs)
            {
                var surfaces = bldg.BldgSurfaces;

                var validation = new ValidateGeometry();
                var filteredSurfaces = validation.FilterUnneccessaryPoints(surfaces);

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
                    bldg.BldgSolid.AddPlane(surface.SurfaceId, surface.Exterior);
                    Log.Debug("Exterior plane created: " + surface.SurfaceId);

                    if(surface.Interior != null)
                    {
                        bldg.BldgSolid.AddPlane(surface.SurfaceId + "_void", surface.Interior);
                        Log.Debug("Interior plane created: " + surface.SurfaceId + "_void");
                    }
                }

                var abL = new List<double[]>();

                foreach (var v in bldg.BldgSolid.Vertices)
                {
                    var ab = new double[] { v.Position.X, v.Position.Y, v.Position.Z };
                    abL.Add(ab);
                }

                //Log.Debug("Calculate new vertex positions via level cut...");
                bldg.BldgSolid.CalculatePositions();

                var abcL = new List<double[]>();

                foreach(var v in bldg.BldgSolid.Vertices)
                {
                    var abc = new double[] { v.Position.X, v.Position.Y, v.Position.Z };
                    abcL.Add(abc);
                }

                for (int i = 0; i < abcL.Count; i++)
                {
                    Log.Debug("Difference = " + (abcL[i][0] - abL[i][0]) + " / " + (abcL[i][1] - abL[i][1]) + " / " + (abcL[i][2] - abL[i][2]));
                }



                

                newBldgs.Add(bldg);
            }

            return newBldgs;
        }
    }
}