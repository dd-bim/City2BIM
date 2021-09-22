using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//embed BimGisCad
using BimGisCad.Representation.Geometry;            //Axis

//embed Xbim                                    //below selected examples that show why these are included
using Xbim.Ifc;                                 //IfcStore
using Xbim.Ifc4.MeasureResource;                //Enumeration for Unit
using Xbim.Ifc4.ProductExtension;               //IfcSite
using Xbim.Ifc4.Interfaces;                     //IfcElementComposition (ENUM)
using Xbim.Ifc4.GeometryResource;               //Shape
using Xbim.Ifc4.RepresentationResource;         //representation res

//embed IfcTerrain logic
using BIMGISInteropLibs.IfcTerrain; //used for handling json settings

//Logging
using BIMGISInteropLibs.Logging;                                 //need for LogPair
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

namespace BIMGISInteropLibs.IFC.Ifc4
{
    public static class Site
    {
        /// <summary>
        /// creates site in project
        /// </summary>
        /// <param name="model">Location for all information that will be inserted into the IFC file</param>
        /// <param name="name">Terrain designation</param>
        /// <param name="placement">Parameter provided by "createLocalPlacement"</param>
        /// <param name="refLatitude">Latitude</param>
        /// <param name="refLongitude">Longitude</param>
        /// <param name="refElevation">Height</param>
        /// <param name="compositionType">DO NOT CHANGE</param>
        /// <returns>IfcSite</returns>
        public static IfcSite Create(IfcStore model,
             string name,
             IFC.LoGeoRef loGeoRef,
             Axis2Placement3D placement = null,
             double? refLatitude = null,
             double? refLongitude = null,
             double? refElevation = null,
             IfcElementCompositionEnum compositionType = IfcElementCompositionEnum.ELEMENT)
        {
            using (var txn = model.BeginTransaction("Create Site"))
            {
                //init model
                LogWriter.Add(LogType.verbose, "[IfcSite] Transaction started.");
                var site = model.Instances.New<IfcSite>(s =>
                {
                    //set site name
                    s.Name = name;
                    LogWriter.Add(LogType.verbose, "[IfcSite] Name ('" + s.Name + "') set.");

                    //set angle
                    s.CompositionType = compositionType;
                    
                    if (refLatitude.HasValue)
                    {
                        s.RefLatitude = IfcCompoundPlaneAngleMeasure.FromDouble(refLatitude.Value);
                        LogWriter.Add(LogType.verbose, "[IfcSite] Latitude ('" + s.RefLatitude.Value + "') set.");
                    }
                    if (refLongitude.HasValue)
                    {
                        s.RefLongitude = IfcCompoundPlaneAngleMeasure.FromDouble(refLongitude.Value);
                        LogWriter.Add(LogType.verbose, "[IfcSite] Longitude ('" + s.RefLongitude.Value + "') set.");
                    }

                    s.RefElevation = refElevation;
                    LogWriter.Add(LogType.verbose, "[IfcSite] Elevation ('" + s.RefElevation.ToString() + "') set.");

                    placement = placement ?? Axis2Placement3D.Standard;

                    //ifc LoGeoRef 30 create with local placement
                    if (loGeoRef == IFC.LoGeoRef.LoGeoRef30)
                    {
                        s.ObjectPlacement = LoGeoRef.Level30.Create(model, placement);
                    }

                });
                txn.Commit();
                LogWriter.Add(LogType.verbose, "[IfcSite] Transaction commited.");
                LogWriter.Add(LogType.debug, "[IfcSite] Site created.");
                return site;
            }
        }
    }

    public class Geo
    {
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

            //init model
            LogWriter.Add(LogType.verbose, "Initalize IfcModel");
            var model = InitModel.Create(config.projectName, config.editorsFamilyName, 
                config.editorsGivenName, config.editorsOrganisationName, out var project);
            
            //site
            LogWriter.Add(LogType.verbose, "Initalize IfcSite");

            //init site as dynamic
            dynamic site = null;

            //init geomRepresContext
            dynamic geomRepContext;

            //site name
            IfcLabel siteName = config.siteName;

            //loop for different LoGeoRef's
            switch (config.logeoref)
            {
                //Level 50 - TODO
                case IFC.LoGeoRef.LoGeoRef50:
                    site = Site.Create(model, siteName, config.logeoref , sitePlacement, refLatitude, refLongitude, refElevation);
                    geomRepContext = LoGeoRef.Level50.Create(model, sitePlacement, config);
                    break;
                //Level 40
                case IFC.LoGeoRef.LoGeoRef40:
                    site = Site.Create(model, siteName, config.logeoref, sitePlacement, refLatitude, refLongitude, refElevation);
                    geomRepContext = LoGeoRef.Level40.Create(model, sitePlacement, config.trueNorth.Value);
                    break;
                //Level 30 DEFAULT
                default:
                    site = Site.Create(model, siteName, config.logeoref, sitePlacement, refLatitude, refLongitude, refElevation);
                    break;
            }

            LogWriter.Add(LogType.verbose, "Entity IfcSite generated.");

            //needed (do not remove or change!)
            RepresentationType representationType;
            RepresentationIdentifier representationIdentifier;

            //set Representation shape
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
            }
            

            //write Shape Representation to model
            LogWriter.Add(LogType.verbose, "Write shape representation to IfcModel...");

            //create IfcShapeRepresentation entity
            var repres = ShapeRepresentation.Create(model, shape, representationIdentifier, representationType);
            
            //add site to IfcProject entity
            LogWriter.Add(LogType.verbose, "Add site to IfcProject entity...");
            
            //start transaction
            using (var txn = model.BeginTransaction("Add Site to Project"))
            {
                var terrain = model.Instances.New<IfcGeographicElement>(s =>
                {
                    //set site name (from user input)
                    s.Name = siteName;

                    //set predefined type to TERRAIN
                    s.PredefinedType = IfcGeographicElementTypeEnum.TERRAIN;

                    //create Identifier (UUID)
                    s.Tag = new IfcIdentifier(Guid.NewGuid().ToString());

                    //
                    s.Representation = model.Instances.New<IfcProductDefinitionShape>(r => r.Representations.Add(repres));

                });

                //
                site.AddElement(terrain);

                //add local placement
                //var lp = terrain.ObjectPlacement as IfcLocalPlacement;

                //add to entity IfcSite
                //site.AddElement(terrain);

                //lp.PlacementRelTo = site.ObjectPlacement;

                //add site to IfcProject
                project.AddSite(site);

                //update owner history entity
                model.OwnerHistoryAddObject.CreationDate = DateTime.Now;
                model.OwnerHistoryAddObject.LastModifiedDate = model.OwnerHistoryAddObject.CreationDate;

                //commit otherwise no update / add
                txn.Commit();
                LogWriter.Add(LogType.verbose, "Transaction commited.");
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

            //return model
            return model;
        }
    }
}
