using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using City2BIM.GetSemantics;
using City2BIM.GmlRep;
using City2BIM.Logging;
using City2BIM.RevitBuilder;
using City2BIM.RevitCommands.City2BIM;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using static City2BIM.LogWriter;
using C2BPoint = City2BIM.GetGeometry.C2BPoint;
using C2BSolid = City2BIM.GetGeometry.C2BSolid;
using XmlAttribute = City2BIM.GetSemantics.XmlAttribute;

namespace City2BIM
{
    public class ReadCityGML
    {
        private C2BPoint lowerCornerPt;
        private Dictionary<string, XNamespace> allns;
        private HashSet<XmlAttribute> attributes = new HashSet<XmlAttribute>();

        // The main Execute method (inherited from IExternalCommand) must be public
        public ReadCityGML(Document doc, bool solid)
        {
            string path = "";

            XDocument gmlDoc;

            if (City2BIM_prop.IsServerRequest)
            {
                //server url as specified (if no change VCS server will be called)
                string wfsUrl = City2BIM_prop.ServerUrl;

                //to ensure correct coordinate order (VCS response is always YXZ order)
                City2BIM_prop.IsGeodeticSystem = true;

                //client class for xml-POST request from WFS server
                WFS.WFSClient client = new WFS.WFSClient(wfsUrl);

                //response with parameters: Site-Lon, Site-Lat, extent, max response of bldgs, CRS)
                //Site coordinates from Revit.SiteLocation
                //extent from used-defined def (default: 300 m)
                //max response dependent of server settings (at VCS), currently 500
                //CRS:  supported from server are currently: Pseudo-Mercator (3857), LatLon (4326), German National Systems: West(25832), East(25833)
                //      supported by PlugIn are only the both German National Systems

                if (GeoRefSettings.Epsg != "EPSG:25832" && GeoRefSettings.Epsg != "EPSG:25833")
                    TaskDialog.Show("EPSG not supported!", "Only EPSG:25832 or EPSG:25833 will be supported by server. Please change the EPSG-Code in Georeferencing window.");

                gmlDoc = client.getFeaturesCircle(City2BIM_prop.ServerCoord[0], City2BIM_prop.ServerCoord[1], City2BIM_prop.Extent, 500, GeoRefSettings.Epsg);

                if (City2BIM_prop.SaveServerResponse)
                {
                    gmlDoc.Save(City2BIM_prop.PathResponse + "\\" + Math.Round(City2BIM_prop.ServerCoord[1], 4) + "_" + Math.Round(City2BIM_prop.ServerCoord[0], 4) + ".gml");
                }
            }
            else
            {
                //local file path
                path = City2BIM_prop.FileUrl;

                //Load XML document from local file
                gmlDoc = XDocument.Load(path);

            }

            #region Namespaces

            //Save all namespaces in Dictionary with shortenings
            this.allns = gmlDoc.Root.Attributes().
                Where(a => a.IsNamespaceDeclaration).
                GroupBy(a => a.Name.Namespace == XNamespace.None ? String.Empty : a.Name.LocalName, a => XNamespace.Get(a.Value)).
                ToDictionary(g => g.Key, g => g.First());

            //special case:
            if (this.allns.ContainsKey(""))
            {
                if (!this.allns.ContainsKey("core"))
                    this.allns.Add("core", this.allns[""]);     //if namespace has no shortener --> core namespace is used
            }

            #endregion Namespaces

            bool sameXY = false, sameHeight = false, swapNE = false;
            CheckInputCRS(doc, gmlDoc, ref sameXY, ref sameHeight, ref swapNE);

            bool continueImport = true;

            if (!sameXY || !sameHeight)
            {
                TaskDialog crsDialog = new TaskDialog("Warning");
                crsDialog.AllowCancellation = true;

                crsDialog.MainInstruction = "Different CRS in input file and Georef settings detected!";

                string messageXY = "There are different CRS for the XY-plane.";
                string messageHeight = "There are different CRS for the Elevation";
                string messageDecision = "Press OK to ignore and continue Import or cancel for checking Georef settings.";

                if (!sameXY && !sameHeight)
                    crsDialog.MainContent = messageXY + "\r\n" + messageHeight + "\r\n" + messageDecision;

                if (!sameXY && sameHeight)
                    crsDialog.MainContent = messageXY + "\r\n" + messageDecision;

                if (sameXY && !sameHeight)
                    crsDialog.MainContent = messageHeight + "\r\n" + messageDecision;

                crsDialog.CommonButtons = TaskDialogCommonButtons.Ok | TaskDialogCommonButtons.Cancel;

                var result = crsDialog.Show();

                if (result == TaskDialogResult.Cancel)
                    continueImport = false;
            }

            if (continueImport)
            {
                var gmlBuildings = ReadGmlData(gmlDoc);

                if (solid)
                {
                    //Creation of Solids
                    gmlBuildings = CalculateSolids(gmlBuildings);
                }
                //erstellt Revit-seitig die Geometrie und ordnet Attributwerte zu (Achtung: ReadXMLDoc muss vorher ausgeführt werden)
                RevitGeometryBuilder cityModel = new RevitGeometryBuilder(doc, gmlBuildings, this.lowerCornerPt/*, swapNE*/);

                //Parameter für Revit-Kategorie erstellen
                //nach ausgewählter Methode (Solids oder Flächen) Parameter an zugehörige Kategorien übergeben

                // TO DO: Logik für Parameter File -Auswahl oder Location

                //erstellt Revit-seitig die Attribute (Achtung: ReadXMLDoc muss vorher ausgeführt werden)
                RevitSemanticBuilder citySem = new RevitSemanticBuilder(doc); //Übergabe der Methoden-Rückgaben zum Schreiben nach Revit

                citySem.CreateParameters(attributes, FileDialog.Data.CityGML); //erstellt Shared Parameters für Kategorie Umgebung

                if (solid)
                {
                    cityModel.CreateBuildings(); //erstellt DirectShape-Geometrie als Kategorie Umgebung
                }
                else
                {
                    cityModel.CreateBuildingsWithFaces(); //erstellt DirectShape-Geometrien der jeweiligen Kategorie
                }
            }
        }

        /// <summary>
        /// Compares georef settings and input crs from gml-file
        /// </summary>
        /// <param name="revDoc">Active Revit document</param>
        /// <param name="gmlDoc">Imported GML document</param>
        /// <param name="sameCRSxy">ref for decision of same XY-crs</param>
        /// <param name="sameCRSh">ref for decision of same height-crs</param>
        /// <param name="swapNE">ref for decision of needed swap of N and E (some EPSG have swapped order of N and E)</param>
        private void CheckInputCRS(Document revDoc, XDocument gmlDoc, ref bool sameCRSxy, ref bool sameCRSh, ref bool swapNE)
        {
            var envelope = gmlDoc.Descendants(this.allns["gml"] + "Envelope").FirstOrDefault();

            var attr = envelope.Attributes();
            var srsName = envelope.Attribute("srsName");
            string gmlCRS = srsName.Value;

            var geoInfo = revDoc.ProjectInformation;

            #region XY comparison

            string inputCRS = "";
            string existCRS = "";
            string revCRSxy = "";

            var epsg = geoInfo.LookupParameter("CRS Name");
            if (epsg != null)
                revCRSxy = epsg.AsString();

            #region Input File - ADV-srsName (no EPSG)

            if (gmlCRS.Contains("ETRS_89") ||
                gmlCRS.Contains("ETRS89") ||
                gmlCRS.Contains("ETRS_1989") ||
                gmlCRS.Contains("ETRS1989"))
            {
                if (gmlCRS.Contains("UTM") && gmlCRS.Contains("31"))
                    inputCRS = "UTM31";

                if (gmlCRS.Contains("UTM") && gmlCRS.Contains("32"))
                    inputCRS = "UTM32";

                if (gmlCRS.Contains("UTM") && gmlCRS.Contains("33"))
                    inputCRS = "UTM33";
            }

            #endregion Input File - ADV-srsName (no EPSG)

            #region Input File - EPSG-srsName

            if (gmlCRS.Contains("EPSG"))
            {
                if (gmlCRS.Contains("25831") ||
                    gmlCRS.Contains("3043") ||
                    gmlCRS.Contains("5649") ||
                    gmlCRS.Contains("5651") ||
                    gmlCRS.Contains("5554"))
                { inputCRS = "UTM31"; }

                if (gmlCRS.Contains("25832") ||
                    gmlCRS.Contains("3044") ||
                    gmlCRS.Contains("4647") ||
                    gmlCRS.Contains("5652") ||
                    gmlCRS.Contains("5555"))
                { inputCRS = "UTM32"; }

                if (gmlCRS.Contains("25833") ||
                    gmlCRS.Contains("3045") ||
                    gmlCRS.Contains("5650") ||
                    gmlCRS.Contains("5653") ||
                    gmlCRS.Contains("5556"))
                { inputCRS = "UTM33"; }
            }

            #endregion Input File - EPSG-srsName

            #region Revit-Settings

            if (revCRSxy.Equals("EPSG:25831") ||
                revCRSxy.Equals("EPSG:3043") ||
                revCRSxy.Equals("EPSG:5649") ||
                revCRSxy.Equals("EPSG:5651") ||
                revCRSxy.Equals("EPSG:5554"))
            {
                existCRS = "UTM31";
            }

            if (revCRSxy.Equals("EPSG:25832") ||
                revCRSxy.Equals("EPSG:3044") ||
                revCRSxy.Equals("EPSG:4647") ||
                revCRSxy.Equals("EPSG:5652") ||
                revCRSxy.Equals("EPSG:5555"))
            {
                existCRS = "UTM32";
            }

            if (revCRSxy.Equals("EPSG:25833") ||
                revCRSxy.Equals("EPSG:3045") ||
                revCRSxy.Equals("EPSG:5650") ||
                revCRSxy.Equals("EPSG:5653") ||
                revCRSxy.Equals("EPSG:5556"))
            {
                existCRS = "UTM33";
            }

            #endregion Revit-Settings

            if (inputCRS.Equals(existCRS))
                sameCRSxy = true;

            #endregion XY comparison

            #region Height comparison

            string inputVert = "";
            string existVert = "";

            var vertD = geoInfo.LookupParameter("VerticalDatum");
            if (vertD != null)
                existVert = vertD.AsString();

            #region Input File - ADV-srsName (no EPSG)

            if (gmlCRS.Contains("DHHN"))
            {
                if (gmlCRS.Contains("2016"))
                    inputVert = "DHHN2016";

                if (gmlCRS.Contains("92"))
                    inputVert = "DHHN92";

                if (gmlCRS.Contains("85"))
                    inputVert = "DHHN85 (NN)";
            }

            if (gmlCRS.Contains("SNN") && gmlCRS.Contains("76"))
                inputVert = "SNN76 (HN)";

            #endregion Input File - ADV-srsName (no EPSG)

            #region Input File - EPSG-srsName

            if (gmlCRS.Contains("EPSG"))
            {
                if (gmlCRS.Contains("5054") || gmlCRS.Contains("5055") || gmlCRS.Contains("5056") ||
                    gmlCRS.Contains("5783"))
                {
                    inputVert = "DHHN92";
                }
                if (gmlCRS.Contains("7037"))
                    inputVert = "DHHN2016";

                if (gmlCRS.Contains("5784"))
                    inputVert = "DHHN85 (NN)";

                if (gmlCRS.Contains("5785"))
                    inputVert = "SNN76 (HN)";
            }

            #endregion Input File - EPSG-srsName

            if (inputVert.Equals(existVert))
                sameCRSh = true;

            #endregion Height comparison

            #region Swap of North-East neccessary

            if (gmlCRS.Contains("3043") ||
                gmlCRS.Contains("3044") ||
                gmlCRS.Contains("3045") ||
                gmlCRS.Contains("5651") ||
                gmlCRS.Contains("5652") ||
                gmlCRS.Contains("5653"))
            { swapNE = true; }

            #endregion Swap of North-East neccessary

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

            for (int i = 0; i < pointsSeperated.Length; i = i + 3)
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

        private bool CheckSameStartEnd(List<C2BPoint> rawPolygon)
        {
            if (rawPolygon.First().X != rawPolygon.Last().X)
                return false;

            if (rawPolygon.First().Y != rawPolygon.Last().Y)
                return false;

            if (rawPolygon.First().Z != rawPolygon.Last().Z)
                return false;

            return true;
        }

        private bool CheckNumberOfPoints(List<C2BPoint> redPolygon)
        {
            if (redPolygon.Count < 4)
                return false;

            return true;
        }

        private List<C2BPoint> CheckDuplicates(List<C2BPoint> redPolygon)
        {
            List<C2BPoint> duplPts = new List<C2BPoint>();

            for (int i = 1; i < redPolygon.Count; i++)
            {
                if (redPolygon[i - 1].X != redPolygon[i].X)
                    continue;

                if (redPolygon[i - 1].Y != redPolygon[i].Y)
                    continue;

                if (redPolygon[i - 1].Z != redPolygon[i].Z)
                    continue;

                duplPts.Add(redPolygon[i]);
            }
            foreach (var dupl in duplPts)
            {
                redPolygon.Remove(dupl);
            }
            return redPolygon;
        }

        private List<C2BPoint> CheckForDeadEndAtAxisPlane(List<C2BPoint> rawPolygonSE, char fixedAxis)       //prüft nur über Koordinatenkomponenetenvergleich und nur für Fall gleiche XY-Koordinaten
        {
            //avoids this:          (dead end will be removed, works only for points with same XY coordinates, dead end along Z axis)
            //       _______      
            //      |       |
            //      |       |
            //      |_______|_________

            List<C2BPoint> deletePt = new List<C2BPoint>();

            var rawPolygon = new List<C2BPoint>();
            rawPolygon.AddRange(rawPolygonSE);
            rawPolygon.RemoveAt(0);

            switch (fixedAxis)
            {
                case 'x':
                    {
                        var sameGroups = rawPolygon.GroupBy(c => Math.Round(c.X, 2));
                        if (sameGroups.Count() == 1)
                            return deletePt;                   //whole Polygon is in XY plane --> e.g. GroundPlane --> no point added
                        break;
                    }

                case 'y':
                    {
                        var sameGroups = rawPolygon.GroupBy(c => Math.Round(c.Y));
                        if (sameGroups.Count() == 1)
                            return deletePt;                   //whole Polygon is in XY plane --> e.g. GroundPlane --> no point added
                        break;
                    }

                case 'z':
                    {
                        var sameGroups = rawPolygon.GroupBy(c => Math.Round(c.Z));
                        if (sameGroups.Count() == 1)
                            return deletePt;                   //whole Polygon is in XY plane --> e.g. GroundPlane --> no point added
                        break;
                    }
            }

            for (int i = 0; i < rawPolygon.Count; i++)
            {
                C2BPoint curr = rawPolygon[i];
                C2BPoint next = rawPolygon[0];
                C2BPoint upperNext = rawPolygon[0];

                if (i < rawPolygon.Count - 2)
                {
                    next = rawPolygon[i + 1];
                    upperNext = rawPolygon[i + 2];
                }
                else if (i == rawPolygon.Count - 2)
                {
                    next = rawPolygon[i + 1];
                    upperNext = rawPolygon[0];
                }
                else
                {
                    next = rawPolygon[0];
                    upperNext = rawPolygon[1];
                }

                if (fixedAxis == 'x')
                {
                    if (curr.X != upperNext.X)         //check to proceed quickly
                        continue;

                    if (curr.X != next.X)         //check to proceed quickly
                        continue;
                }

                if (fixedAxis == 'y')
                {
                    if (curr.Y != upperNext.Y)         //check to proceed quickly
                        continue;

                    if (curr.Y != next.Y)         //check to proceed quickly
                        continue;
                }

                if (fixedAxis == 'z')
                {
                    if (curr.Z != upperNext.Z)         //check to proceed quickly
                        continue;

                    if (curr.Z != next.Z)         //check to proceed quickly
                        continue;
                }

                var d12 = C2BPoint.DistanceSq(curr, next);
                var d23 = C2BPoint.DistanceSq(next, upperNext);
                var d13 = C2BPoint.DistanceSq(curr, upperNext);

                if (d13 > d12 && d13 > d23)     //in this case no dead end
                    continue;

                if (d13 < 0.001 || d12 < 0.001 || d23 < 0.001)
                    continue;

                var delPts = from r in rawPolygonSE
                             where (r.X == next.X && r.Y == next.Y && r.Z == next.Z)
                             select r;

                deletePt.AddRange(delPts);                    //second point is dead end --> will be removed later
            }
            return deletePt;
        }

        private C2BPoint SplitCoordinate(string[] xyzString, C2BPoint lowerCorner)
        {
            double z = Double.Parse(xyzString[2], CultureInfo.InvariantCulture) - lowerCorner.Z;

            //Left-handed (geodetic) vs. right-handed (mathematical) system

            double axis0 = Double.Parse(xyzString[0], CultureInfo.InvariantCulture);
            double axis1 = Double.Parse(xyzString[1], CultureInfo.InvariantCulture);

            return new C2BPoint(axis0 - lowerCorner.X, axis1 - lowerCorner.Y, z);
        }

        private List<GmlSurface> ReadSurfaces(XElement bldgEl, HashSet<XmlAttribute> attributes, out GmlBldg.LodRep lod)
        {
            var bldgParts = bldgEl.Elements(this.allns["bldg"] + "consistsOfBuildingPart");
            var bldg = bldgEl.Elements().Except(bldgParts);

            var surfaces = new List<GmlSurface>();

            bool poly = bldg.Descendants().Where(l => l.Name.LocalName.Contains("Polygon")).Any();

            if (!poly)                  //no polygons --> directly return (e.g. Buildings with Parts but no geometry at building level)
            {
                lod = GmlBldg.LodRep.unknown;
                return surfaces;
            }
            bool lod2 = bldg.DescendantsAndSelf().Where(l => l.Name.LocalName.Contains("lod2")).Count() > 0;
            bool lod1 = bldg.DescendantsAndSelf().Where(l => l.Name.LocalName.Contains("lod1")).Count() > 0;

            if (lod2)
            {
                lod = GmlBldg.LodRep.LOD2;

                #region WallSurfaces

                var lod2Walls = bldg.DescendantsAndSelf(this.allns["bldg"] + "WallSurface");

                foreach (var wall in lod2Walls)
                {
                    List<GmlSurface> wallSurface = ReadSurfaceType(bldgEl, wall, GmlSurface.FaceType.wall);
                    if (wallSurface == null)
                        return null;

                    surfaces.AddRange(wallSurface);
                }

                #endregion WallSurfaces

                #region RoofSurfaces

                var lod2Roofs = bldg.DescendantsAndSelf(this.allns["bldg"] + "RoofSurface");

                foreach (var roof in lod2Roofs)
                {
                    List<GmlSurface> roofSurface = ReadSurfaceType(bldgEl, roof, GmlSurface.FaceType.roof);
                    surfaces.AddRange(roofSurface);
                }

                #endregion RoofSurfaces

                #region GroundSurfaces

                var lod2Grounds = bldg.DescendantsAndSelf(this.allns["bldg"] + "GroundSurface");

                foreach (var ground in lod2Grounds)
                {
                    List<GmlSurface> groundSurface = ReadSurfaceType(bldgEl, ground, GmlSurface.FaceType.ground);
                    surfaces.AddRange(groundSurface);
                }

                #endregion GroundSurfaces

                #region ClosureSurfaces

                var lod2Closures = bldg.DescendantsAndSelf(this.allns["bldg"] + "ClosureSurface");

                foreach (var closure in lod2Closures)
                {
                    List<GmlSurface> closureSurface = ReadSurfaceType(bldgEl, closure, GmlSurface.FaceType.closure);
                    surfaces.AddRange(closureSurface);
                }

                #endregion ClosureSurfaces

                #region OuterCeilingSurfaces

                var lod2OuterCeiling = bldg.DescendantsAndSelf(this.allns["bldg"] + "OuterCeilingSurface");

                foreach (var ceiling in lod2OuterCeiling)
                {
                    List<GmlSurface> outerCeilingSurface = ReadSurfaceType(bldgEl, ceiling, GmlSurface.FaceType.outerCeiling);
                    surfaces.AddRange(outerCeilingSurface);
                }

                #endregion OuterCeilingSurfaces

                #region OuterFloorSurfaces

                var lod2OuterFloor = bldg.DescendantsAndSelf(this.allns["bldg"] + "OuterFloorSurface");

                foreach (var floor in lod2OuterFloor)
                {
                    List<GmlSurface> outerFloorSurface = ReadSurfaceType(bldgEl, floor, GmlSurface.FaceType.outerFloor);
                    surfaces.AddRange(outerFloorSurface);
                }

                #endregion OuterFloorSurfaces
            }
            else if (lod1)
            {
                #region lod1Surfaces

                lod = GmlBldg.LodRep.LOD1;

                //one occurence per building
                var lod1Rep = bldg.DescendantsAndSelf(this.allns["bldg"] + "lod1Solid").FirstOrDefault();

                if (lod1Rep == null)
                    lod1Rep = bldg.DescendantsAndSelf(this.allns["bldg"] + "lod1MultiSurface").FirstOrDefault();

                if (lod1Rep != null)
                {
                    var polys = lod1Rep.Descendants(this.allns["gml"] + "Polygon").ToList();
                    var elemsWithID = lod1Rep.DescendantsAndSelf().Where(a => a.Attribute(allns["gml"] + "id") != null);

                    for (var i = 0; i < polys.Count(); i++)          //normally 1 polygon but sometimes surfaces are grouped under the surface type
                    {
                        var surface = new GmlSurface();

                        var faceID = polys[i].Attribute(allns["gml"] + "id").Value;

                        if (faceID == null)
                        {
                            var gmlSolid = lod1Rep.Descendants(this.allns["gml"] + "Solid").FirstOrDefault();

                            faceID = gmlSolid.Attribute(allns["gml"] + "id").Value;

                            if (faceID == null)
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
            }
            else
                lod = GmlBldg.LodRep.unknown;

            return surfaces;
        }

        private List<GmlSurface> ReadSurfaceType(XElement bldg, XElement gmlSurface, GmlSurface.FaceType type)
        {
            List<GmlSurface> polyList = new List<GmlSurface>();

            var polysR = gmlSurface.Descendants(this.allns["gml"] + "Polygon").ToList();

            for (var i = 0; i < polysR.Count(); i++)          //normally 1 polygon but sometimes surfaces are grouped under the surface type
            {
                var surface = new GmlSurface();

                var faceID = polysR[i].Attribute(allns["gml"] + "id");

                if (faceID != null)
                    surface.SurfaceId = faceID.Value;

                surface.Facetype = type;
                surface.SurfaceAttributes = new ReadSemValues().ReadAttributeValuesSurface(gmlSurface, attributes, type);

                var surfacePl = ReadSurfaceData(polysR[i], surface);
                if (surfacePl == null)
                    return null;

                polyList.Add(surfacePl);
            }
            return polyList;
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

            var ptList = new List<C2BPoint>();

            if (posListExt.Any())
            {
                ptList.AddRange(CollectPoints(posListExt.FirstOrDefault(), this.lowerCornerPt));
            }
            else
            {
                var posExt = exteriorF.Descendants(allns["gml"] + "pos");

                foreach (var pos in posExt)
                {
                    ptList.Add(CollectPoint(pos, this.lowerCornerPt));
                }
            }

            //-------------------Check section-----------------------

            //Start = End

            if (!CheckSameStartEnd(ptList))
                ptList.Add(ptList[0]);

            List<C2BPoint> pointsRed = ptList;
            //-------------

            var deletionXY = CheckForDeadEndAtAxisPlane(pointsRed, 'z');

            if (deletionXY.Count > 0)
            {
                bool addNewStart = false;

                if (deletionXY.Where(p => p.X == pointsRed[0].X && p.Y == pointsRed[0].Y && p.Z == pointsRed[0].Z).Any())
                    addNewStart = true;

                pointsRed = pointsRed.Except(deletionXY).ToList();

                if (addNewStart)
                    pointsRed.Add(pointsRed[0]);
            }

            var deletionXZ = CheckForDeadEndAtAxisPlane(pointsRed, 'y');

            if (deletionXZ.Count > 0)
            {
                bool addNewStart = false;

                if (deletionXZ.Where(p => p.X == pointsRed[0].X && p.Y == pointsRed[0].Y && p.Z == pointsRed[0].Z).Any())
                    addNewStart = true;

                pointsRed = pointsRed.Except(deletionXZ).ToList();

                if (addNewStart)
                    pointsRed.Add(pointsRed[0]);
            }

            var deletionYZ = CheckForDeadEndAtAxisPlane(pointsRed, 'x');

            if (deletionYZ.Count > 0)
            {
                bool addNewStart = false;

                if (deletionYZ.Where(p => p.X == pointsRed[0].X && p.Y == pointsRed[0].Y && p.Z == pointsRed[0].Z).Any())
                    addNewStart = true;

                pointsRed = pointsRed.Except(deletionYZ).ToList();

                if (addNewStart)
                    pointsRed.Add(pointsRed[0]);
            }

            //--------------

            //Duplicates:
            pointsRed = CheckDuplicates(pointsRed);

            //Too few points (possibly reduced point list will be investigated because dead end could be removed)
            if (!CheckNumberOfPoints(pointsRed))
                ptList.Clear();

            //-----------------------------------------------------------

            surface.ExteriorPts = pointsRed;

            #endregion ExteriorPolygon

            #region InteriorPolygon

            //if existent, it also could have more than one hole

            var interiorF = poly.Descendants(this.allns["gml"] + "interior");

            var posListInt = interiorF.Descendants(allns["gml"] + "posList").ToList();

            if (posListInt.Any())
            {
                List<List<C2BPoint>> interiorPolys = new List<List<C2BPoint>>();

                for (var j = 0; j < posListInt.Count(); j++)
                {
                    interiorPolys.Add(CollectPoints(posListInt[j], this.lowerCornerPt));
                }
                surface.InteriorPts = interiorPolys;
            }
            else
            {
                var rings = interiorF.Descendants(allns["gml"] + "LinearRing").ToList();

                List<List<C2BPoint>> interiorPolys = new List<List<C2BPoint>>();

                for (var k = 0; k < rings.Count() - 1; k++)
                {
                    var posInt = rings[k].Descendants(allns["gml"] + "pos");

                    if (posInt.Any())
                    {
                        var ptListI = new List<C2BPoint>();

                        foreach (var pos in posInt)
                        {
                            ptListI.Add(CollectPoint(pos, this.lowerCornerPt));
                        }
                        interiorPolys.Add(ptListI);
                    }
                }
                surface.InteriorPts = interiorPolys;
            }

            #endregion InteriorPolygon

            return surface;
        }

        public List<GmlBldg> ReadGmlData(XDocument gmlDoc/*, DxfVisualizer dxf*/)
        {
            #region LowerCorner

            //For better calculation, Identify lower Corner
            var lowerCorner = gmlDoc.Descendants(this.allns["gml"] + "lowerCorner").FirstOrDefault();
            this.lowerCornerPt = CollectPoint(lowerCorner, new C2BPoint(0, 0, 0));

            #endregion LowerCorner

            //Read all overall building elements
            var gmlBuildings = gmlDoc.Descendants(this.allns["bldg"] + "Building");
            //--------------------------------------------------------------------------

            #region Semantics

            //Read all semantic attributes first:
            //Loop over all buildings, parameter list in Revit needs consistent parameters for object types
            var sem = new ReadSemAttributes();

            //first of all regular schema attributes (inherited by parsing of XML schema for core and bldg, standard specific)
            this.attributes = sem.GetSchemaAttributes();

            //secondly add generic attributes (file specific)
            var genAttr = sem.ReadGenericAttributes(gmlBuildings, this.allns);

            //union for consistent attribute list
            this.attributes.UnionWith(genAttr);
            //--------------------------------------------------------------------------------------------------------------------------

            #endregion Semantics

            #region BuildingInstances

            //set up of individual building elements for overall list
            var gmlBldgs = new List<GmlBldg>();

            foreach (var bldg in gmlBuildings)
            {
                //create instance of GmlBldg
                var gmlBldg = new GmlBldg();
                //use gml_id as id for building
                gmlBldg.BldgId = bldg.Attribute(allns["gml"] + "id").Value;

                gmlBldg.LogEntries = new List<LogPair>();
                gmlBldg.LogEntries.Add(new LogPair(LogType.info, "CityGML-Building_ID: " + gmlBldg.BldgId));

                //read attributes for building (first level, no parts are handled internally in method)
                gmlBldg.BldgAttributes = new ReadSemValues().ReadAttributeValuesBldg(bldg, attributes, allns);

                var surfaces = new List<GmlSurface>();

                surfaces = ReadSurfaces(bldg, attributes, out var lod);

                gmlBldg.Lod = lod.ToString();
                gmlBldg.BldgSurfaces = surfaces;

                //investigation of building parts
                var gmlBuildingParts = bldg.Descendants(this.allns["bldg"] + "BuildingPart");    //in LOD1 und LOD2 possible

                var parts = new List<GmlBldgPart>();

                foreach (var part in gmlBuildingParts)
                {
                    //create instace for building part
                    var gmlBldgPart = new GmlBldgPart();

                    //use gml_id for building part
                    gmlBldgPart.BldgPartId = part.Attribute(allns["gml"] + "id").Value;

                    //read attributes for building part
                    gmlBldgPart.BldgPartAttributes = new ReadSemValues().ReadAttributeValuesBldg(part, attributes, allns);

                    var partSurfaces = new List<GmlSurface>();

                    partSurfaces = ReadSurfaces(part, attributes, out var lodP);

                    gmlBldgPart.Lod = lodP.ToString();
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

        public List<GmlBldg> CalculateSolids(List<GmlBldg> bldgs)
        {
            var newBldgs = new List<GmlBldg>();

            foreach (var bldg in bldgs)
            {
                bldg.BldgSolid = new C2BSolid(bldg.BldgId);

                var surfaces = bldg.BldgSurfaces;

                if (surfaces.Count > 0)
                {
                    foreach (var surface in surfaces)
                    {
                        bldg.BldgSolid.AddPlane(surface.InternalID.ToString(), surface.ExteriorPts, surface.InteriorPts);
                    }

                    bldg.BldgSolid.IdentifySimilarPlanes();

                    bldg.BldgSolid.CalculatePositions();

                    bldg.LogEntries.AddRange(bldg.BldgSolid.LogEntries);
                }

                var parts = bldg.Parts;

                var newParts = new List<GmlBldgPart>();

                foreach (var part in parts)
                {
                    var newPart = part;

                    var partSolid = new C2BSolid(part.BldgPartId);

                    foreach (var partSurface in newPart.PartSurfaces)
                    {
                        partSolid.AddPlane(partSurface.InternalID.ToString(), partSurface.ExteriorPts, partSurface.InteriorPts);
                    }
                    partSolid.IdentifySimilarPlanes();

                    partSolid.CalculatePositions();

                    bldg.LogEntries.Add(new LogPair(LogType.info, "CityGML-BuildingPart_ID: " + part.BldgPartId));

                    bldg.LogEntries.AddRange(partSolid.LogEntries);

                    newPart.PartSolid = partSolid;

                    newParts.Add(newPart);
                }

                bldg.Parts = newParts;

                newBldgs.Add(bldg);
            }
            return newBldgs;
        }
    }
}