using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

using System.Xml.Linq;
using System;
using System.Collections.Generic;

using Serilog;

using BIMGISInteropLibs.CityGML;
using BIMGISInteropLibs.Geometry;
using BIMGISInteropLibs.WFS;
using CityBIM.Builder;

//namespace CityBIM.GUI
namespace CityBIM.GUI.City2BIM
{
    /// <remarks>
    /// The "HelloWorld" external command. The class must be Public.
    /// </remarks>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class Cmd_ImportCityGML : IExternalCommand
    {
        // The main Execute method (inherited from IExternalCommand) must be public
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            Document doc = revit.Application.ActiveUIDocument.Document;

            Prop_GeoRefSettings.SetInitialSettings(doc);

            var dialog = new CityGML_ImportUI();

            dialog.ShowDialog();

            if (dialog.StartImport)
            {
                var importSettings = dialog.ImportSettings;

                XDocument xdoc = null;

                switch (importSettings.ImportSource)
                {
                    case CitySource.File:
                        try
                        {
                            string path = importSettings.FilePath;
                            xdoc = XDocument.Load(path);
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Probleme while loading specified file during CityGML import. Is file a XML-File?");
                            Log.Error(ex.ToString());
                            TaskDialog.Show("Error", ex.ToString());
                        }
                        break;

                    case CitySource.Server:
                        try
                        {
                            //client class for xml-POST request from WFS server
                            WFSClient client = new WFSClient(importSettings.serverURL);

                            //response with parameters: Site-Lon, Site-Lat, extent, max response of bldgs, CRS)
                            //Site coordinates from Revit.SiteLocation
                            //extent from used-defined def (default: 300 m)
                            //max response dependent of server settings (at VCS), currently 500
                            //CRS:  supported from server are currently: Pseudo-Mercator (3857), LatLon (4326), German National Systems: West(25832), East(25833)
                            //      supported by PlugIn are only the both German National Systems

                            if (Prop_GeoRefSettings.Epsg != "EPSG:25832" && Prop_GeoRefSettings.Epsg != "EPSG:25833")
                                TaskDialog.Show("EPSG not supported!", "Only EPSG:25832 or EPSG:25833 will be supported by server. Please change the EPSG-Code in Georeferencing window.");

                            xdoc = client.getFeaturesCircle(importSettings.CenterCoords[1], importSettings.CenterCoords[0], importSettings.Extent, 500, Prop_GeoRefSettings.Epsg);

                            if (importSettings.saveResponse)
                            {
                                xdoc.Save(importSettings.FolderPath + "\\" + Math.Round(importSettings.CenterCoords[0], 4) + "_" + Math.Round(importSettings.CenterCoords[1], 4) + ".gml");
                            }
                        }

                        catch (Exception ex)
                        {
                            TaskDialog.Show("Error", "Could not process server request. See log-file for further information");
                            Log.Error("Error during processing of CityGML Server Request");
                            Log.Error(ex.ToString());
                        }
                        break;

                    default:
                        break;
                }

                if (xdoc != null)
                {
                    importSettings.XDoc = xdoc;
                    CityGMLReader cityReader;

                    switch (importSettings.ImportGeomType)
                    {
                        case CityGeometry.Faces:
                            cityReader = new CityGMLReader(importSettings.XDoc, false);
                            break;
                        case CityGeometry.Solid:
                            cityReader = new CityGMLReader(importSettings.XDoc, true);
                            break;
                        default:
                            cityReader = new CityGMLReader(importSettings.XDoc, true);
                            break;
                    }

                    List<CityGml_Bldg> gmlBuildings = cityReader.GmlBuildings;
                    C2BPoint lowerCorner = cityReader.LowerCornerPt;
                    var attributes = cityReader.Attributes;


                    //Field names in extensible storage does not allow "-" 
                    foreach (var attr in attributes)
                    {
                        if (attr.Name.Contains("-")) {
                            attr.Name = attr.Name.Replace('-', '_');
                        }
                    }

                    if (gmlBuildings.Count < 1)
                    {
                        TaskDialog.Show("Error", "No CityGML buildings read from file, canceling import!");
                        return Result.Failed;
                    }

                    //erstellt Revit-seitig die Geometrie und ordnet Attributwerte zu
                    CityBuilder cityModel = new CityBuilder(doc, gmlBuildings, lowerCorner, attributes, importSettings.CodeTranslate);

                    if (importSettings.ImportGeomType == CityGeometry.Solid)
                        cityModel.CreateBuildings();
                    else
                        cityModel.CreateBuildingsWithFaces();
                }
                TaskDialog.Show("Information", "Import process finished!");
            }
            return Result.Succeeded;
        }
    }
}
