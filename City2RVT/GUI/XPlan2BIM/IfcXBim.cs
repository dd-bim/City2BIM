using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
//using System.Windows.Media.Media3D;
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
using System.Windows.Forms;
using Xbim.Ifc4.TopologyResource;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.SharedBldgElements;
using Xbim.Ifc4.ProfileResource;
using Xbim.Ifc4.MaterialResource;
using System.Xml;

namespace City2RVT.GUI.XPlan2BIM
{
    public class IfcXBim
    {
        public static IfcStore CreateandInitModel(string projectName)
        {
            //first we need to set up some credentials for ownership of data in the new model
            var credentials = new XbimEditorCredentials
            {
                ApplicationDevelopersName = "LandBIMTeam",
                ApplicationFullName = "Revit to IFC Application",
                ApplicationIdentifier = "IFC.exe",
                ApplicationVersion = "1.0",
                EditorsFamilyName = "Team",
                EditorsGivenName = "LandBIM",
                EditorsOrganisationName = "LandBIMTeam"
            };
            //now we can create an IfcStore, it is in Ifc4 format and will be held in memory rather than in a database
            //database is normally better in performance terms if the model is large >50MB of Ifc or if robust transactions are required

            var model = IfcStore.Create(credentials, XbimSchemaVersion.Ifc4, Xbim.IO.XbimStoreType.InMemoryModel);

            //Begin a transaction as all changes to a model are ACID
            using (var txn = model.BeginTransaction("Initialise Model"))
            {
                //create a project
                var project = model.Instances.New<IfcProject>();
                //set the units to metres
                project.Initialize(ProjectUnits.SIUnitsUK);
                project.Name = projectName;
                project.UnitsInContext.SetOrChangeSiUnit(IfcUnitEnum.LENGTHUNIT, IfcSIUnitName.METRE, null);

                //set a few basic properties
                model.Instances.New<IfcRelDefinesByProperties>(rel =>
                {
                    rel.RelatedObjects.Add(project);
                    rel.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pset =>
                    {
                        pset.Name = "IfcExportTopographie";

                        pset.HasProperties.AddRange(new[]
                        {
                            model.Instances.New<IfcPropertySingleValue>(p =>
                                {
                                    string rfaNameAttri = "Bezeichnung des Bauvorhabens";
                                    p.Name = rfaNameAttri;
                                    p.NominalValue = new IfcLabel("LandBIM Bauvorhaben");
                                }),
                        });                        
                    });
                });

                //now commit the changes, else they will be rolled back at the end of the scope of the using statement
                txn.Commit();
            }
            return model;
        }
    }
}
