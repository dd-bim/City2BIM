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
using netDxf;

namespace City2BIM
{
    /// <remarks>
    /// The "HelloWorld" external command. The class must be Public.
    /// </remarks>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class ReadCityGML : IExternalCommand
    {
        // The main Execute method (inherited from IExternalCommand) must be public
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
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

            var folder = @"D:\1_CityBIM\1_Programmierung\City2BIM\CityGML_Data\CityGML_Data\";

            //var path = @"files\Berlin\input_clean_3.gml";
            //var path = @"files\Erfurt\LoD2_642_5648_2_TH\LoD2_642_5648_2_TH.gml";
            //var path = @"files\NRW\LoD2_370_5667_1_NW.gml";
            //var path = @"files\Greiz\LoD2_726_5616_2_TH\LoD2_726_5616_2_TH.gml";
            //var path = @"files\Greiz\LoD2_726_5616_2_TH\sorben8_9.gml";
            //var path = @"files\Dresden\Gebaeude_LOD1_citygml.gml";
            //var path = @"files\Dresden\Gebaeude_LOD2_citygml.gml";
            //var path = @"files\Dresden\Gebaeude_LOD3_citygml.gml";
            //var path = @"files\Dresden\Vegetation.gml";
            //var path = @"files\Greiz\LoD2_726_5616_2_TH\Greiz_bldg_parts.gml";
            //var path = @"files\Bayern\714_5323.gml"; //große Kachel (ca. 30% Erfolg) --> raised to 85% ! --> 92 % (prop: 1mm²)
            //var path = @"files\Bayern\713_5322.gml"; //kleine Kachel mit nur 2 Gebäuden
            //var path = @"files\Brandenburg\testdaten_lod2.gml";
            //var path = @"files\Bayern\LoD1UTM32city_p.gml";

            Log.Information("File: " + path);

            //Hauptmethode zur Erstellung der Geometrie, Ermitteln der Attribute und Füllen der Attributwerte
            var solidList = ReadXMLDoc(path, dxf); //ref für attributes?

            //var solidList = ReadXMLDoc(folder + path); //ref für attributes?

            //erstellt Revit-seitig die Attribute (Achtung: ReadXMLDoc muss vorher ausgeführt werden)
            RevitSemanticBuilder citySem = new RevitSemanticBuilder(doc, this.attributes); //Übergabe der Methoden-Rückgaben zum Schreiben nach Revit
            citySem.CreateParameters(); //erstellt Shared Parameters für Kategorie Umgebung

            //erstellt Revit-seitig die Geometrie und ordnet Attributwerte zu (Achtung: ReadXMLDoc muss vorher ausgeführt werden)
            RevitGeometryBuilder cityModel = new RevitGeometryBuilder(doc, solidList, dxf);
            cityModel.CreateBuildings(path); //erstellt DirectShape-Geometrie als Kategorie Umgebung

            Log.Debug("ReadData-Object, gelesene Geometrien = " + solidList.Count);

            string res = "";

            if(cityModel == null)
                res = "empty";
            else
                res = "not empty";

            Log.Debug("CityModel: " + res);

            //debug

            dxf.DrawDxf(path);

            return Result.Succeeded;
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

                var xmlBuildingParts = xmlBuildingNode.Descendants(this.allns["bldg"] + "BuildingPart");

                var partsCt = xmlBuildingParts.Count();

                Log.Information("- BuildingParts?: " + partsCt);

                //allgemeine Semantik (direkt in Building)
                var bldgSemAllg = semVal.ReadAttributeValues(xmlBuildingNode, attributes, allns); //in Methode regeln, dass Parts nicht gelesen werden

                Log.Information("- Allgemeine Semantik?: " + bldgSemAllg.Count);

                var bldgSemSpez = new Dictionary<Attribute, string>();

                var bldgGeom = new Solid();

                //Geometrie auslesen hängt davon ab, ob es Parts gibt (geometriebildend sind die Parts, jeder Part wird eigenständiges Revit-Objekt)
                if(partsCt > 0)
                {
                    foreach(var part in xmlBuildingParts)
                    {
                        //zusätzlich spezifische Semantik je Part auslesen

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
                {
                    //Geometrie des Buildings
                    bldgGeom = geom.CollectBuilding(xmlBuildingNode, lowerCorner);

                    solids.Add(bldgGeom, bldgSemAllg);
                }
            }

            return solids;
        }
    }
}