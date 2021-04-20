using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//embed BimGisCad
using BimGisCad.Representation.Geometry;            //Axis
using BimGisCad.Representation.Geometry.Composed;   //TIN
using BimGisCad.Collections;                        //MESH

//embed Xbim                                    //below selected examples that show why these are included
using Xbim.Ifc;                                 //IfcStore
using Xbim.Ifc4.GeometryResource;               //IfcAxis2Placement3D
using Xbim.Ifc4.MeasureResource;                //IfcLabel
using Xbim.Ifc4.RepresentationResource;         //IfcShapeRepresentation
using Xbim.IO;                                  //StorageType
 
//embed IfcTerrain logic
using BIMGISInteropLibs.IfcTerrain; //used for handling json settings

//embed for Logging
using BIMGISInteropLibs.Logging;                                 //need for LogPair
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

namespace BIMGISInteropLibs.IFC.Ifc4
{
    public class Store
    {
        /// <summary>
        /// Building Model Method
        /// </summary>
        /// <param name="projectName"></param>
        /// <param name="editorsFamilyName"></param>
        /// <param name="editorsGivenName"></param>
        /// <param name="editorsOrganisationName"></param>
        /// <param name="siteName"></param>
        /// <param name="sitePlacement"></param>
        /// <param name="tin"></param>
        /// <param name="surfaceType"></param>
        /// <param name="breakDist"></param>
        /// <param name="refLatitude"></param>
        /// <param name="refLongitude"></param>
        /// <param name="refElevation"></param>
        /// <returns></returns>
        public static IfcStore CreateViaTin(
            JsonSettings jSt,
            JsonSettings_DIN_SPEC_91391_2 jsonSettings_DIN_SPEC,
            IFC.LoGeoRef loGeoRef,
            Axis2Placement3D sitePlacement,
            Result result,
            SurfaceType surfaceType,
            double? breakDist = null,
            double? refLatitude = null,
            double? refLongitude = null,
            double? refElevation = null)
        {
            //create model
            var model = InitModel.Create(jSt.projectName, jSt.editorsFamilyName, jSt.editorsGivenName, jSt.editorsOrganisationName, out var project);
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "Entity IfcProject generated."));

            //init site as dynamic
            dynamic site = null;

            //init geomRepresContext
            dynamic geomRepContext;

            //read site name from json settings
            IfcLabel siteName = jSt.siteName;

            //loop for different Level of Georef
            //[TODO] may clean up code
            switch (loGeoRef)
            {
                //Level 50 - TODO
                case IFC.LoGeoRef.LoGeoRef50:
                    site = Site.Create(model, siteName, loGeoRef, sitePlacement, refLatitude, refLongitude, refElevation);
                    geomRepContext = LoGeoRef.Level50.Create(model, sitePlacement, jSt);
                    break;
                //Level 40
                case IFC.LoGeoRef.LoGeoRef40:
                    site = Site.Create(model, siteName, loGeoRef, sitePlacement, refLatitude, refLongitude, refElevation);
                    geomRepContext = LoGeoRef.Level40.Create(model, sitePlacement, jSt.trueNorth);
                    break;
                //Level 30 DEFAULT
                default:
                    site = Site.Create(model, siteName, loGeoRef, sitePlacement, refLatitude, refLongitude, refElevation);
                    break;
            }
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "Entity IfcSite generated."));
            
            RepresentationType representationType;
            RepresentationIdentifier representationIdentifier;
            
            //init geometric representation
            IfcGeometricRepresentationItem shape;

            //distinction whether TIN (true) or MESH (false)
            if (jSt.isTin) //TIN processing
            {
                //distinction which shape representation
                switch (surfaceType)
                {
                    //IfcTFS
                    case SurfaceType.TFS:
                        shape = TriangulatedFaceSet.CreateViaTin(model, sitePlacement.Location, result, out representationType, out representationIdentifier);
                        break;
                    //IfcSBSM
                    case SurfaceType.SBSM:
                        shape = ShellBasedSurfaceModel.CreateViaTin(model, sitePlacement.Location, result, out representationType, out representationIdentifier);
                        break;
                    //IfcGCS (default)
                    default:
                        shape = GeometricCurveSet.CreateViaTin(model, sitePlacement.Location, result, breakDist, out representationType, out representationIdentifier);
                        break;
                }
            }
            else //MESH processing
            {
                //distinction which shape representation
                switch (surfaceType)
                {
                    //IfcTFS
                    case SurfaceType.TFS:
                        shape = TriangulatedFaceSet.CreateViaMesh(model, sitePlacement.Location, result, out representationType, out representationIdentifier);
                        break;
                    //IfcSBSM
                    case SurfaceType.SBSM:
                        shape = ShellBasedSurfaceModel.CreateViaMesh(model, sitePlacement.Location, result, out representationType, out representationIdentifier);
                        break;
                    //IfcGCS (default)
                    default:
                        shape = GeometricCurveSet.CreateViaMesh(model, sitePlacement.Location, result, breakDist, out representationType, out representationIdentifier);
                        break;
                }
            }
            //create IfcShapeRepresentation entity
            var repres = ShapeRepresentation.Create(model, shape, representationIdentifier, representationType);

            //add site entity to model
            using (var txn = model.BeginTransaction("Add Site to Project"))
            {
                //get site entity
                site.Representation = model.Instances.New<IfcProductDefinitionShape>(r => r.Representations.Add(repres));

                //add site to project
                project.AddSite(site);
                LogWriter.Entries.Add(new LogPair(LogType.verbose, "IfcShapeRepresentation add to IfcSite."));

                //modfiy owner history
                model.OwnerHistoryAddObject.CreationDate = DateTime.Now;
                model.OwnerHistoryAddObject.LastModifiedDate = model.OwnerHistoryAddObject.CreationDate;
                LogWriter.Entries.Add(new LogPair(LogType.verbose, "Entity IfcOwnerHistory updated."));

                //commit otherwise would not update / add
                txn.Commit();
            }

            //TODO: read out properties of the metadata dynamic
            //start transaction to create property set
            using (var txn = model.BeginTransaction("Ifc Property Set"))
            {
                //Query if metadata should be exported as IfcPropertySet?
                if (jSt.outIfcPropertySet)
                {
                    //Methode to store Metadata from DIN 91391-2
                    PropertySet.CreatePSetMetaDin91391(model, jsonSettings_DIN_SPEC);

                    //commit transaction
                    txn.Commit();
                }
                else
                {
                    //rollback transaction not need to store pr
                    txn.RollBack();
                }
            }
            
            return model;
        }

        /// <summary>
        /// this method write the dest file <para/>
        /// supports STEP, XML, ZIP
        /// </summary>
        public static void WriteFile(IfcStore model, JsonSettings jSettings)
        {
            switch (jSettings.outFileType)
            {
                //if it is to be saved as an STEP file
                case IfcFileType.Step:
                    try
                    {
                        model.SaveAs(jSettings.destFileName, StorageType.Ifc);
                        LogWriter.Entries.Add(new LogPair(LogType.verbose, "IFC file (as '" + jSettings.outFileType.ToString() + "') generated."));
                    }
                    catch (Exception ex)
                    {
                        LogWriter.Entries.Add(new LogPair(LogType.error, "IFC file (as '" + jSettings.outFileType.ToString() + "') could not be generated.\nError message: " + ex));
                    }
                    break;

                //if it is to be saved as an XML file
                case IfcFileType.ifcXML:
                    try
                    {
                        model.SaveAs(jSettings.destFileName, StorageType.IfcXml);
                        LogWriter.Entries.Add(new LogPair(LogType.verbose, "IFC file (as '" + jSettings.outFileType.ToString() + "') generated."));
                    }
                    catch (Exception ex)
                    {
                        LogWriter.Entries.Add(new LogPair(LogType.error, "IFC file (as '" + jSettings.outFileType.ToString() + "') could not be generated.\nError message: " + ex));
                    }
                    break;

                //if it is to be saved as an ifcZIP file
                case IfcFileType.ifcZip:
                    try
                    {
                        model.SaveAs(jSettings.destFileName, StorageType.IfcZip);
                        LogWriter.Entries.Add(new LogPair(LogType.verbose, "IFC file (as '" + jSettings.outFileType.ToString() + "') generated."));
                    }
                    catch (Exception ex)
                    {
                        LogWriter.Entries.Add(new LogPair(LogType.error, "IFC file (as '" + jSettings.outFileType.ToString() + "') could not be generated.\nError message: " + ex));
                    }
                    break;
            }
        }
    }
}
