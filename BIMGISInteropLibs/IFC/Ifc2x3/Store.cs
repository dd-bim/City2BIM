using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//embed BimGisCad
using BimGisCad.Representation.Geometry;
using BimGisCad.Representation.Geometry.Composed;
using BimGisCad.Collections;                    //provides MESH --> will be removed

//embed Xbim                                    //below selected examples that show why these are included
using Xbim.Ifc;                                 //IfcStore
using Xbim.Ifc2x3.GeometryResource;             //IfcAxis2Placement3D
using Xbim.Ifc2x3.MeasureResource;              //Enumeration for Unit
using Xbim.Ifc2x3.RepresentationResource;       //IfcShapeRepresentation

namespace BIMGISInteropLibs.IFC.Ifc2x3
{
    public static class Store
    {
        public static IfcStore CreateViaMesh(
            string projectName,
            string editorsFamilyName,
            string editorsGivenName,
            string editorsOrganisationName,
            IfcLabel siteName,
            Axis2Placement3D sitePlacement,
            Mesh mesh,
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
                case SurfaceType.SBSM:
                    shape = ShellBasedSurfaceModel.CreateViaMesh(model, sitePlacement.Location, mesh, out representationType, out representationIdentifier);
                    break;
                default:
                    shape = GeometricCurveSet.CreateViaMesh(model, sitePlacement.Location, mesh, breakDist, out representationType, out representationIdentifier);
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
