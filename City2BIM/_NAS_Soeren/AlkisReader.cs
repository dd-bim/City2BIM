using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using City2BIM;
using City2BIM._NAS_Soeren;
using City2BIM.GetGeometry;
using City2BIM.RevitBuilder;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace NasImport
{
    //[TransactionAttribute(TransactionMode.Manual)]
    //[Regeneration(RegenerationOption.Manual)]
    //[Journaling(JournalingMode.NoCommandData)]
    public class AlkisReader

    {
        //ExternalCommandData commandData;
        Document doc;
        //UIDocument uiDoc;
        //private Transform transf = RevitGeorefSetter.TrafoPBP;
        //double feetToMeter = 1.0 / 0.3048;
        //double RE = 6380;       //mittlerer Erdradius in km


        //public class AxesFailure : IFailuresPreprocessor
        //{
        //    //Eventhandler, der eine ignorierbare Warnung, die nur auf einzelnen Geräten auftrat, überspringt.
        //    public FailureProcessingResult PreprocessFailures(
        //      FailuresAccessor a)
        //    {
        //        // inside event handler, get all warnings
        //        IList<FailureMessageAccessor> failures
        //          = a.GetFailureMessages();

        //        foreach (FailureMessageAccessor f in failures)
        //        {
        //            // check failure definition ids 
        //            // against ones to dismiss:

        //            FailureDefinitionId id
        //              = f.GetFailureDefinitionId();

        //            if (BuiltInFailures.InaccurateFailures.InaccurateSketchLine
        //              == id)
        //            {
        //                a.DeleteWarning(f);
        //            }
        //        }
        //        return FailureProcessingResult.Continue;
        //    }
        //}

        public ICollection<Element> SelectAllElements(UIDocument uidoc, Document doc)
        {
            FilteredElementCollector allTopos = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Topography);
            ICollection<Element> allToposList = allTopos.ToElements();
            return allToposList;
        }

        private double ConvertToRevit(string rawValue)
        {
            double val = double.Parse(rawValue, CultureInfo.InvariantCulture);
            double valUnproj = val / GeoRefSettings.ProjScale;
            double valUnprojFt = valUnproj * Prop.feetToM;

            return valUnprojFt;
        }

        private double[] ReadTopoGeom(string[] ssizeTopo)
        {
            double[] sizeTopo = new double[4];

            sizeTopo[0] = ConvertToRevit(ssizeTopo[0]);
            sizeTopo[1] = ConvertToRevit(ssizeTopo[1]);

            sizeTopo[2] = ConvertToRevit(ssizeTopo[2]);
            sizeTopo[3] = ConvertToRevit(ssizeTopo[3]);

            return sizeTopo;
        }

        public Dictionary<string, XNamespace> allns;

        private List<C2BPoint[]> ReadSegments(XElement surfaceExt)
        {
            List<C2BPoint[]> segments = new List<C2BPoint[]>();

            var posLists = surfaceExt.Descendants(allns["gml"] + "posList");

            foreach (XElement posList in posLists)
            {
                var line = ReadLineString(posList);

                segments.AddRange(line);
            }
            return segments;
        }

        private List<List<C2BPoint[]>> ReadInnerSegments(List<XElement> surfaceInt)
        {
            List<List<C2BPoint[]>> innerSegments = new List<List<C2BPoint[]>>();

            foreach (var interior in surfaceInt)
            {
                List<C2BPoint[]> segments = new List<C2BPoint[]>();

                var posLists = interior.Descendants(allns["gml"] + "posList");

                foreach (XElement posList in posLists)
                {
                    var line = ReadLineString(posList);

                    segments.AddRange(line);
                }
                innerSegments.Add(segments);
            }
            return innerSegments;
        }

        private List<C2BPoint[]> ReadLineString(XElement posList)
        {
            List<C2BPoint[]> segments = new List<C2BPoint[]>();

            var coords = posList.Value;
            string[] coord = coords.Split(' ');

            for (var c = 0; c < coord.Length - 3; c += 2)
            {
                C2BPoint start = new C2BPoint(double.Parse(coord[c], CultureInfo.InvariantCulture), double.Parse(coord[c + 1], CultureInfo.InvariantCulture), 0.0);
                C2BPoint end = new C2BPoint(double.Parse(coord[c + 2], CultureInfo.InvariantCulture), double.Parse(coord[c + 3], CultureInfo.InvariantCulture), 0.0);

                segments.Add(new C2BPoint[] { start, end });
            }
            return segments;
        }


        public List<string> parcelTypes = new List<string>
        {
            "AX_Flurstueck"
        };

        public List<string> buildingTypes = new List<string>
        {
            "AX_Gebaeude"
        };

        public List<string> usageTypes = new List<string>
        {
            //group "Siedlung"
            "AX_Wohnbauflaeche",
            "AX_IndustrieUndGewerbeflaeche",
            "AX_Halde",
            "AX_Bergbaubetrieb",
            "AX_TagebauGrubeSteinbruch",
            "AX_FlaecheGemischterNutzung",
            "AX_FlaecheBesondererFunktionalerPraegung",
            "AX_SportFreizeitUndErholungsflaeche",
            "AX_Friedhof",

            //group "Verkehr"
            "AX_Strassenverkehr",
            "AX_Weg",
            "AX_Platz",
            "AX_Bahnverkehr",
            "AX_Flugverkehr",
            "AX_Schiffsverkehr",

            //group "Vegetation"
            "AX_Landwirtschaft",
            "AX_Wald",
            "AX_Gehoelz",
            "AX_Heide",
            "AX_Moor",
            "AX_Sumpf",
            "AX_UnlandVegetationsloseFlaeche",

            //group "Gewaesser"
            "AX_Fliessgewaesser",
            "AX_Hafenbecken",
            "AX_StehendesGewaesser",
            "AX_Meer"
        };

        public AlkisReader(Document doc)
        {
            this.doc = doc;

            var import = new City2BIM.FileDialog();
            string path = import.ImportPath(City2BIM.FileDialog.Data.ALKIS);
            List<AX_Object> axObjects = new List<AX_Object>();

            XDocument xDoc = XDocument.Load(path);

            allns = xDoc.Root.Attributes().
                    Where(a => a.IsNamespaceDeclaration).
                    GroupBy(a => a.Name.Namespace == XNamespace.None ? String.Empty : a.Name.LocalName, a => XNamespace.Get(a.Value)).
                    ToDictionary(g => g.Key, g => g.First());

            //read all parcelTypes objects

            foreach (string axObject in parcelTypes)
            {
                var xmlObjType = xDoc.Descendants(allns[""] + axObject);

                foreach (XElement xmlObj in xmlObjType)
                {
                    AX_Object axObj = new AX_Object();
                    axObj.UsageType = axObject;

                    XElement extSeg = xmlObj.Descendants(allns["gml"] + "exterior").SingleOrDefault();
                    axObj.Segments = ReadSegments(extSeg);

                    List<XElement> intSeg = xmlObj.Descendants(allns["gml"] + "interior").ToList();
                    if (intSeg.Any())
                        axObj.InnerSegments = ReadInnerSegments(intSeg);

                    axObj.Group = AX_Object.AXGroup.parcel;
                    axObj.Attributes = new Alkis_Sem_Reader(xDoc, allns).ReadAttributeValuesParcel(xmlObj, Alkis_Semantic.GetParcelAttributes());

                    axObjects.Add(axObj);
                }
            }

            //---------------

            //read all buildingTypes objects

            foreach (string axObject in buildingTypes)
            {
                var xmlObjType = xDoc.Descendants(allns[""] + axObject);

                foreach (XElement xmlObj in xmlObjType)
                {
                    AX_Object axObj = new AX_Object();
                    axObj.UsageType = axObject;

                    XElement extSeg = xmlObj.Descendants(allns["gml"] + "exterior").SingleOrDefault();
                    axObj.Segments = ReadSegments(extSeg);

                    List<XElement> intSeg = xmlObj.Descendants(allns["gml"] + "interior").ToList();
                    if (intSeg.Any())
                        axObj.InnerSegments = ReadInnerSegments(intSeg);

                    axObj.Group = AX_Object.AXGroup.building;

                    axObjects.Add(axObj);
                }
            }

            //---------------

            //read all usageTypes objects

            foreach (string axObject in usageTypes)
            {
                var xmlObjType = xDoc.Descendants(allns[""] + axObject);

                foreach (XElement xmlObj in xmlObjType)
                {
                    AX_Object axObj = new AX_Object();
                    axObj.UsageType = axObject;

                    XElement extSeg = xmlObj.Descendants(allns["gml"] + "exterior").SingleOrDefault();
                    axObj.Segments = ReadSegments(extSeg);

                    List<XElement> intSeg = xmlObj.Descendants(allns["gml"] + "interior").ToList();
                    if (intSeg.Any())
                        axObj.InnerSegments = ReadInnerSegments(intSeg);

                    axObj.Group = AX_Object.AXGroup.usage;

                    axObjects.Add(axObj);
                }
            }

            //---------------

            var semBuilder = new RevitSemanticBuilder(doc);
            semBuilder.CreateParameters(Alkis_Semantic.GetParcelAttributes(), City2BIM.FileDialog.Data.ALKIS);

            var geomBuilder = new RevitAlkisBuilder(doc);
            geomBuilder.CreateTopo(axObjects);






            ////UIApplication uiapp = commandData.Application;
            ////UIDocument uidoc = uiapp.ActiveUIDocument;
            ////Document doc = uidoc.Document;
            //Autodesk.Revit.ApplicationServices.Application app = doc.Application;

            ////TO DO: Use XDocument!

            //XmlDocument xmlDoc = new XmlDocument();
            //xmlDoc.Load(path);



            //#region sitesubregion exterior

            //Category category = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Topography);
            //CategorySet categorySet = app.Create.NewCategorySet();
            //categorySet.Insert(category);

            //string spFile = ofdParam.FileName;
            //DefinitionFile sharedParameterFile = app.OpenSharedParameterFile();

            ////die Transaction tCreateSpFile erstellt die für den Parameterimport nach Revit erforderliche "Shared Parameter Datei". Dabei ist eine bestehende Datei notwendig 
            ////(erstellt über Revit, Gruppe Verwalten, Gemeinsam genutzte Parameter). Die Datei kann entweder leer sein (heißt: nur der durch Revit erstellte "Tabellenkopf"), oder ebenso gefüllt.
            ////In diesem Fall ergänzt die Transaktion die Datei ggf. um die fehlenden Parameter
            //Transaction tCreateSpFile = new Transaction(doc, "Create Shared Parameter File");
            //{
            //    tCreateSpFile.Start();

            //    try
            //    {
            //        app.SharedParametersFilename = spFile;
            //    }
            //    catch (Exception)
            //    {
            //        MessageBox.Show("No Shared Parameter File found");
            //    }

            //    DefinitionGroup dgSp = sharedParameterFile.Groups.get_Item("Flurstuecksdaten");

            //    ExternalDefinitionCreationOptions gemeindeOpt = new ExternalDefinitionCreationOptions("Gemeinde", ParameterType.Text);
            //    ExternalDefinitionCreationOptions gemarkNummerOpt = new ExternalDefinitionCreationOptions("Gemarkungsnummer", ParameterType.Text);
            //    ExternalDefinitionCreationOptions gemarkSchlüsselOpt = new ExternalDefinitionCreationOptions("Gemarkungsschlüssel", ParameterType.Text);
            //    ExternalDefinitionCreationOptions flstNummerOpt = new ExternalDefinitionCreationOptions("Flurstücksnummer", ParameterType.Text);
            //    ExternalDefinitionCreationOptions kreisOpt = new ExternalDefinitionCreationOptions("Kreis", ParameterType.Text);
            //    ExternalDefinitionCreationOptions landOpt = new ExternalDefinitionCreationOptions("Land", ParameterType.Text);
            //    ExternalDefinitionCreationOptions zeitpktDerEntstehOpt = new ExternalDefinitionCreationOptions("Zeitpunkt der Entstehung", ParameterType.Text);
            //    ExternalDefinitionCreationOptions dienstStelleOpt = new ExternalDefinitionCreationOptions("Dienststelle", ParameterType.Text);
            //    ExternalDefinitionCreationOptions flurNummerOpt = new ExternalDefinitionCreationOptions("Flurnummer", ParameterType.Text);
            //    ExternalDefinitionCreationOptions flurstücksKennzOpt = new ExternalDefinitionCreationOptions("Flurstückskennzeichen", ParameterType.Text);
            //    ExternalDefinitionCreationOptions regierBezirkOpt = new ExternalDefinitionCreationOptions("Regierungsbezirk", ParameterType.Text);
            //    ExternalDefinitionCreationOptions amtFlaecheOpt = new ExternalDefinitionCreationOptions("Amtliche Fläche", ParameterType.Text);
            //    ExternalDefinitionCreationOptions nachnameOderFirmaOpt = new ExternalDefinitionCreationOptions("Nachname oder Firma", ParameterType.Text);
            //    ExternalDefinitionCreationOptions vornameOpt = new ExternalDefinitionCreationOptions("Vorname", ParameterType.Text);
            //    ExternalDefinitionCreationOptions ortOpt = new ExternalDefinitionCreationOptions("Ort", ParameterType.Text);
            //    ExternalDefinitionCreationOptions plzOpt = new ExternalDefinitionCreationOptions("PLZ", ParameterType.Text);
            //    ExternalDefinitionCreationOptions strasseOpt = new ExternalDefinitionCreationOptions("Strasse", ParameterType.Text);
            //    ExternalDefinitionCreationOptions hausnummerOpt = new ExternalDefinitionCreationOptions("Hausnummer", ParameterType.Text);

            //    Definition gemeindeDefinition = default(Definition);
            //    Definition gemarkNummerDefinition = default(Definition);
            //    Definition gemarkSchlüsselDefinition = default(Definition);
            //    Definition flstNummerDefinition = default(Definition);
            //    Definition kreisDefinition = default(Definition);
            //    Definition landDefinition = default(Definition);
            //    Definition zeitpktDerEntstehDefinition = default(Definition);
            //    Definition dienstStelleDefinition = default(Definition);
            //    Definition flurNummerDefinition = default(Definition);
            //    Definition flurstücksKennzDefinition = default(Definition);
            //    Definition regierBezirDefinition = default(Definition);
            //    Definition amtFlaecheDefinition = default(Definition);
            //    Definition nachnameOderFirmaDefinition = default(Definition);
            //    Definition vornameDefinition = default(Definition);
            //    Definition ortDefinition = default(Definition);
            //    Definition plzDefinition = default(Definition);
            //    Definition strasseDefinition = default(Definition);
            //    Definition hausnummerDefinition = default(Definition);

            //    if (dgSp == null)
            //    {
            //        dgSp = sharedParameterFile.Groups.Create("Flurstuecksdaten");
            //        gemeindeDefinition = dgSp.Definitions.Create(gemeindeOpt);
            //        gemarkNummerDefinition = dgSp.Definitions.Create(gemarkNummerOpt);
            //        gemarkSchlüsselDefinition = dgSp.Definitions.Create(gemarkSchlüsselOpt);
            //        flstNummerDefinition = dgSp.Definitions.Create(flstNummerOpt);
            //        kreisDefinition = dgSp.Definitions.Create(kreisOpt);
            //        landDefinition = dgSp.Definitions.Create(landOpt);
            //        zeitpktDerEntstehDefinition = dgSp.Definitions.Create(zeitpktDerEntstehOpt);
            //        dienstStelleDefinition = dgSp.Definitions.Create(dienstStelleOpt);
            //        flurNummerDefinition = dgSp.Definitions.Create(flurNummerOpt);
            //        flurstücksKennzDefinition = dgSp.Definitions.Create(flurstücksKennzOpt);
            //        regierBezirDefinition = dgSp.Definitions.Create(regierBezirkOpt);
            //        amtFlaecheDefinition = dgSp.Definitions.Create(amtFlaecheOpt);
            //        nachnameOderFirmaDefinition = dgSp.Definitions.Create(nachnameOderFirmaOpt);
            //        vornameDefinition = dgSp.Definitions.Create(vornameOpt);
            //        ortDefinition = dgSp.Definitions.Create(ortOpt);
            //        plzDefinition = dgSp.Definitions.Create(plzOpt);
            //        strasseDefinition = dgSp.Definitions.Create(strasseOpt);
            //        hausnummerDefinition = dgSp.Definitions.Create(hausnummerOpt);
            //    }

            //    else if (dgSp != null)
            //    {
            //        if (gemeindeDefinition != null)
            //        {

            //        }
            //        else if (gemeindeDefinition == null)
            //        {
            //            gemeindeDefinition = dgSp.Definitions.get_Item("Gemeinde");
            //        }
            //        if (gemarkNummerDefinition != null)
            //        {

            //        }
            //        else if (gemarkNummerDefinition == null)
            //        {
            //            gemarkNummerDefinition = dgSp.Definitions.get_Item("Gemarkungsnummer");
            //        }
            //        if (gemarkSchlüsselDefinition != null)
            //        {

            //        }
            //        else if (gemarkSchlüsselDefinition == null)
            //        {
            //            gemarkSchlüsselDefinition = dgSp.Definitions.get_Item("Gemarkungsschlüssel");
            //        }
            //        if (flstNummerDefinition != null)
            //        {

            //        }
            //        else if (flstNummerDefinition == null)
            //        {
            //            flstNummerDefinition = dgSp.Definitions.get_Item("Flurstücksnummer");
            //        }
            //        if (kreisDefinition != null)
            //        {

            //        }
            //        else if (kreisDefinition == null)
            //        {
            //            kreisDefinition = dgSp.Definitions.get_Item("Kreis");
            //        }
            //        if (landDefinition != null)
            //        {

            //        }
            //        else if (landDefinition == null)
            //        {
            //            landDefinition = dgSp.Definitions.get_Item("Land");
            //        }
            //        if (zeitpktDerEntstehDefinition != null)
            //        {

            //        }
            //        else if (zeitpktDerEntstehDefinition == null)
            //        {
            //            zeitpktDerEntstehDefinition = dgSp.Definitions.get_Item("Zeitpunkt der Entstehung");
            //        }
            //        if (dienstStelleDefinition != null)
            //        {

            //        }
            //        else if (dienstStelleDefinition == null)
            //        {
            //            dienstStelleDefinition = dgSp.Definitions.get_Item("Dienststelle");
            //        }
            //        if (flurNummerDefinition != null)
            //        {

            //        }
            //        else if (flurNummerDefinition == null)
            //        {
            //            flurNummerDefinition = dgSp.Definitions.get_Item("Flurnummer");
            //        }
            //        if (flurstücksKennzDefinition != null)
            //        {

            //        }
            //        else if (flurstücksKennzDefinition == null)
            //        {
            //            flurstücksKennzDefinition = dgSp.Definitions.get_Item("Flurstückskennzeichen");
            //        }
            //        if (regierBezirDefinition != null)
            //        {

            //        }
            //        else if (regierBezirDefinition == null)
            //        {
            //            regierBezirDefinition = dgSp.Definitions.get_Item("Regierungsbezirk");
            //        }
            //        if (amtFlaecheDefinition != null)
            //        {

            //        }
            //        else if (amtFlaecheDefinition == null)
            //        {
            //            amtFlaecheDefinition = dgSp.Definitions.get_Item("Amtliche Fläche");
            //        }
            //        if (nachnameOderFirmaDefinition != null)
            //        {

            //        }
            //        else if (nachnameOderFirmaDefinition == null)
            //        {
            //            nachnameOderFirmaDefinition = dgSp.Definitions.get_Item("Nachname oder Firma");
            //        }
            //        if (vornameDefinition != null)
            //        {

            //        }
            //        else if (vornameDefinition == null)
            //        {
            //            vornameDefinition = dgSp.Definitions.get_Item("Vorname");
            //        }
            //        if (ortDefinition != null)
            //        {

            //        }
            //        else if (ortDefinition == null)
            //        {
            //            ortDefinition = dgSp.Definitions.get_Item("Ort");
            //        }
            //        if (plzDefinition != null)
            //        {

            //        }
            //        else if (plzDefinition == null)
            //        {
            //            plzDefinition = dgSp.Definitions.get_Item("PLZ");
            //        }
            //        if (strasseDefinition != null)
            //        {

            //        }
            //        else if (strasseDefinition == null)
            //        {
            //            strasseDefinition = dgSp.Definitions.get_Item("Strasse");
            //        }
            //        if (hausnummerDefinition != null)
            //        {

            //        }
            //        else if (hausnummerDefinition == null)
            //        {
            //            hausnummerDefinition = dgSp.Definitions.get_Item("Hausnummer");
            //        }
            //    }
            //}
            //tCreateSpFile.Commit();

            ////Für jede Gruppe im SP File (hier: eine Gruppe "Flurstuecksdaten") werden die Parameter ausgelesen und an die Topographien angebracht. 
            //foreach (DefinitionGroup dg in sharedParameterFile.Groups)
            //{
            //    if (dg.Name == "Flurstuecksdaten")
            //    {
            //        ExternalDefinition gemarkSchlüsselExtDef = dg.Definitions.get_Item("Gemarkungsschlüssel") as ExternalDefinition;
            //        ExternalDefinition gemarkNummerExtDef = dg.Definitions.get_Item("Gemarkungsnummer") as ExternalDefinition;
            //        ExternalDefinition flstNummerExtDef = dg.Definitions.get_Item("Flurstücksnummer") as ExternalDefinition;
            //        ExternalDefinition flstKennzExtDef = dg.Definitions.get_Item("Flurstückskennzeichen") as ExternalDefinition;
            //        ExternalDefinition AmtlFlaecheExtDef = dg.Definitions.get_Item("Amtliche Fläche") as ExternalDefinition;
            //        ExternalDefinition flurNummerExtDef = dg.Definitions.get_Item("Flurnummer") as ExternalDefinition;
            //        ExternalDefinition zeitpunktExtDef = dg.Definitions.get_Item("Zeitpunkt der Entstehung") as ExternalDefinition;
            //        ExternalDefinition landExtDef = dg.Definitions.get_Item("Land") as ExternalDefinition;
            //        ExternalDefinition regBezirkExtDef = dg.Definitions.get_Item("Regierungsbezirk") as ExternalDefinition;
            //        ExternalDefinition kreisExtDef = dg.Definitions.get_Item("Kreis") as ExternalDefinition;
            //        ExternalDefinition gemeindeExtDef = dg.Definitions.get_Item("Gemeinde") as ExternalDefinition;
            //        ExternalDefinition dienststelleExtDef = dg.Definitions.get_Item("Dienststelle") as ExternalDefinition;
            //        ExternalDefinition nachnameOderFirmaExtDef = dg.Definitions.get_Item("Nachname oder Firma") as ExternalDefinition;
            //        ExternalDefinition vornameExtDef = dg.Definitions.get_Item("Vorname") as ExternalDefinition;
            //        ExternalDefinition ortExtDef = dg.Definitions.get_Item("Ort") as ExternalDefinition;
            //        ExternalDefinition plzExtDef = dg.Definitions.get_Item("PLZ") as ExternalDefinition;
            //        ExternalDefinition strasseExtDef = dg.Definitions.get_Item("Strasse") as ExternalDefinition;
            //        ExternalDefinition hausnummerExtDef = dg.Definitions.get_Item("Hausnummer") as ExternalDefinition;

            //        Transaction tParam = new Transaction(doc, "Insert Parameter");
            //        {
            //            tParam.Start();
            //            InstanceBinding newIB = app.Create.NewInstanceBinding(categorySet);
            //            doc.ParameterBindings.Insert(gemarkSchlüsselExtDef, newIB, BuiltInParameterGroup.PG_DATA);
            //            doc.ParameterBindings.Insert(gemarkNummerExtDef, newIB, BuiltInParameterGroup.PG_DATA);
            //            doc.ParameterBindings.Insert(flstNummerExtDef, newIB, BuiltInParameterGroup.PG_DATA);
            //            doc.ParameterBindings.Insert(flstKennzExtDef, newIB, BuiltInParameterGroup.PG_DATA);
            //            doc.ParameterBindings.Insert(AmtlFlaecheExtDef, newIB, BuiltInParameterGroup.PG_DATA);
            //            doc.ParameterBindings.Insert(flurNummerExtDef, newIB, BuiltInParameterGroup.PG_DATA);
            //            doc.ParameterBindings.Insert(zeitpunktExtDef, newIB, BuiltInParameterGroup.PG_DATA);
            //            doc.ParameterBindings.Insert(landExtDef, newIB, BuiltInParameterGroup.PG_DATA);
            //            doc.ParameterBindings.Insert(regBezirkExtDef, newIB, BuiltInParameterGroup.PG_DATA);
            //            doc.ParameterBindings.Insert(kreisExtDef, newIB, BuiltInParameterGroup.PG_DATA);
            //            doc.ParameterBindings.Insert(gemeindeExtDef, newIB, BuiltInParameterGroup.PG_DATA);
            //            doc.ParameterBindings.Insert(dienststelleExtDef, newIB, BuiltInParameterGroup.PG_DATA);
            //            doc.ParameterBindings.Insert(nachnameOderFirmaExtDef, newIB, BuiltInParameterGroup.PG_DATA);
            //            doc.ParameterBindings.Insert(vornameExtDef, newIB, BuiltInParameterGroup.PG_DATA);
            //            doc.ParameterBindings.Insert(ortExtDef, newIB, BuiltInParameterGroup.PG_DATA);
            //            doc.ParameterBindings.Insert(plzExtDef, newIB, BuiltInParameterGroup.PG_DATA);
            //            doc.ParameterBindings.Insert(strasseExtDef, newIB, BuiltInParameterGroup.PG_DATA);
            //            doc.ParameterBindings.Insert(hausnummerExtDef, newIB, BuiltInParameterGroup.PG_DATA);

            //        }
            //        tParam.Commit();

            //        int iEigent = 0;
            //        foreach (XmlNode flstnodeExt in flst_list)
            //        {
            //            XmlNodeList pos_nachIdExt = flstnodeExt.SelectNodes("ns2:position/gml:Surface/gml:patches/gml:PolygonPatch/gml:exterior/gml:Ring/gml:curveMember/gml:Curve/gml:segments/gml:LineStringSegment/gml:posList", nsmgr);

            //            List<XmlNode> leerList = new List<XmlNode>();
            //            List<string> innerTextList = new List<string>();




            //            leerList.Add(gemarkSchl);
            //            leerList.Add(gemarkNumm);
            //            leerList.Add(flstNummZ);
            //            leerList.Add(flstNummN);
            //            leerList.Add(flstKennz);
            //            leerList.Add(amtlFlae);
            //            leerList.Add(flNr);
            //            leerList.Add(zDE);
            //            leerList.Add(land);
            //            leerList.Add(regBez);
            //            leerList.Add(kreis);
            //            leerList.Add(gemeinde);
            //            leerList.Add(dienStL);
            //            leerList.Add(dienStS);
            //            leerList.Add(personNachnameOderFirma);
            //            leerList.Add(vorname);
            //            leerList.Add(ort);
            //            leerList.Add(plz);
            //            leerList.Add(strasse);
            //            leerList.Add(hausnummer);

            //            for (int x = 0; x < leerList.Count; x++)
            //            {
            //                try
            //                {
            //                    innerTextList.Add(leerList[x].InnerText);
            //                }
            //                catch
            //                {
            //                    innerTextList.Add("-");
            //                }
            //            }

            //            //Knoten für die über xlink verknüpften Daten, wie z.B. Eigentümerdaten

            //            //Flurstück auslesen: ID für Buchungsblatt
            //            XmlNodeList flstIstGebuchtList = (xmlDoc.SelectNodes("//ns2:AX_Flurstueck/ns2:istGebucht", nsmgr));

            //            string flstIstGebuchtHrefId = flstIstGebuchtList[iEigent].Attributes["xlink:href"].Value;

            //            //Liest pro Flurstück den HRef Wert für "istGebucht" aus
            //            //Console.WriteLine("flstIstGebuchtHrefId: " + flstIstGebuchtHrefId); 
            //            string flstIstGebuchtId = flstIstGebuchtHrefId.Substring(flstIstGebuchtHrefId.Length - 16);


            //            //Buchungsblatt auslesen: ID für Buchungsstelle
            //            var buchungsStelleZuFlst = xmlDoc.SelectSingleNode("//ns2:AX_Buchungsstelle[@gml:id='" + flstIstGebuchtId + "']", nsmgr);
            //            var buchungsstelleIBVHrefId2 = buchungsStelleZuFlst["istBestandteilVon"].Attributes["xlink:href"].Value;
            //            //urn:adv:oid:DEBBAL0600000WY6
            //            string buchungsstelleIBVHrefId2M16 = buchungsstelleIBVHrefId2.Substring(buchungsstelleIBVHrefId2.Length - 16);

            //            //Buchungsblatt suchen, auf welches sich die Buchungsstelle mit ihrer HrefId bezieht
            //            var buchungsblattZuBuchungsstelle = xmlDoc.SelectSingleNode("//ns2:AX_Buchungsblatt[@gml:id='" + buchungsstelleIBVHrefId2M16 + "']", nsmgr);

            //            //Namensnummer suchen, die bestandteil der Buchungsstelle ist
            //            var namensnummerZuBuchungsstelle = xmlDoc.SelectSingleNode("//ns2:AX_Namensnummer/ns2:istBestandteilVon[@xlink:href='" + buchungsstelleIBVHrefId2 + "']", nsmgr);
            //            string personNachnameOderFirmaString = default(string);
            //            string vornameString = default(string);
            //            XmlNode personZuNamensnummer = default(XmlNode);
            //            string ortString = default(string);
            //            string plzString = default(string);
            //            string strasseString = default(string);
            //            string hausnummerString = default(string);

            //            if (namensnummerZuBuchungsstelle == null)
            //            {
            //                personNachnameOderFirma = null;
            //                personNachnameOderFirmaString = "-";
            //                vornameString = "-";
            //            }
            //            else if (namensnummerZuBuchungsstelle != null)
            //            {
            //                //"benennt"-HrefId suchen, um Person zu finden
            //                var namensnummerBenenntHrefId = namensnummerZuBuchungsstelle.ParentNode["benennt"].Attributes["xlink:href"].Value;
            //                string namensnummerBenenntHrefIdM16 = namensnummerBenenntHrefId.Substring(namensnummerBenenntHrefId.Length - 16);

            //                //Person suchen, die zur "benennt" -href passt
            //                personZuNamensnummer = xmlDoc.SelectSingleNode("//ns2:AX_Person[@gml:id='" + namensnummerBenenntHrefIdM16 + "']", nsmgr);
            //                personNachnameOderFirma = personZuNamensnummer["nachnameOderFirma"];
            //                personNachnameOderFirmaString = personNachnameOderFirma.InnerText;
            //                if (personZuNamensnummer.ChildNodes[6].Name == "vorname")
            //                {
            //                    vorname = personZuNamensnummer["vorname"];
            //                    vornameString = vorname.InnerText;
            //                }
            //                else
            //                {
            //                    //MessageBox.Show("nein");
            //                    vornameString = "-";
            //                }
            //            }

            //            var personHatHrefId = default(string);
            //            var personHatHrefIdM16 = default(string);

            //            if (personZuNamensnummer == null)
            //            {

            //            }
            //            else
            //            {
            //                if (personZuNamensnummer["hat"] == null)
            //                {
            //                    personHatHrefId = "-";
            //                }
            //                else
            //                {
            //                    personHatHrefId = personZuNamensnummer["hat"].Attributes["xlink:href"].Value;

            //                }
            //                personHatHrefId = personZuNamensnummer["hat"].Attributes["xlink:href"].Value;
            //                personHatHrefIdM16 = personHatHrefId.Substring(personHatHrefId.Length - 16);
            //            }

            //            XmlNode anschriftZuPerson = xmlDoc.SelectSingleNode("//ns2:AX_Anschrift[@gml:id='" + personHatHrefIdM16 + "']", nsmgr);
            //            if (anschriftZuPerson == null)
            //            {
            //            }
            //            else
            //            {

            //                if (anschriftZuPerson["ort_Post"] == null)
            //                {
            //                    ortString = "-";
            //                }
            //                else
            //                {
            //                    ort = anschriftZuPerson["ort_Post"];
            //                    ortString = ort.InnerText;
            //                }
            //                if (anschriftZuPerson["postleitzahlPostzustellung"] == null)
            //                {
            //                    plzString = "-";
            //                }
            //                else
            //                {
            //                    plz = anschriftZuPerson["postleitzahlPostzustellung"];
            //                    plzString = plz.InnerText;
            //                }
            //                if (anschriftZuPerson["strasse"] == null)
            //                {
            //                    strasseString = "-";
            //                }
            //                else
            //                {
            //                    strasse = anschriftZuPerson["strasse"];
            //                    strasseString = strasse.InnerText;
            //                }
            //                if (anschriftZuPerson["hausnummer"] == null)
            //                {
            //                    hausnummerString = "-";
            //                }
            //                else
            //                {
            //                    hausnummer = anschriftZuPerson["hausnummer"];
            //                    hausnummerString = hausnummer.InnerText;
            //                }
            //            }
            //            iEigent++;

            //            #endregion Knoten Parameter

            //            List<string> listAreaExt = new List<String>();

            //             List<CurveLoop> cLoopListExt = new List<CurveLoop>();
            //            CurveLoop cLoopLineExt = new CurveLoop();

            //            if (cLoopLineExt.GetExactLength() > 0)
            //            {
            //                cLoopListExt.Add(cLoopLineExt);
            //            }

            //            if (cLoopListExt.Count() > 0)
            //            {
            //                Transaction tExtFlst = new Transaction(doc, "Create Exterior");
            //                {
            //                    FailureHandlingOptions options = tExtFlst.GetFailureHandlingOptions();
            //                    options.SetFailuresPreprocessor(new AxesFailure());
            //                    tExtFlst.SetFailureHandlingOptions(options);

            //                    tExtFlst.Start();
            //                    SketchPlane sketch = SketchPlane.Create(doc, geomPlane);
            //                    SiteSubRegion siteSubRegion = SiteSubRegion.Create(doc, cLoopListExt, elementIdFlst);
            //                }
            //                tExtFlst.Commit();

            //                ICollection<Element> eleColle = SelectAllElements(uiDoc, doc);

            //                foreach (Element el in eleColle)
            //                {
            //                    #region Parameter
            //                    Parameter parameter0 = el.LookupParameter("Kommentare");
            //                    Parameter parameter1 = el.LookupParameter("Gemarkungsschlüssel");
            //                    Parameter parameter2 = el.LookupParameter("Gemarkungsnummer");
            //                    Parameter parameter3 = el.LookupParameter("Flurstücksnummer");
            //                    Parameter parameter4 = el.LookupParameter("Flurstückskennzeichen");
            //                    Parameter parameter5 = el.LookupParameter("Amtliche Fläche");
            //                    Parameter parameter6 = el.LookupParameter("Flurnummer");
            //                    Parameter parameter7 = el.LookupParameter("Zeitpunkt der Entstehung");
            //                    Parameter parameter8 = el.LookupParameter("Land");
            //                    Parameter parameter9 = el.LookupParameter("Regierungsbezirk");
            //                    Parameter parameter10 = el.LookupParameter("Kreis");
            //                    Parameter parameter11 = el.LookupParameter("Gemeinde");
            //                    Parameter parameter12 = el.LookupParameter("Dienststelle");
            //                    Parameter parameter13 = el.LookupParameter("Nachname oder Firma");
            //                    Parameter parameter14 = el.LookupParameter("Vorname");
            //                    Parameter parameter15 = el.LookupParameter("Ort");
            //                    Parameter parameter16 = el.LookupParameter("PLZ");
            //                    Parameter parameter17 = el.LookupParameter("Strasse");
            //                    Parameter parameter18 = el.LookupParameter("Hausnummer");




            //                    #endregion parameter

            //                    using (Transaction t = new Transaction(doc, "parameter"))
            //                    {
            //                        t.Start("Parameterwerte hinzufügen");
            //                        try
            //                        {
            //                            if (parameter0.HasValue.Equals(false))
            //                            {
            //                                parameter0.Set("Exterior-Flaeche");
            //                                if (parameter1.HasValue.Equals(false))
            //                                {
            //                                    for (int y = 0; y < innerTextList.Count(); y++)
            //                                    {
            //                                        parameter1.Set(innerTextList[0]);
            //                                        parameter2.Set(innerTextList[1]);
            //                                        if (flstNummN != null)
            //                                        {
            //                                            parameter3.Set(innerTextList[2] + "/" + innerTextList[3]);
            //                                        }
            //                                        else if (flstNummN == null)
            //                                        {
            //                                            parameter3.Set(innerTextList[2]);
            //                                        }

            //                                        parameter4.Set(innerTextList[4]);
            //                                        parameter5.Set(innerTextList[5]);
            //                                        parameter6.Set(innerTextList[6]);
            //                                        parameter7.Set(innerTextList[7]);
            //                                        parameter8.Set(innerTextList[8]);
            //                                        parameter9.Set(innerTextList[9]);
            //                                        parameter10.Set(innerTextList[10]);
            //                                        parameter11.Set(innerTextList[10]);
            //                                        parameter12.Set(innerTextList[12] + "/" + innerTextList[13]);
            //                                        parameter13.Set(innerTextList[14]);
            //                                        parameter14.Set(innerTextList[15]);
            //                                        parameter15.Set(innerTextList[16]);
            //                                        parameter16.Set(innerTextList[17]);
            //                                        parameter17.Set(innerTextList[18]);
            //                                        parameter18.Set(innerTextList[19]);

            //                                    }
            //                                }
            //                            }

            //                            else if (parameter1.HasValue.Equals(true))
            //                            {

            //                            }
            //                        }
            //                        catch { }
            //                        t.Commit();
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}
            //#endregion sitesubregion exterior
        }
    }
}
