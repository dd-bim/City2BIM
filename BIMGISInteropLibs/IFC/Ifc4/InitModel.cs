//embed Xbim                                    //below selected examples that show why these are included
using Xbim.Ifc;                                 //IfcStore
using Xbim.Ifc4.Kernel;                         //IfcProject
using Xbim.Common.Step21;                       //Enumeration to XbimShemaVersion
using Xbim.IO;                                  //Enumeration to XbimStoreType
using Xbim.Common;                              //ProjectUnits (Hint: support imperial (TODO: check if required)
using Xbim.Ifc4.Interfaces;                     //Enumeration for Unit

namespace BIMGISInteropLibs.IFC.Ifc4
{
    /// <summary>
    /// class to create IfcProject
    /// </summary>
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
                ApplicationDevelopersName = "HTW Dresden [DD BIM]",
                ApplicationFullName = System.Reflection.Assembly.GetExecutingAssembly().FullName,
                ApplicationIdentifier = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                ApplicationVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                EditorsFamilyName = editorsFamilyName,
                EditorsGivenName = editorsGivenName,
                EditorsOrganisationName = editorsOrganisationName
            };
            //write credentials to IfcStore (model)
            var model = IfcStore.Create(credentials, XbimSchemaVersion.Ifc4, XbimStoreType.EsentDatabase);

            //Begin a transaction as all changes to a model are ACID
            using (var txn = model.BeginTransaction("Initialise Model"))
            {
                //create a project
                project = model.Instances.New<IfcProject>();

                //set the units to SI (metres)
                project.Initialize(ProjectUnits.SIUnitsUK);
                
                //set project Name
                project.Name = projectName;

                //set unit for Length to metre
                project.UnitsInContext.SetOrChangeSiUnit(IfcUnitEnum.LENGTHUNIT, IfcSIUnitName.METRE, null);

                //unit for angle remains unchanged and is output as "rad"
                
                //now commit the changes, else they will be rolled back at the end of the scope of the using statement
                txn.Commit();
            }
            return model;
        }
    }
}
