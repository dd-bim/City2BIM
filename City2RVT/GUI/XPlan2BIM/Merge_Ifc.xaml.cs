using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Xml;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;

using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc4.PresentationOrganizationResource;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.PresentationAppearanceResource;
using Xbim.Ifc4.TopologyResource;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.SharedBldgElements;
using Xbim.Ifc4.ProfileResource;
using Xbim.Ifc4.MaterialResource;

using NLog;
using NLog.Targets;
using NLog.Config;

namespace City2RVT.GUI.XPlan2BIM
{
    /// <summary>
    /// Interaktionslogik für Merge_Ifc.xaml
    /// </summary>
    public partial class Merge_Ifc : Window
    {
        ExternalCommandData commandData;
        public Merge_Ifc(ExternalCommandData cData)
        {
            commandData = cData;

            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //const string original = @"D:\Daten\\Bauwerksmodell.ifc";
            //const string inserted = @"D:\Daten\\revit2ifc.ifc";
            const string original = @"D:\Daten\\revit2ifc.ifc";
            //const string inserted = @"D:\Daten\\merged_ifc.ifc";
            const string inserted = @"D:\Daten\\Bauwerksmodell.ifc";


            System.Windows.Forms.MessageBox.Show(original);

            PropertyTranformDelegate semanticFilter = (property, parentObject) =>
            {
                ////leave out geometry and placement
                //if (parentObject is IIfcProduct &&
                //    (property.PropertyInfo.Name == nameof(IIfcProduct.Representation) ||
                //    property.PropertyInfo.Name == nameof(IIfcProduct.ObjectPlacement)))
                //    return null;

                ////leave out mapped geometry
                //if (parentObject is IIfcTypeProduct &&
                //     property.PropertyInfo.Name == nameof(IIfcTypeProduct.RepresentationMaps))
                //    return null;

                ////only bring over IsDefinedBy and IsTypedBy inverse relationships which will take over all properties and types
                //if (property.EntityAttribute.Order < 0 && !(
                //    property.PropertyInfo.Name == nameof(IIfcProduct.IsDefinedBy) ||
                //    property.PropertyInfo.Name == nameof(IIfcProduct.IsTypedBy)
                //    ))
                //    return null;

                return property.PropertyInfo.GetValue(parentObject, null);
            };

            using (var model = IfcStore.Open(original))
            {
                var topographies = model.Instances.OfType<IfcSite>();
                using (var iModel = IfcStore.Create(model.SchemaVersion, Xbim.IO.XbimStoreType.InMemoryModel))
                {
                    using (var txn = iModel.BeginTransaction("Insert copy"))
                    {
                        //single map should be used for all insertions between two models
                        var map = new XbimInstanceHandleMap(model, iModel);

                        foreach (var topography in topographies)
                        {
                            iModel.InsertCopy(topography, map, semanticFilter, true, false);
                        }

                        txn.Commit();
                    }

                    iModel.SaveAs(inserted);
                }
            }

        }
    }
}
