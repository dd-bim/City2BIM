using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

using System.Xml.Linq;
using System;
using System.Collections.Generic;
using System.IO;

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
                            if (!File.Exists(path))
                            {
                                TaskDialog.Show("Error", "Could not find a file for specified path");
                                return Result.Failed;
                            }
                            xdoc = XDocument.Load(path);
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Probleme while loading specified file during CityGML import. Is file a XML-File?");
                            Log.Error(ex.ToString());
                            TaskDialog.Show("Error", ex.ToString());
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
