using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using City2BIM.Geometry;
using City2BIM.GmlRep;
using City2RVT.GUI;
using City2RVT.Builder;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using City2BIM.WFS;

namespace City2RVT.Reader
{
    public class ReadCityGML
    {
        // The main Execute method (inherited from IExternalCommand) must be public
        public ReadCityGML(Document doc, bool solid)
        {
            XDocument gmlDoc;

            if (Prop_CityGML_settings.IsServerRequest)
            {
                //server url as specified (if no change VCS server will be called)
                string wfsUrl = Prop_CityGML_settings.ServerUrl;

                //to ensure correct coordinate order (VCS response is always YXZ order)
                Prop_CityGML_settings.IsGeodeticSystem = true;

                //client class for xml-POST request from WFS server
                WFSClient client = new WFSClient(wfsUrl);

                //response with parameters: Site-Lon, Site-Lat, extent, max response of bldgs, CRS)
                //Site coordinates from Revit.SiteLocation
                //extent from used-defined def (default: 300 m)
                //max response dependent of server settings (at VCS), currently 500
                //CRS:  supported from server are currently: Pseudo-Mercator (3857), LatLon (4326), German National Systems: West(25832), East(25833)
                //      supported by PlugIn are only the both German National Systems

                if (Prop_GeoRefSettings.Epsg != "EPSG:25832" && Prop_GeoRefSettings.Epsg != "EPSG:25833")
                    TaskDialog.Show("EPSG not supported!", "Only EPSG:25832 or EPSG:25833 will be supported by server. Please change the EPSG-Code in Georeferencing window.");

                gmlDoc = client.getFeaturesCircle(Prop_CityGML_settings.ServerCoord[0], Prop_CityGML_settings.ServerCoord[1], Prop_CityGML_settings.Extent, 500, Prop_GeoRefSettings.Epsg);

                if (Prop_CityGML_settings.SaveServerResponse)
                {
                    gmlDoc.Save(Prop_CityGML_settings.PathResponse + "\\" + Math.Round(Prop_CityGML_settings.ServerCoord[1], 4) + "_" + Math.Round(Prop_CityGML_settings.ServerCoord[0], 4) + ".gml");
                }
            }
            else
            {
                //local file path
                var path = Prop_CityGML_settings.FileUrl;

                //Load XML document from local file
                gmlDoc = XDocument.Load(path);

            }

            var gmlRead = new City2BIM.CityGMLReader(gmlDoc, solid); /*gmlDoc,*/ /*, user-defined calculation parameters may from extended GUI or from config file*/
        
            
            string gmlCRS = gmlRead.GmlCRS;

            bool sameXY = false, sameHeight = false;
            CheckInputCRS(doc, gmlCRS, ref sameXY, ref sameHeight);

            bool continueImport = true;

            if (!sameXY || !sameHeight)
            {
                using (TaskDialog crsDialog = new TaskDialog("Warning")
                {
                    AllowCancellation = true,

                    MainInstruction = "Different CRS in input file and Georef settings detected!"
                })
                {
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
            }

            if (continueImport)
            {
                List<CityGml_Bldg> gmlBuildings = gmlRead.GmlBuildings;
                C2BPoint lowerCorner = gmlRead.LowerCornerPt;
                var attributes = gmlRead.Attributes;

                //erstellt Revit-seitig die Geometrie und ordnet Attributwerte zu (Achtung: ReadXMLDoc muss vorher ausgeführt werden)
                RevitCityBuilder cityModel = new Builder.RevitCityBuilder(doc, gmlBuildings, lowerCorner);

                if (solid)
                    cityModel.CreateBuildings();
                else
                    cityModel.CreateBuildingsWithFaces();


                Revit_Semantic citySem = new Builder.Revit_Semantic(doc); //Übergabe der Methoden-Rückgaben zum Schreiben nach Revit
                citySem.CreateParameters(attributes); //erstellt Shared Parameters für Kategorie Umgebung
            }
        }

        /// <summary>
        /// Compares georef settings and input crs from gml-file
        /// </summary>
        /// <param name="revDoc">Active Revit document</param>
        /// <param name="gmlDoc">Imported GML document</param>
        /// <param name="sameCRSxy">ref for decision of same XY-crs</param>
        /// <param name="sameCRSh">ref for decision of same height-crs</param>
        private void CheckInputCRS(Document revDoc, string gmlCRS, ref bool sameCRSxy, ref bool sameCRSh)
        {
            var geoInfo = revDoc.ProjectInformation;

            #region XY comparison

            string inputCRS = "";
            string existCRS = "";
            string revCRSxy = "";

            var epsg = geoInfo.LookupParameter("Name");
            if (epsg != null)
                revCRSxy = epsg.AsString();

            #region Input File - ADV-srsName (no EPSG)

            if (gmlCRS.Contains("ETRS_89") ||
                gmlCRS.Contains("ETRS89") ||
                gmlCRS.Contains("ETRS_1989") ||
                gmlCRS.Contains("ETRS1989"))
            {
                if (gmlCRS.Contains("UTM") && gmlCRS.Contains("32"))
                    inputCRS = "UTM32";

                if (gmlCRS.Contains("UTM") && gmlCRS.Contains("33"))
                    inputCRS = "UTM33";
            }

            #endregion Input File - ADV-srsName (no EPSG)

            #region Input File - EPSG-srsName

            if (gmlCRS.Contains("EPSG") || gmlCRS.Contains("epsg"))
            {
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
            {
                Prop_CityGML_settings.IsGeodeticSystem = false;
            }

            #endregion Swap of North-East neccessary
        }
    }
}