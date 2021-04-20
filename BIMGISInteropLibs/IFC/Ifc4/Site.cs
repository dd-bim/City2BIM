using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//embed BimGisCad
using BimGisCad.Representation.Geometry;            //Axis
using BimGisCad.Representation.Geometry.Composed;   //TIN
using BimGisCad.Representation.Geometry.Elementary; //Vector, Points, ...
using BimGisCad.Collections;                        //MESH

//embed Xbim                                    //below selected examples that show why these are included
using Xbim.Ifc;                                 //IfcStore
using Xbim.Ifc4.MeasureResource;                //Enumeration for Unit
using Xbim.Ifc4.ProductExtension;               //IfcSite
using Xbim.Ifc4.Interfaces;                     //IfcElementComposition (ENUM)
using Xbim.Ifc4.GeometryResource;               //Shape
using Xbim.Ifc4.GeometricConstraintResource;    //IfcLocalPlacement

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
                LogWriter.Entries.Add(new LogPair(LogType.verbose, "[IfcSite] Transaction started."));
                var site = model.Instances.New<IfcSite>(s =>
                {
                    //set site name
                    s.Name = name;
                    LogWriter.Entries.Add(new LogPair(LogType.verbose, "[IfcSite] Name ('" + s.Name + "') set."));

                    //set angle
                    s.CompositionType = compositionType;
                    if (refLatitude.HasValue)
                    {
                        s.RefLatitude = IfcCompoundPlaneAngleMeasure.FromDouble(refLatitude.Value);
                        LogWriter.Entries.Add(new LogPair(LogType.verbose, "[IfcSite] Latitude ('" + s.RefLatitude.Value + "') set."));
                    }
                    if (refLongitude.HasValue)
                    {
                        s.RefLongitude = IfcCompoundPlaneAngleMeasure.FromDouble(refLongitude.Value);
                        LogWriter.Entries.Add(new LogPair(LogType.verbose, "[IfcSite] Longitude ('" + s.RefLongitude.Value + "') set."));
                    }

                    s.RefElevation = refElevation;
                    LogWriter.Entries.Add(new LogPair(LogType.verbose, "[IfcSite] Elevation ('" + s.RefElevation.ToString() + "') set."));

                    placement = placement ?? Axis2Placement3D.Standard;

                    //ifc LoGeoRef 30 create with local placement
                    if (loGeoRef == IFC.LoGeoRef.LoGeoRef30)
                    {
                        s.ObjectPlacement = LoGeoRef.Level30.Create(model, placement);
                    }

                });
                txn.Commit();
                LogWriter.Entries.Add(new LogPair(LogType.verbose, "[IfcSite] Transaction commited."));
                LogWriter.Entries.Add(new LogPair(LogType.debug, "[IfcSite] Site created."));
                return site;
            }
        }
    }

    public class Geo
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="siteName"></param>
        /// <param name="jSt"></param>
        /// <param name="loGeoRef"></param>
        /// <param name="sitePlacement"></param>
        /// <param name="tin"></param>
        /// <param name="mesh"></param>
        /// <param name="breaklines"></param>
        /// <param name="surfaceType"></param>
        /// <param name="breakDist"></param>
        /// <param name="refLatitude"></param>
        /// <param name="refLongitude"></param>
        /// <param name="refElevation"></param>
        /// <returns></returns>
        public static IfcStore Create(
             JsonSettings jSt,
             IFC.LoGeoRef loGeoRef,
             Axis2Placement3D sitePlacement,
             Result result,
             SurfaceType surfaceType,
             double? breakDist = null,
             double? refLatitude = null,
             double? refLongitude = null,
              double? refElevation = null)
        {
            //site name
            IfcLabel siteName = jSt.siteName;

            //init model
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "Initalize IfcModel"));
            var model = InitModel.Create(jSt.projectName, jSt.editorsFamilyName, jSt.editorsGivenName, jSt.editorsOrganisationName, out var project);
            
            //site
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "Initalize IfcSite"));
            var site = Site.Create(model, siteName, loGeoRef, sitePlacement, refLatitude, refLongitude, refElevation);
            
            //needed (do not remove or change!)
            RepresentationType representationType;
            RepresentationIdentifier representationIdentifier;

            //set Representation shape
            IfcGeometricRepresentationItem shape;

            //Case discrimination for different shape representations
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
            //write Shape Representation to model
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "Write shape representation to IfcModel..."));
            var repres = ShapeRepresentation.Create(model, shape, representationIdentifier, representationType);
            
            //var terrain = createTerrain(model, "TIN", mesh.Id, null, repres);
            var terrain = Terrain.Create(model, "TIN", null, null, repres);

            //add site to IfcProject entity
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "Add site to IfcProject entity..."));
            
            //start transaction
            using (var txn = model.BeginTransaction("Add Site to Project"))
            {
                LogWriter.Entries.Add(new LogPair(LogType.verbose, "Transaction started."));
                
                //add to entity IfcSite
                site.AddElement(terrain);

                //TODO
                //add local placement
                var lp = terrain.ObjectPlacement as IfcLocalPlacement;
                lp.PlacementRelTo = site.ObjectPlacement;
                
                //add site to IfcProject
                project.AddSite(site);

                //update owner history entity
                model.OwnerHistoryAddObject.CreationDate = DateTime.Now;
                model.OwnerHistoryAddObject.LastModifiedDate = model.OwnerHistoryAddObject.CreationDate;

                //commit otherwise no update / add
                txn.Commit();
                LogWriter.Entries.Add(new LogPair(LogType.verbose, "Transaction commited."));
            }

            //return model
            return model;
        }
    }
}
