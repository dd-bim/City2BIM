using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace City2RVT.GUI.Modify
{
    /// <remarks>
    /// The "HelloWorld" external command. The class must be Public.
    /// </remarks>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class Cmd_HideLayers : IExternalCommand
    {
        //ExternalCommandData commandData;
        // The main Execute method (inherited from IExternalCommand) must be public
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            Prop_GeoRefSettings.SetInitialSettings(doc);

            var view = commandData.Application.ActiveUIDocument.ActiveView;

            using (Transaction trans = new Transaction(doc, "Modify Layer Visibility"))
            {
                trans.Start();
                
                //Alkis / XPlanung 
                FilteredElementCollector topoCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Topography);

                List<LayerStatus> layerStatusList = new List<LayerStatus>();

                foreach (var element in topoCollector)
                {
                    string nameParam = element.LookupParameter("Name").AsString();
                    if (nameParam != null && nameParam.StartsWith("Ref"))
                    {
                        bool isHidden = element.IsHidden(view);
                        layerStatusList.Add(new LayerStatus { LayerName = nameParam, Visibility = !isHidden });

                    }
                }

                //CityGML
                FilteredElementCollector cityGMLCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Entourage);
                var citySchema = utils.getSchemaByName("CityGMLImportSchema");
                var isHiddenCityGML = false;
                List<ElementId> CityGMLElements = new List<ElementId>();

                foreach (var element in cityGMLCollector)
                {
                    if (element.GetEntity(citySchema).IsValid() && element.GetEntity(citySchema) != null)
                    {
                        isHiddenCityGML = element.IsHidden(view);
                        CityGMLElements.Add(element.Id);
                        //layerStatusList.Add(new LayerStatus { LayerName = "CityGML Buildings", Visibility = !isHidden });
                    }
                }

                if (CityGMLElements.Count > 0)
                {
                    layerStatusList.Add(new LayerStatus { LayerName = "CityGMLBuildings", Visibility = !isHiddenCityGML });
                }
                

                var layerUI = new LayerSelector(layerStatusList);
                layerUI.ShowDialog();

                List<ElementId> layerToShow = new List<ElementId>();
                List<ElementId> layerToHide = new List<ElementId>();

                //loop layer die gezeigt werden sollen
                foreach (var visibleLayer in layerUI.visibleLayers)
                {
                    //ref plane layer ermitteln und abhängige subregions in liste schreiben
                    foreach (var element in topoCollector)
                    {
                        string nameParam = element.LookupParameter("Name").AsString();

                        if (nameParam != null && nameParam.Equals(visibleLayer))
                        {
                            var topoSurf = element as TopographySurface;
                            layerToShow.Add(topoSurf.Id);
                            layerToShow.AddRange(topoSurf.GetHostedSubRegionIds());
                        }
                    }
                }

                if (layerUI.visibleLayers.Contains("CityGMLBuildings"))
                {
                    layerToShow.AddRange(CityGMLElements);
                }

                foreach (var unvisibleLayer in layerUI.unvisibleLayers)
                {
                    foreach (var element in topoCollector)
                    {
                        string nameParam = element.LookupParameter("Name").AsString();

                        if (nameParam != null && nameParam.Equals(unvisibleLayer))
                        {
                            var topoSurf = element as TopographySurface;
                            layerToHide.Add(topoSurf.Id);
                            layerToHide.AddRange(topoSurf.GetHostedSubRegionIds());
                        }
                    }
                }

                if (layerUI.unvisibleLayers.Contains("CityGMLBuildings"))
                {
                    layerToHide.AddRange(CityGMLElements);
                }

                
                
                if (layerToHide.Count > 0)
                {
                    view.HideElements(layerToHide);
                }

                if (layerToShow.Count > 0)
                {
                    view.UnhideElements(layerToShow);
                }

                trans.Commit();
            }
            
            return Result.Succeeded;
        }
    }

    public class LayerStatus : INotifyPropertyChanged
    {
        private string layerName;
        private bool visible;

        public string LayerName
        {
            get { return layerName; }
            set
            {
                layerName = value;
            }
        }

        public bool Visibility
        {
            get { return visible; }
            set
            {
                visible = value;
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }

}
