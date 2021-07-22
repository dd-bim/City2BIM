using System;
using System.IO;
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

using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.IO.Step21;

 
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
        public static IfcStore CreateViaTin(
            JsonSettings jSt,
            JsonSettings_DIN_SPEC_91391_2 jsonSettings_DIN_SPEC,
            JsonSettings_DIN_18740_6 jsonSettings_DIN_18740_6,
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
            LogWriter.Add(LogType.verbose, "Entity IfcProject generated.");

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
            LogWriter.Add(LogType.verbose, "Entity IfcSite generated.");
            
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

            //TODO Breaklines
            /*
            using (var txn = model.BeginTransaction("Breaklines"))
            {
                //query if breaklines should be processed
                if (jSt.breakline)
                {
                    //Breakline.Create();

                    txn.Commit();
                }
                else
                {
                    //rollback transaction
                    txn.RollBack();
                }
            }
            */

            //add site entity to model
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
                if (jSt.outIfcPropertySet)
                {
                    //switch between cases for metadata export
                    if (jSt.exportMetadataDin91391)
                    {
                        //Methode to store Metadata according to DIN 91391-2
                        PropertySet.CreatePSetMetaDin91391(model, jsonSettings_DIN_SPEC);
                    }                  
                    //case 2: din 18740
                    if (jSt.exportMetadataDin18740)
                    {
                        //Methode to store Metadata according to DIN 18740-6
                        PropertySet.CreatePSetMetaDin18740(model, jsonSettings_DIN_18740_6);
                    }

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
                        /*
                        using (StreamWriter fileStream = new StreamWriter(jSettings.destFileName))
                        {
                            Save(fileStream, model);
                        }
                        */ 

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

        //below a small "bug fix" for IfcCartesianPointList
        //source: https://github.com/xBimTeam/XbimGeometry/issues/291
        /// <summary>
        /// methode to save file and check for entity length
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="model"></param>
        public static void Save(TextWriter writer, IModel model)
        {
            Part21Writer.WriteHeader(model.Header, writer, "IFC4");
            var metadata = model.Metadata;
            foreach (var instance in model.Instances)
                WriteEntity(instance, writer, metadata);
  
            Part21Writer.WriteFooter(writer);
        }

        /// <summary>
        /// enntity writer & checker
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="output"></param>
        /// <param name="metadata"></param>
        private static void WriteEntity(IPersistEntity entity, TextWriter output, ExpressMetaData metadata)
        {
            var expressType = metadata.ExpressType(entity);
            output.Write("#{0}={1}(", entity.EntityLabel, expressType.ExpressNameUpper);

            var first = true;

            foreach (var ifcProperty in expressType.Properties.Values)
            //only write out persistent attributes, ignore inverses
            {
                if (ifcProperty.EntityAttribute.State == EntityAttributeState.DerivedOverride)
                {
                    if (!first)
                        output.Write(',');
                    output.Write('*');
                    first = false;
                }
                else
                {
                    // workaround for IfcCartesianPointList3D from IFC4x1
                    if (entity is IfcCartesianPointList3D && ifcProperty.Name == "TagList")
                        continue;

                    var propType = ifcProperty.PropertyInfo.PropertyType;
                    var propVal = ifcProperty.PropertyInfo.GetValue(entity, null);
                    if (!first)
                        output.Write(',');
                    Part21Writer.WriteProperty(propType, propVal, output, null, metadata);
                    first = false;
                }
            }
            output.Write(");"+Environment.NewLine);
        }
    }
}
