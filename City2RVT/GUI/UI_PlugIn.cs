using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace City2RVT.GUI
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class UI_PlugIn : IExternalApplication
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

            #region Georef panel

            RibbonPanel panel2 = application.CreateRibbonPanel(tabName, "Georeferencing");

            /*var globePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Georef_32px_96dpi.png");           
            Uri uriImage = new Uri(globePath);
            BitmapImage largeImage = new BitmapImage(uriImage);*/
            //System.IO.MemoryStream ms = new System.IO.MemoryStream();
            var bitmap = ResourcePictures.Georef_32px_96dpi;
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            BitmapImage largeImage = new BitmapImage();
            ms.Position = 0;
            largeImage.BeginInit();
            largeImage.StreamSource = ms;
            largeImage.EndInit();

            PushButton buttonGeoRef = panel2.AddItem(new PushButtonData("Show Georef information", "Level of Georef",
            thisAssemblyPath, "City2RVT.GUI.Cmd_GeoRefUI")) as PushButton;
            buttonGeoRef.ToolTip = "Show georef information for current project.";
            buttonGeoRef.LargeImage = largeImage; 

            #endregion Georef panel

            #region City2BIM panel

            //---------------------City2BIM panel------------------------------------------------------------

            RibbonPanel panel3 = application.CreateRibbonPanel(tabName, "City2BIM");

            /*var citygmlPath =
                System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "img\\citygml_32px_96dpi.png");
            Uri uriImageC = new Uri(citygmlPath);
            BitmapImage largeImageC = new BitmapImage(uriImageC);*/
            System.IO.MemoryStream msC = new System.IO.MemoryStream();
            var bitmapC = ResourcePictures.citygml_32px_96dpi;
            bitmapC.Save(msC, System.Drawing.Imaging.ImageFormat.Png);
            BitmapImage largeImageC = new BitmapImage();
            msC.Position = 0;
            largeImageC.BeginInit();
            largeImageC.StreamSource = msC;
            largeImageC.EndInit();

            PulldownButton btCityGml = panel3.AddItem(new PulldownButtonData("LoadCityGML", "Import City Model...")) as PulldownButton;
            btCityGml.ToolTip = "Import functionality for CityGML data.";
            btCityGml.LargeImage = largeImageC;

            PushButtonData btSolid = new PushButtonData("LoadCityGMLSolids", "... as Solids",
            thisAssemblyPath, "City2RVT.GUI.Cmd_ReadCityGMLSolids")
            {
                ToolTip = "Imports one Solid representation for each Building(part) to Revit category Entourage."
            };

            PushButtonData btSurfaces = new PushButtonData("LoadCityGMLFaces", "... as Surfaces",
            thisAssemblyPath, "City2RVT.GUI.Cmd_ReadCityGMLFaces")
            {
                ToolTip = "Imports every Surface to the matching Revit category Wall, Roof or Slab."
            };

            btCityGml.AddPushButton(btSurfaces);
            btCityGml.AddPushButton(btSolid);

            /*var citygmlsetPath =
                System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "img\\citygml_set_32px.png");
            Uri uriImageCSet = new Uri(citygmlsetPath);
            BitmapImage largeImageCSet = new BitmapImage(uriImageCSet);*/
            System.IO.MemoryStream msCSet = new System.IO.MemoryStream();
            var bitmapCSet = ResourcePictures.citygml_set_32px;
            bitmapCSet.Save(msCSet, System.Drawing.Imaging.ImageFormat.Png);
            BitmapImage largeImageCSet = new BitmapImage();
            msCSet.Position = 0;
            largeImageCSet.BeginInit();
            largeImageCSet.StreamSource = msCSet;
            largeImageCSet.EndInit();

            PushButton btCitySettings = panel3.AddItem(new PushButtonData("OpenCityGMLSettings", "Settings",
            thisAssemblyPath, "City2RVT.GUI.Cmd_OpenSettings")) as PushButton;
            btCitySettings.ToolTip = "Import settings for city model.";
            btCitySettings.LargeImage = largeImageCSet;

            //-------------------------------------------------------------------------------------------------

            #endregion City2BIM panel

            #region ALKIS panel

            RibbonPanel panel4 = application.CreateRibbonPanel(tabName, "ALKIS2BIM");

            /*var alkisPath =
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "img\\ALKIS_32px_96dpi.png");
            Uri uriImageA = new Uri(alkisPath);
            BitmapImage largeImageA = new BitmapImage(uriImageA);*/
            System.IO.MemoryStream msA = new System.IO.MemoryStream();
            var bitmapA = ResourcePictures.ALKIS_32px_96dpi;
            bitmapA.Save(msA, System.Drawing.Imaging.ImageFormat.Png);
            BitmapImage largeImageA = new BitmapImage();
            msA.Position = 0;
            largeImageA.BeginInit();
            largeImageA.StreamSource = msA;
            largeImageA.EndInit();

            PushButton buttonAlkis = panel4.AddItem(new PushButtonData("LoadALKIS", "Import ALKIS data",
            thisAssemblyPath, "City2RVT.GUI.Cmd_ReadALKIS")) as PushButton;
            buttonAlkis.ToolTip = "Import functionality ALKIS data from NAS-XML files.)";
            buttonAlkis.LargeImage = largeImageA;

            /*var alkisxmlsetPath =
                 System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "ALKISset_32px.png");
            Uri uriImageCAlk = new Uri(alkisxmlsetPath);
            BitmapImage largeImageCAlk = new BitmapImage(uriImageCAlk);*/
            System.IO.MemoryStream msCAlk = new System.IO.MemoryStream();
            var bitmapCAlk = ResourcePictures.ALKISset_32px;
            bitmapCAlk.Save(msCAlk, System.Drawing.Imaging.ImageFormat.Png);
            BitmapImage largeImageCAlk = new BitmapImage();
            msCAlk.Position = 0;
            largeImageCAlk.BeginInit();
            largeImageCAlk.StreamSource = msCAlk;
            largeImageCAlk.EndInit();

            PushButton btAlkisSettings = panel4.AddItem(new PushButtonData("OpenALKISSettings", "Settings",
            thisAssemblyPath, "City2RVT.GUI.Cmd_OpenALKISSettings")) as PushButton;
            btAlkisSettings.ToolTip = "Import settings for ALKIS data.";
            btAlkisSettings.LargeImage = largeImageCAlk;


            #endregion ALKIS panel

            #region Terrain panel

            RibbonPanel panel5 = application.CreateRibbonPanel(tabName, "DTM2BIM");

            /*var terrainPath =
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "img\\DTM_32px_96dpi.png");
            Uri uriImageT = new Uri(terrainPath);
            BitmapImage largeImageT = new BitmapImage(uriImageT);*/
            System.IO.MemoryStream msT = new System.IO.MemoryStream();
            var bitmapT = ResourcePictures.DTM_32px_96dpi;
            bitmapT.Save(msT, System.Drawing.Imaging.ImageFormat.Png);
            BitmapImage largeImageT = new BitmapImage();
            msT.Position = 0;
            largeImageT.BeginInit();
            largeImageT.StreamSource = msT;
            largeImageT.EndInit();

            PushButton buttonDTM = panel5.AddItem(new PushButtonData("DTM_Importer", "Get Terrain data",
            thisAssemblyPath, "City2RVT.GUI.Cmd_ReadTerrainXYZ")) as PushButton;
            buttonDTM.ToolTip = "Import functionality for Digital Terrain Models out of XYZ data (regular grid)";
            buttonDTM.LargeImage = largeImageT;

            #endregion Terrain panel

            #region IFC Export panel

            RibbonPanel panel6 = application.CreateRibbonPanel(tabName, "IFC Export");

            /*var ifcExportPath =
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "img\\IFC_32px_96dpi.png");
            Uri uriImageIfc = new Uri(ifcExportPath);
            BitmapImage largeImageIfc = new BitmapImage(uriImageIfc);*/
            System.IO.MemoryStream msIfc = new System.IO.MemoryStream();
            var bitmapIfc = ResourcePictures.IFC_32px_96dpi;
            bitmapIfc.Save(msIfc, System.Drawing.Imaging.ImageFormat.Png);
            BitmapImage largeImageIfc = new BitmapImage();
            msIfc.Position = 0;
            largeImageIfc.BeginInit();
            largeImageIfc.StreamSource = msIfc;
            largeImageIfc.EndInit();

            PushButton buttonIFC = panel6.AddItem(new PushButtonData("IFC_Exporter", "Export data to IFC-file",
            thisAssemblyPath, "City2RVT.GUI.Cmd_ExportIFC")) as PushButton;
            buttonIFC.ToolTip = "Export functionality for writing IFC files.";
            buttonIFC.LargeImage = largeImageIfc;

            #endregion IFC Export panel

            #region XPlanung panel
            RibbonPanel panel7 = application.CreateRibbonPanel(tabName, "XPlanung");

            var xPlanPath =
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            var xPlanRootPath = Path.Combine(xPlanPath, "..", "..");
            var xPlanSetPath = Path.Combine(xPlanRootPath, "img", "XPlan_32px.png");
            Uri uriImageXPlan = new Uri(xPlanSetPath);
            BitmapImage largeImageXPLan = new BitmapImage(uriImageXPlan);

            PushButton buttonXPlan = panel7.AddItem(new PushButtonData("XPlanung_Importer", "Import XPlanung-file",
            thisAssemblyPath, "City2RVT.GUI.Cmd_ImportXPlan")) as PushButton;
            buttonXPlan.ToolTip = "Import functionality for XPlanung files.";
            buttonXPlan.LargeImage = largeImageXPLan;

            var xPlan2IFCPath =
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            var xPlan2IFCRootPath = Path.Combine(xPlan2IFCPath, "..", "..");
            var xPlan2IFCSetPath = Path.Combine(xPlan2IFCRootPath, "img", "IFC_32px_96dpi.png");
            Uri uriImageXPlan2IFC = new Uri(xPlan2IFCSetPath);
            BitmapImage largeImageXPLan2IFC = new BitmapImage(uriImageXPlan2IFC);

            PushButton buttonXPlan2IFC = panel7.AddItem(new PushButtonData("XPlanung2IFC", "Exports XPlanung to IFC",
            thisAssemblyPath, "City2RVT.GUI.XPlan2BIM.Cmd_ExportXPlan2IFC")) as PushButton;
            buttonXPlan2IFC.ToolTip = "IFC Export functionality for XPlanung files.";
            buttonXPlan2IFC.LargeImage = largeImageXPLan2IFC;

            var hideLayerPath =
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            var hideLayerRootPath = Path.Combine(hideLayerPath, "..", "..");
            var hideLayerSetPath = Path.Combine(hideLayerRootPath, "img", "IFC_32px_96dpi.png");
            Uri uriImagehideLayer = new Uri(hideLayerSetPath);
            BitmapImage largeImagehideLayer = new BitmapImage(uriImagehideLayer);

            PushButton buttonHideLayer = panel7.AddItem(new PushButtonData("Hide Layer", "Hides Layer in view",
            thisAssemblyPath, "City2RVT.GUI.XPlan2BIM.Cmd_HideLayers")) as PushButton;
            buttonHideLayer.ToolTip = "Hides Layers.";
            buttonHideLayer.LargeImage = largeImagehideLayer;

            var mergeIfcPath =
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            var mergeIfcRootPath = Path.Combine(mergeIfcPath, "..", "..");
            var mergeIfcSetPath = Path.Combine(mergeIfcRootPath, "img", "IFC_32px_96dpi.png");
            Uri mergeIfcUriImage = new Uri(mergeIfcSetPath);
            BitmapImage mergeIfcLargeImage = new BitmapImage(mergeIfcUriImage);

            PushButton mergeIfcButton = panel7.AddItem(new PushButtonData("Merge Ifc", "Merge Ifc files",
            thisAssemblyPath, "City2RVT.GUI.XPlan2BIM.Cmd_MergeIfc")) as PushButton;
            mergeIfcButton.ToolTip = "Merge Ifc.";
            mergeIfcButton.LargeImage = mergeIfcLargeImage;


            #endregion XPlanung panel

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