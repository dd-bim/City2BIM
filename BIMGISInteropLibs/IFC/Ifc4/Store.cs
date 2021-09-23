using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//embed BimGisCad
using BimGisCad.Representation.Geometry;            //Axis

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
        public static IfcStore Create(
            Result result,
            Config config,
            WriteInput writeInput,
            configDin91391 jsonSettings_DIN_SPEC,
            configDin18740 jsonSettings_DIN_18740_6,
            double? refLatitude = null,
            double? refLongitude = null,
            double? refElevation = null)
        {
            Axis2Placement3D sitePlacement = writeInput.Placement;
            SurfaceType surfaceType = writeInput.SurfaceType;

            //create model
            LogWriter.Add(LogType.verbose, "Initalize IfcModel");
            var model = InitModel.Create(config.projectName, config.editorsFamilyName, 
                config.editorsGivenName, config.editorsOrganisationName, out var project);

            //site
            LogWriter.Add(LogType.verbose, "Initalize IfcSite");

            //init site as dynamic
            dynamic site = null;

            //init geomRepresContext
            dynamic geomRepContext;

            //read site name from json settings
            IfcLabel siteName = config.siteName;

            //loop for different Level of Georef
            switch (config.logeoref)
            {
                //Level 50 - TODO
                case IFC.LoGeoRef.LoGeoRef50:
                    site = Site.Create(model, siteName, config.logeoref, sitePlacement, refLatitude, refLongitude, refElevation);
                    geomRepContext = LoGeoRef.Level50.Create(model, sitePlacement, config);
                    break;
                //Level 40
                case IFC.LoGeoRef.LoGeoRef40:
                    site = Site.Create(model, siteName, config.logeoref, sitePlacement, refLatitude, refLongitude, refElevation);
                    geomRepContext = LoGeoRef.Level40.Create(model, sitePlacement, config.trueNorth.Value);
                    break;
                
                default:
                    site = Site.Create(model, siteName, config.logeoref, sitePlacement, refLatitude, refLongitude, refElevation);
                    break;
            }
            LogWriter.Add(LogType.verbose, "Entity IfcSite generated.");

            //needed (do not remove or change!)
            RepresentationType representationType = new RepresentationType();
            RepresentationIdentifier representationIdentifier = new RepresentationIdentifier();
            
            //init geometric representation as null value
            IfcGeometricRepresentationItem shape;

            //distinction which shape representation
            switch (surfaceType)
            {
                //IfcTFS
                case SurfaceType.TFS:
                    shape = TriangulatedFaceSet.Create(model, sitePlacement.Location, result, out representationType, out representationIdentifier);
                    break;

                //IfcSBSM
                case SurfaceType.SBSM:
                    shape = ShellBasedSurfaceModel.Create(model, sitePlacement.Location, result, out representationType, out representationIdentifier);
                    break;

                //IfcGCS
                default:
                    shape = GeometricCurveSet.Create(model, sitePlacement.Location, result, out representationType, out representationIdentifier);
                    break;

                case SurfaceType.TIN:
                    throw new NotImplementedException();
                    //shape = TriangulatedIrregularNetwork.Create(model, sitePlacement.Location, result, out representationType, out representationIdentifier);
                    //break;
            }
            //write Shape Representation to model
            LogWriter.Add(LogType.verbose, "Write shape representation to IfcModel...");

            //create IfcShapeRepresentation entity
            var repres = ShapeRepresentation.Create(model, shape, representationIdentifier, representationType);

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
                if (config.outIfcPropertySet.GetValueOrDefault())
                {
                    //switch between cases for metadata export
                    if (config.exportMetadataDin91391.GetValueOrDefault())
                    {
                        //Methode to store Metadata according to DIN 91391-2
                        PropertySet.CreatePSetMetaDin91391(model, jsonSettings_DIN_SPEC);
                    }                  
                    //case 2: din 18740
                    if (config.exportMetadataDin18740.GetValueOrDefault())
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
