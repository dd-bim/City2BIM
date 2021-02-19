﻿using System;
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
using Xbim.Ifc4.MeasureResource;                //IfcLabel
using Xbim.Ifc4.ProductExtension;               //IfcSite
using Xbim.Ifc4.GeometricModelResource;         //IfcShellBasedSurfaceModel or IfcGeometricCurveSet
using Xbim.Ifc4.TopologyResource;               //IfcOpenShell
using Xbim.Ifc4.RepresentationResource;         //IfcShapeRepresentation


namespace BIMGISInteropLibs.IFC.Ifc4
{
    class Model
    {
        /// <summary>
        /// 
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
            string projectName,
            string editorsFamilyName,
            string editorsGivenName,
            string editorsOrganisationName,
            IfcLabel siteName,
            Axis2Placement3D sitePlacement,
            Tin tin,
            SurfaceType surfaceType,
            double? breakDist = null,
             double? refLatitude = null,
             double? refLongitude = null,
             double? refElevation = null)
        {
            var model = InitModel.Create(projectName, editorsFamilyName, editorsGivenName, editorsOrganisationName, out var project);
            var site = Site.Create(model, siteName, sitePlacement, refLatitude, refLongitude, refElevation);
            RepresentationType representationType;
            RepresentationIdentifier representationIdentifier;
            IfcGeometricRepresentationItem shape;
            switch (surfaceType)
            {
                case SurfaceType.TFS:
                    return null;
                case SurfaceType.SBSM:
                    shape = ShellBasedSurfaceModel.CreateViaTin(model, sitePlacement.Location, tin, out representationType, out representationIdentifier);
                    break;
                default:
                    shape = GeometricCurveSet.CreateViaTin(model, sitePlacement.Location, tin, breakDist, out representationType, out representationIdentifier);
                    break;
            }
            var repres = ShapeRepresentation.Create(model, shape, representationIdentifier, representationType);

            using (var txn = model.BeginTransaction("Add Site to Project"))
            {
                site.Representation = model.Instances.New<IfcProductDefinitionShape>(r => r.Representations.Add(repres));
                project.AddSite(site);

                model.OwnerHistoryAddObject.CreationDate = DateTime.Now;
                model.OwnerHistoryAddObject.LastModifiedDate = model.OwnerHistoryAddObject.CreationDate;

                txn.Commit();
            }

            return model;
        }

    }
}
