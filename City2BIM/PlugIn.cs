﻿using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace City2BIM
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class PlugIn : IExternalApplication
    {
        // Both OnStartup and OnShutdown must be implemented as public method
        // Method "OnStartup" is a Method of IExternalApplication Interface and executes some tasks when Revit starts
        public Result OnStartup(UIControlledApplication application)
        {
            //DB RibbonPanel panel = ribbonPanel(application);
            // Create a push button to trigger a command add it to the ribbon panel.
            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;

            // Add a new Tab
            string tabName = "City2BIM";
            application.CreateRibbonTab(tabName);

            #region Settings panel

            // Add a new ribbon panel
            RibbonPanel panel1 = application.CreateRibbonPanel(tabName, "Settings");

            PushButton btSettings =
                panel1.AddItem(new PushButtonData("Create info table", "Import settings", thisAssemblyPath, "City2BIM.ImportSource")) as PushButton;
            btSettings.ToolTip = "Test window appearing";

            #endregion Settings panel

            #region Georef panel

            RibbonPanel panel2 = application.CreateRibbonPanel(tabName, "Georeferencing");

            var globePath =
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Georef_32px_96dpi.png");
            Uri uriImage = new Uri(globePath);
            BitmapImage largeImage = new BitmapImage(uriImage);

            PushButton buttonGeoRef = panel2.AddItem(new PushButtonData("Show Georef information", "Level of Georef",
            thisAssemblyPath, "City2BIM.GeorefUI")) as PushButton;
            buttonGeoRef.ToolTip = "Show georef information for current project.";
            buttonGeoRef.LargeImage = largeImage;

            #endregion Georef panel

            #region City2BIM panel

            //---------------------City2BIM panel------------------------------------------------------------

            RibbonPanel panel3 = application.CreateRibbonPanel(tabName, "City2BIM");

            var citygmlPath =
                System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "citygml_32px_96dpi.png");
            Uri uriImageC = new Uri(citygmlPath);
            BitmapImage largeImageC = new BitmapImage(uriImageC);

            PulldownButton btCityGml = panel3.AddItem(new PulldownButtonData("LoadCityGML", "Import City Model...")) as PulldownButton;
            btCityGml.ToolTip = "Import functionality for CityGML data.";
            btCityGml.LargeImage = largeImageC;

            PushButtonData btSolid = new PushButtonData("LoadCityGMLSolids", "... as Solids",
            thisAssemblyPath, "City2BIM.ReadCityGMLSolids");
            btSolid.ToolTip = "Imports one Solid representation for each Building(part) to Revit category Entourage.";

            PushButtonData btSurfaces = new PushButtonData("LoadCityGMLFaces", "... as Surfaces",
            thisAssemblyPath, "City2BIM.ReadCityGMLFaces");
            btSurfaces.ToolTip = "Imports every Surface to the matching Revit category Wall, Roof or Slab.";

            btCityGml.AddPushButton(btSurfaces);
            btCityGml.AddPushButton(btSolid);

            //PushButton buttonCode = panel3.AddItem(new PushButtonData("Get CityGML Code Description", "Get Code Decription",
            //thisAssemblyPath, "City2BIM.ReadCode")) as PushButton;
            //buttonCode.ToolTip = "Import functionality for CityGML data";

            //RadioButtonGroupData radioGeom = new RadioButtonGroupData("RadioGmlGeom");
            //RadioButtonGroup radioGeomGroup = panel3.AddItem(radioGeom) as RadioButtonGroup;

            //// create toggle buttons and add to radio button group
            //ToggleButtonData tbFaces = new ToggleButtonData("toggleButton1", "Import Surfaces");
            //tbFaces.ToolTip = "CityGML geometry will be imported as Surfaces in Wall/Roof/Slab category.";

            //ToggleButtonData tbSolids = new ToggleButtonData("toggleButton2", "Green");
            //tbSolids.ToolTip = "CityGML geometry will be imported as Solids in Entourage category.";

            //radioGeomGroup.AddItem(tbFaces);
            //radioGeomGroup.AddItem(tbSolids);

            //-------------------------------------------------------------------------------------------------

            #endregion City2BIM panel

            #region ALKIS panel

            RibbonPanel panel4 = application.CreateRibbonPanel(tabName, "ALKIS2BIM");

            var alkisPath =
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ALKIS_32px_96dpi.png");
            Uri uriImageA = new Uri(alkisPath);
            BitmapImage largeImageA = new BitmapImage(uriImageA);

            PushButton buttonAlkis = panel4.AddItem(new PushButtonData("LoadALKIS", "Import ALKIS data",
            thisAssemblyPath, "City2BIM.ReadALKIS")) as PushButton;
            buttonAlkis.ToolTip = "Import functionality ALKIS data from NAS-XML files.)";
            buttonAlkis.LargeImage = largeImageA;

            #endregion ALKIS panel

            #region Terrain panel

            RibbonPanel panel5 = application.CreateRibbonPanel(tabName, "DTM2BIM");

            var terrainPath =
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DTM_32px_96dpi.png");
            Uri uriImageT = new Uri(terrainPath);
            BitmapImage largeImageT = new BitmapImage(uriImageT);

            PushButton buttonDTM = panel5.AddItem(new PushButtonData("DTM_Importer", "Get Terrain data",
            thisAssemblyPath, "City2BIM.ReadTerrainXYZ")) as PushButton;
            buttonDTM.ToolTip = "Import functionality for Digital Terrain Models out of XYZ data (regular grid)";
            buttonDTM.LargeImage = largeImageT;

            #endregion Terrain panel

            #region IFC Export panel

            RibbonPanel panel6 = application.CreateRibbonPanel(tabName, "IFC Export");

            var ifcExportPath =
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "IFC_32px_96dpi.png");
            Uri uriImageIfc = new Uri(ifcExportPath);
            BitmapImage largeImageIfc = new BitmapImage(uriImageIfc);

            PushButton buttonIFC = panel6.AddItem(new PushButtonData("IFC_Exporter", "Export data to IFC-file",
            thisAssemblyPath, "City2BIM.ExportIFC")) as PushButton;
            buttonIFC.ToolTip = "Export functionality for writing IFC files.";
            buttonIFC.LargeImage = largeImageIfc;

            #endregion IFC Export panel

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            // nothing to clean up in this simple case
            return Result.Succeeded;
        }

        //public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        //{
        //    throw new NotImplementedException();
        //}
    }
}