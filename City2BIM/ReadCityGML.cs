using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using City2BIM.GetGeometry;
using City2BIM.GetSemantics;
using City2BIM.RevitBuilder;
using Serilog;
using Attribute = City2BIM.GetSemantics.Attribute;
using Solid = City2BIM.GetGeometry.Solid;
using XYZ = City2BIM.GetGeometry.XYZ;

namespace City2BIM
{
    /// <remarks>
    /// The "HelloWorld" external command. The class must be Public.
    /// </remarks>
    public class ReadCityGML
    {
        // The main Execute method (inherited from IExternalCommand) must be public
        public ReadCityGML(ExternalCommandData revit, bool solid)
        {
            Log.Logger = new LoggerConfiguration()
                //.MinimumLevel.Debug()
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
            var solidList = ReadXMLDoc(path, dxf); //ref für attributes?

            Log.Debug("ReadData-Object, gelesene Geometrien = " + solidList.Count);

            //erstellt Revit-seitig die Attribute (Achtung: ReadXMLDoc muss vorher ausgeführt werden)
            RevitSemanticBuilder citySem = new RevitSemanticBuilder(doc, this.attributes); //Übergabe der Methoden-Rückgaben zum Schreiben nach Revit

            //erstellt Revit-seitig die Geometrie und ordnet Attributwerte zu (Achtung: ReadXMLDoc muss vorher ausgeführt werden)
            RevitGeometryBuilder cityModel = new RevitGeometryBuilder(doc, solidList, dxf);

            Transform revitTransf = GetRevitProjectLocation(doc);

            //Parameter für Revit-Kategorie erstellen
            //nach ausgewählter Methode (Solids oder Flächen) Parameter an zugehörige Kategorien übergeben

            if(solid)
            {
                citySem.CreateParameters(BuiltInCategory.OST_Entourage); //erstellt Shared Parameters für Kategorie Umgebung
                cityModel.CreateBuildings(path, revitTransf); //erstellt DirectShape-Geometrie als Kategorie Umgebung
            }
            else
            {
                citySem.CreateParameters(BuiltInCategory.OST_Walls);
                citySem.CreateParameters(BuiltInCategory.OST_Roofs);
                citySem.CreateParameters(BuiltInCategory.OST_StructuralFoundation);
                cityModel.CreateBuildingsWithFaces(revitTransf); //erstellt DirectShape-Geometrien der jeweiligen Kategorie
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

        private Dictionary<string, XNamespace> allns;
        private HashSet<Attribute> attributes = new HashSet<Attribute>();

        private bool GetGlobalLC(XDocument xmlDoc, ref XYZ lowerCorner)
        {
            var gml = this.allns["gml"];

            var globalBoundedBy = xmlDoc.Elements(allns["core"] + "CityModel").Single().Elements(gml + "boundedBy").FirstOrDefault();

            //var globalBoundedBy = xmlDoc.Elements(gml + "boundedBy").FirstOrDefault(); //sollte nur einmal vorkommen

            if(globalBoundedBy != null)
            {
                var LC = globalBoundedBy.Descendants(gml + "lowerCorner").FirstOrDefault();

                CalcLowerCorner(LC, ref lowerCorner);

                return true;
            }
            else
                return false;
        }

        private bool GetBuildingLC(XElement bldg, ref XYZ lowerCorner)
        {
            var gml = this.allns["gml"];

            var LC = bldg.Elements(gml + "boundedBy").Single().Descendants(gml + "lowerCorner");

            if(LC.Count() == 1)
            {
                CalcLowerCorner(LC.Single(), ref lowerCorner);
                return true;
            }
            else
                return false;
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

        private void CalcLowerCorner(XElement gmlCorner, ref XYZ lowerCorner)
        {
            var pointSeperated = gmlCorner.Value.Split(new[] { (' ') }, StringSplitOptions.RemoveEmptyEntries);
            lowerCorner.X = Double.Parse(pointSeperated[0], CultureInfo.InvariantCulture);
            lowerCorner.Y = Double.Parse(pointSeperated[1], CultureInfo.InvariantCulture);
            lowerCorner.Z = Double.Parse(pointSeperated[2], CultureInfo.InvariantCulture);

            //Log.Information(String.Format("Eine lower Corner: {0,5:N3} {1,5:N3} {2,5:N3}  ", lowerCorner.X, lowerCorner.Y, lowerCorner.Z));
        }

        public Dictionary<Solid, Dictionary<Attribute, string>> ReadXMLDoc(string path, DxfVisualizer dxf)
        {
            var solids = new Dictionary<Solid, Dictionary<Attribute, string>>();

            XDocument xmlDoc = XDocument.Load(path);

            this.allns = xmlDoc.Root.Attributes().
                Where(a => a.IsNamespaceDeclaration).
                GroupBy(a => a.Name.Namespace == XNamespace.None ? String.Empty : a.Name.LocalName,
                a => XNamespace.Get(a.Value)).
                ToDictionary(g => g.Key,
                     g => g.First());

            foreach(var ns in allns)
            {
                Log.Debug("Namespaces: " + ns.Key);
            }

            if(this.allns.ContainsKey(""))
            {
                if(!this.allns.ContainsKey("core"))
                    this.allns.Add("core", this.allns[""]);     //wenn Namespace ohne prefix vorhanden ist, ist das das core-Modul
            }

            double offsetX = 0.0;
            double offsetY = 0.0;
            double offsetZ = 0.0;

            XYZ lowerCorner = new XYZ(offsetX, offsetY, offsetZ);

            var globLC = GetGlobalLC(xmlDoc, ref lowerCorner);
            Log.Debug("Global LC?: " + globLC);

            var gmlBuildings = xmlDoc.Descendants(this.allns["bldg"] + "Building"); //alle Bldg-Elemente samt Kindern und Kindeskindern
            Log.Debug("Anzahl von Buildings: " + gmlBuildings.Count());

            //Semantik:____________________________________________________________________

            //Anlegen der semantischen Attribute (ohne Werte):
            //zunächst Parsen der XML-Schema-Dateien für festgelegte (möglich vorkommende) Attribute, die im bldg- und core-Modul definiert sind
            //Auslesen nur einmal (!)

            var sem = new ReadSemAttributes();

            //vorgegebene Semantik aus CityGML-Schema:

            this.attributes = sem.GetSchemaAttributes();

            //Anlegen der semantischen Attribute (ohne Werte):
            //Parsen der generischen Attribute
            //Auslesen pro Bldg

            var genAttr = sem.ReadGenericAttributes(gmlBuildings, this.allns["gen"]);

            this.attributes.UnionWith(genAttr);

            Log.Debug("Gefundene Attribute:");

            foreach(var attr in attributes)
            {
                Log.Debug(" - " + attr.GmlNamespace + " , " + attr.Name);
            }

            //Geometrie:____________________________________________________________________

            int i = 0, j = 0;

            Log.Information("Auslesen der einzelnen Gebäude:");

            foreach(XElement xmlBuildingNode in gmlBuildings)
            {
                Log.Information("---------------------------------------------------------------------------------------");
                Log.Information("Bldg-Id: " + xmlBuildingNode.FirstAttribute.Value);

                //lokale BoundingBox, wenn nötig (gibts das auch bei parts?)
                //---------------------------------
                if(!globLC)
                {
                    var bldgLC = GetBuildingLC(xmlBuildingNode, ref lowerCorner);

                    Log.Debug("- LocalLC?: " + bldgLC);

                    if(!globLC && !bldgLC)
                        Log.Debug("- No global and local lower corner was found!");
                }
                //---------------------------------

                var semVal = new ReadSemValues();
                var geom = new ReadGeomData(this.allns, dxf);

                //Spezialfall: BuildingParts
                //- Geometrie liegt unterhalb Part
                //- Semantik liegt teilweise unterhalb Part (spezifische Attribute, wie Dachform)
                //- Semantik liegt teilweise unterhalb Building (selbe Ebene wie Parts, allgemeine Semantik für alle Parts, Bsp. Adresse)

                var xmlParts = xmlBuildingNode.Elements(this.allns["bldg"] + "consistsOfBuildingPart");

                var partsCt = xmlParts.Count();

                Log.Information("- BuildingParts?: " + partsCt);

                //allgemeine Semantik (direkt in Building)
                var bldgSemAllg = semVal.ReadAttributeValues(xmlBuildingNode, attributes, allns); //in Methode regeln, dass Parts nicht gelesen werden

                Log.Information("- Allgemeine Semantik?: " + bldgSemAllg.Count);

                var bldgSemSpez = new Dictionary<Attribute, string>();

                var bldgGeom = new Solid();

                //Geometrie auslesen hängt davon ab, ob es Parts gibt (geometriebildend sind die Parts, jeder Part wird eigenständiges Revit-Objekt)
                //Beispieldatensätze zeigen unterschiedliche Implementierungen
                //Fall a: keine Parts implementiert -> Geometrie immer direkt unterhalb als Kinder von Building (zB Berlin)
                //Fall b: Parts implementiert -> Bldg, die Parts enthalten, speichern Geometrie nur als Kinder von BuildingPart (zB TH)
                //Fall c: Parts implementiert -> Bldg, die Parts enthalten, speichern Geometrie gemischt -> tlw. als Kinder von Bldg, tlw als Kinder von Part (zB BB)

                if(xmlParts.Any())     //Fall b und c (Gebäude, die Parts haben)
                {
                    if(xmlBuildingNode.Elements(this.allns["bldg"] + "boundedBy").Any() ||
                        xmlBuildingNode.Elements(this.allns["bldg"] + "lod1Solid").Any() ||
                        xmlBuildingNode.Elements(this.allns["bldg"] + "lod1MultiSurface").Any())   //Fall c (LOD2 und LOD1)
                    {
                        bldgGeom = geom.CollectBuilding(xmlBuildingNode, lowerCorner);

                        solids.Add(bldgGeom, bldgSemAllg);
                    }

                    var xmlBuildingParts = xmlParts.Elements(this.allns["bldg"] + "BuildingPart");    //in LOD1 und LOD2 möglich

                    foreach(var part in xmlBuildingParts)       //Fall b und c
                    {
                        //zusätzlich spezifische Semantik je Part auslesen

                        Log.Information("BldgPart-Id: " + part.FirstAttribute.Value);

                        bldgGeom = geom.CollectBuilding(part, lowerCorner);

                        bldgSemSpez = semVal.ReadAttributeValues(part, attributes, allns);

                        Log.Information("- Spezifische Semantik?: " + bldgSemSpez.Count);

                        Dictionary<Attribute, string> bldgSem = new Dictionary<Attribute, string>(bldgSemAllg);

                        foreach(KeyValuePair<Attribute, String> kvp in bldgSemSpez)
                        {
                            Log.Debug("- Parts-Dictionary lesen...");

                            if(!bldgSem.ContainsKey(kvp.Key))
                            {
                                bldgSem.Add(kvp.Key, kvp.Value);

                                Log.Debug("- Key nicht allgemein vorhanden --> wird hinzugefügt: " + kvp.Key.Name);
                            }
                            else
                            {
                                Log.Debug("- Key allgemein schon vorhanden --> wird ersetzt: " + kvp.Key.Name);
                                Log.Debug("- alter Wert: " + bldgSem[kvp.Key]);

                                bldgSem[kvp.Key] = kvp.Value;       //Überschreiben des Attributes, falls es in Part wieder vorkommt

                                Log.Debug("- neuer Wert: " + bldgSem[kvp.Key]);
                            }
                        }

                        //SolidList benötigt schließlich geometrisch gebildete Solids aus bldg(parts) sowie die dazugehörige Semantik (Attribute+Wert) je bldg(part)
                        solids.Add(bldgGeom, bldgSem);
                    }
                }
                else
                {   //Fall a (und alle Gebäude für Fall b und c, die keine Parts haben)
                    //Geometrie des Buildings
                    bldgGeom = geom.CollectBuilding(xmlBuildingNode, lowerCorner);

                    solids.Add(bldgGeom, bldgSemAllg);
                }
            }

            return solids;
        }
    }
}