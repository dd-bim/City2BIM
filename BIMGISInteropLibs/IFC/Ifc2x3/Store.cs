//embed BimGisCad
using BimGisCad.Representation.Geometry;            //Axis
//embed IfcTerrain logic
using BIMGISInteropLibs.IfcTerrain;
//embed for Logging
using BIMGISInteropLibs.Logging;                                 //need for LogPair
using System;
//embed Xbim                                    //below selected examples that show why these are included
using Xbim.Ifc;                                 //IfcStore
using Xbim.Ifc2x3.GeometryResource;             //IfcAxis2Placement3D
using Xbim.Ifc2x3.MeasureResource;              //Enumeration for Unit
using Xbim.Ifc2x3.RepresentationResource;       //IfcShapeRepresentation
using Xbim.IO;                                  //StorageType
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

namespace BIMGISInteropLibs.IFC.Ifc2x3
{
    public static class Store
    {
        /// <summary>
        /// Building Model Method
        /// </summary>
        /// <param name="siteName"></param>
        /// <param name="jSt"></param>
        /// <param name="sitePlacement"></param>
        /// <param name="tin"></param>
        /// <param name="mesh"></param>
        /// <param name="surfaceType"></param>
        /// <param name="breakDist"></param>
        /// <param name="refLatitude"></param>
        /// <param name="refLongitude"></param>
        /// <param name="refElevation"></param>
        /// <returns></returns>
        public static IfcStore CreateViaTin(
            JsonSettings jSt,
            JsonSettings_DIN_SPEC_91391_2 jsonSettings_DIN_SPEC,
            JsonSettings_DIN_18740_6 jsonSettings_DIN_18740_6,
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
            LogWriter.Add(LogType.verbose, "Entity IfcProject generated.");

            //site name
            IfcLabel siteName = jSt.siteName;

            IFC.LoGeoRef loGeoRef = jSt.logeoref;

            //init site as dynamic
            dynamic site = null;

            //loop for different Level of GeoRef
            switch (loGeoRef)
            {
                //Level 50 --> NOT SUPPORTED!

                //Level 40
                case IFC.LoGeoRef.LoGeoRef40:
                    site = Site.Create(model, siteName, loGeoRef, sitePlacement, refLatitude, refLongitude, refElevation);
                    var geomRepContext = LoGeoRef.Level40.Create(model, sitePlacement);
                    break;

                //Level 30
                default: site = Site.Create(model, siteName, loGeoRef, sitePlacement, refLatitude, refLongitude, refElevation);
                    break;

            }
            LogWriter.Add(LogType.verbose, "Entity IfcSite generated.");

            //init
            RepresentationType representationType;
            RepresentationIdentifier representationIdentifier;
            
            //init geometric representation (entity IfcShapeRepresentation)
            IfcGeometricRepresentationItem shape;

            //distinction whether TIN (true) or MESH (false)
            if (jSt.isTin) //TIN processing
            {
                //distinction which shape representation
                switch (surfaceType)
                {
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

            //create IfcShapeRepres
            var repres = ShapeRepresentation.Create(model, shape, representationIdentifier, representationType);
            LogWriter.Add(LogType.verbose, "Entity IfcShapeRepresentation generated.");

            //add site entity to ifc project (model)
            using (var txn = model.BeginTransaction("Add Site to Project"))
            {
                //get site entity
                site.Representation = model.Instances.New<IfcProductDefinitionShape>(r => r.Representations.Add(repres));
                
                //add site to project
                project.AddSite(site);
                LogWriter.Add(LogType.verbose, "IfcShapeRepresentation add to IfcSite.");

                //modfiy owner history
                model.OwnerHistoryAddObject.CreationDate = DateTime.Now;
                model.OwnerHistoryAddObject.LastModifiedDate = model.OwnerHistoryAddObject.CreationDate;
                LogWriter.Add(LogType.verbose, "Entity IfcOwnerHistory updated.");

                //commit otherwise would not update / add
                txn.Commit();
            }

            //start transaction to create property set
            using (var txn = model.BeginTransaction("Ifc Property Set"))
            {
                //Query if metadata should be exported as IfcPropertySet?
                if (jSt.outIfcPropertySet.GetValueOrDefault())
                {
                    //Methode to store Metadata according to DIN 91391-2
                    PropertySet.CreatePSetMetaDin91391(model, jsonSettings_DIN_SPEC);

                    //Methode to store Metadata according to DIN 18740-6
                    PropertySet.CreatePSetMetaDin18740(model, jsonSettings_DIN_18740_6);

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
                        LogWriter.Add(LogType.verbose, "IFC file (as '" + jSettings.outFileType.ToString() + "') generated.");
                    }
                    catch (Exception ex)
                    {
                        LogWriter.Add(LogType.error, "IFC file (as '" + jSettings.outFileType.ToString() + "') could not be generated.\nError message: " + ex);
                    }
                    break;

                //if it is to be saved as an XML file
                case IfcFileType.ifcXML:
                    try
                    {
                        model.SaveAs(jSettings.destFileName, StorageType.IfcXml);
                        LogWriter.Add(LogType.verbose, "IFC file (as '" + jSettings.outFileType.ToString() + "') generated.");
                    }
                    catch (Exception ex)
                    {
                        LogWriter.Add(LogType.error, "IFC file (as '" + jSettings.outFileType.ToString() + "') could not be generated.\nError message: " + ex);
                    }
                    break;

                //if it is to be saved as an ifcZIP file
                case IfcFileType.ifcZip:
                    try
                    {
                        model.SaveAs(jSettings.destFileName, StorageType.IfcZip);
                        LogWriter.Add(LogType.verbose, "IFC file (as '" + jSettings.outFileType.ToString() + "') generated.");
                    }
                    catch (Exception ex)
                    {
                        LogWriter.Add(LogType.error, "IFC file (as '" + jSettings.outFileType.ToString() + "') could not be generated.\nError message: " + ex);
                    }
                    break;
            }
        }
    }
}
