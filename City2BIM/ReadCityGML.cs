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

            UIApplication uiApp = revit.Application;
            Document doc = uiApp.ActiveUIDocument.Document;

            TaskDialog.Show("CityGML", "Button funzt");

            ReadXMLDoc(); //erstellt SolidList mit Gebäudegeometrie sowie Attributes mit allen Attributen

            //Semantics:

            RevitSemanticBuilder citySem = new RevitSemanticBuilder(doc, attributes); //Übergabe der Methoden-Rückgaben zum Schreiben nach Revit

            citySem.CreateParameters(); //erstellt Shared Parameters für Kategorie Umgebung

            RevitGeometryBuilder cityModel = new RevitGeometryBuilder(doc, this.solidList);

            cityModel.CreateBuildings(); //erstellt DirectShape-Geometrie als Kategorie Umgebung

            //TO DO:

            //Zuweisen der Attributwerte zum jeweiligen DirectShape

            //debug:

            Log.Information("Start...");

            Log.Debug("ReadData-Object, gelesene Geometrien = " + this.solidList.Count);

            string res = "";

            if(cityModel == null)
                res = "empty";
            else
                res = "not empty";

            Log.Debug("CityModel: " + res);

            //debug

            return Result.Succeeded;
        }

        private Dictionary<string, XNamespace> allns;
        private Dictionary<Solid, Dictionary<Attribute, string>> solidList = new Dictionary<Solid, Dictionary<Attribute, string>>();
        private HashSet<Attribute> attributes = new HashSet<Attribute>();

        // 1. Find LowerCorner (Global oder Lokal?)
        //IEnumerable<XElement> pointLowerCorner = xmlDoc.Descendants(gml + "lowerCorner");
        ////int clc = pointLowerCorner.Count();
        //switch(globalLCct)
        //{
        //    case 0:
        //        Log.Information("keine lower Corner");
        //        break;

        //    case 1:
        //        readOffset(ref offsetX, ref offsetY, ref offsetZ, globalLC.First());
        //        Log.Information(String.Format("Eine lower Corner: {0,5:N3} {1,5:N3} {2,5:N3}  ", offsetX, offsetY, offsetZ));
        //        break;

        //    default:
        //        Log.Information("Anzahl lower Corner: " + clc);
        //        break;
        //}

        //return new XYZ(offsetX, offsetY, offsetZ);

        private void readOffset(ref Double x, ref Double y, ref Double z, XElement e)
        {
            var pointSeperated = e.Value.Split(new[] { (' ') }, StringSplitOptions.RemoveEmptyEntries);
            x = Double.Parse(pointSeperated[0], CultureInfo.InvariantCulture);
            y = Double.Parse(pointSeperated[1], CultureInfo.InvariantCulture);
            z = Double.Parse(pointSeperated[2], CultureInfo.InvariantCulture);
        }

        private bool GetGlobalLC(XDocument xmlDoc, ref XYZ lowerCorner)
        {
            var gml = this.allns["gml"];

            var LC = xmlDoc.Elements(gml + "lowerCorner");

            if(LC.Count() == 1)
            {
                CalcLowerCorner(LC.Single(), ref lowerCorner);

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
            readOffset(ref lowerCorner.X, ref lowerCorner.Y, ref lowerCorner.Z, gmlCorner);
            Log.Information(String.Format("Eine lower Corner: {0,5:N3} {1,5:N3} {2,5:N3}  ", lowerCorner.X, lowerCorner.Y, lowerCorner.Z));
        }

        public void ReadXMLDoc()
        {
            //-------------------------

            var folder = @"D:\1_CityBIM\1_Programmierung\City2BIM\CityGML_Data\CityGML_Data\";

            var path = @"files\Berlin\input_clean_3.gml";
            //var path = @"files\Erfurt\LoD2_642_5648_2_TH\LoD2_642_5648_2_TH.gml";
            //var path = @"files\Dresden\Gebaeude_LOD1_citygml.gml";
            //var path = @"files\Dresden\Gebaeude_LOD2_citygml.gml";
            //var path = @"files\Dresden\Gebaeude_LOD3_citygml.gml";
            //var path = @"files\Dresden\Vegetation.gml";
            //var path = @"files\Einzelhaus.gml";

            XDocument xmlDoc = XDocument.Load(folder + path);

            this.allns = xmlDoc.Root.Attributes().
                Where(a => a.IsNamespaceDeclaration).
                GroupBy(a => a.Name.Namespace == XNamespace.None ? String.Empty : a.Name.LocalName,
                a => XNamespace.Get(a.Value)).
                ToDictionary(g => g.Key,
                     g => g.First());

            double offsetX = 0.0;
            double offsetY = 0.0;
            double offsetZ = 0.0;

            XYZ lowerCorner = new XYZ(offsetX, offsetY, offsetZ);

            var globLC = GetGlobalLC(xmlDoc, ref lowerCorner);

            Log.Information("file: " + path);

            Log.Information("ReadGeometryFromXml Methode... (speichert bldgs in Solid-List");

            var gmlBuildings = xmlDoc.Descendants(this.allns["bldg"] + "Building"); //alle Bldg-Elemente samt Kindern und Kindeskindern

            //Semantik:____________________________________________________________________

            //Anlegen der semantischen Attribute (ohne Werte):
            //zunächst Parsen der XML-Schema-Dateien für festgelegte (möglich vorkommende) Attribute, die im bldg- und core-Modul definiert sind
            //Auslesen nur einmal (!)

            var sem = new ReadSemData();

            //vorgegebene Semantik aus CityGML-Schema:

            XDocument xsdCore = XDocument.Load(folder + @"schemas\cityGMLBase.xsd");
            XDocument xsdBldgs = XDocument.Load(folder + @"schemas\building.xsd");
            XDocument xsdGen = XDocument.Load(folder + @"schemas\generics.xsd");

            string coreNsp = "";

            if(allns.ContainsKey("core"))
                coreNsp = "core";

            var coreAttr = sem.ReadSchemaAttributes(xsdCore, coreNsp, this.allns);
            var bldgAttr = sem.ReadSchemaAttributes(xsdBldgs, "bldg", this.allns);

            this.attributes.UnionWith(coreAttr);
            this.attributes.UnionWith(bldgAttr);

            //Anlegen der semantischen Attribute (ohne Werte):
            //Parsen der generischen Attribute
            //Auslesen pro Bldg

            var genAttr = sem.ReadGenericAttributes(gmlBuildings, this.allns["gen"]);

            this.attributes.UnionWith(genAttr);

            foreach (var attr in attributes)
            {
                Log.Information(attr.Name);
            }


            //Geometrie:____________________________________________________________________

            var geom = new ReadGeomData(this.allns);

            foreach(XElement xmlBuildingNode in gmlBuildings)
            {
                //List<Solid> buildings = CreateGeometry(doc);

                //Log.Debug("Buildings, Anzahl = " + buildings.Count);

                //--------------------------------

                //Auslesen der Attributwerte

                var bldgSem = sem.ReadAttributeValues(xmlBuildingNode, attributes, allns);

                //geom

                if(!globLC)
                {
                    var bldgLC = GetBuildingLC(xmlBuildingNode, ref lowerCorner);

                    if(!globLC && !bldgLC)
                        Log.Information("No global and local lower corner was found!");
                }

                var bldgGeom = geom.CollectBuilding(xmlBuildingNode, lowerCorner);

                this.solidList.Add(bldgGeom, bldgSem);
            }
        }
    }
}