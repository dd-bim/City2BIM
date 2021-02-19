using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//embed BimGisCad
using BimGisCad.Representation.Geometry;
using BimGisCad.Representation.Geometry.Elementary;
using BimGisCad.Representation.Geometry.Composed;
using BimGisCad.Collections;                    //provides MESH --> will be removed

//embed Xbim                                    //below selected examples that show why these are included
using Xbim.Ifc;                                 //IfcStore
using Xbim.Ifc4.GeometricConstraintResource;    //IfcLocalPlacement
using Xbim.Ifc4.GeometryResource;               //IfcAxis2Placement3D
using Xbim.Ifc4.Kernel;                         //IfcProject
using Xbim.Common.Step21;                       //Enumeration to XbimShemaVersion
using Xbim.IO;                                  //Enumeration to XbimStoreType
using Xbim.Common;                              //ProjectUnits (Hint: support imperial (TODO: check if required)
using Xbim.Ifc4.Interfaces;                     //Enumeration for Unit
using Xbim.Ifc4.ProductExtension;               //IfcSite
using Xbim.Ifc4.GeometricModelResource;         //IfcShellBasedSurfaceModel or IfcGeometricCurveSet
using Xbim.Ifc4.TopologyResource;               //IfcOpenShell
using Xbim.Ifc4.RepresentationResource;         //IfcShapeRepresentation

namespace BIMGISInteropLibs.IFC.Ifc4
{
    public static class InitModel
    {
        /// <summary>
        /// Initializes an empty project
        /// </summary>
        /// <param name="projectName">Titel of project</param>
        /// <param name="editorsFamilyName">family name</param>
        /// <param name="editorsGivenName">given name</param>
        /// <param name="editorsOrganisationName">organisation</param>
        /// <param name="project">Return parameter for further processing</param>
        /// <returns>IfcProject</returns>
        public static IfcStore Create(string projectName,
            string editorsFamilyName,
            string editorsGivenName,
            string editorsOrganisationName,
            out IfcProject project)
        {
            //first we need to set up some credentials for ownership of data in the new model
            var credentials = new XbimEditorCredentials
            {
                ApplicationDevelopersName = "HTW Dresden for DDBIM",
                ApplicationFullName = System.Reflection.Assembly.GetExecutingAssembly().FullName,
                ApplicationIdentifier = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                ApplicationVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                EditorsFamilyName = editorsFamilyName,
                EditorsGivenName = editorsGivenName,
                EditorsOrganisationName = editorsOrganisationName
            };
            //write credentials to IfcStore (model)
            var model = IfcStore.Create(credentials, XbimSchemaVersion.Ifc2X3, XbimStoreType.EsentDatabase);

            //Begin a transaction as all changes to a model are ACID
            using (var txn = model.BeginTransaction("Initialise Model"))
            {
                //create a project
                project = model.Instances.New<IfcProject>();

                //set the units to SI (metres)
                project.Initialize(ProjectUnits.SIUnitsUK);
                
                //set project Name
                project.Name = projectName;

                project.UnitsInContext.SetOrChangeSiUnit(IfcUnitEnum.LENGTHUNIT, IfcSIUnitName.METRE, null);
                //now commit the changes, else they will be rolled back at the end of the scope of the using statement
                txn.Commit();
            }
            return model;
        }
    }
}
