using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//embed BimGisCad
using BimGisCad.Representation.Geometry;            //Axis
using BimGisCad.Representation.Geometry.Composed;   //TIN
using BimGisCad.Representation.Geometry.Elementary;
using BimGisCad.Collections;                        //MESH (will be removed soon tm)

//embed Xbim                                    //below selected examples that show why these are included
using Xbim.Ifc;                                 //IfcStore
using Xbim.Ifc4.MeasureResource;                //Enumeration for Unit
using Xbim.Ifc4.ProductExtension;               //IfcSite
using Xbim.Ifc4.Interfaces;                     //IfcElementComposition (ENUM)
using Xbim.Ifc4.GeometryResource;               //Shape
using Xbim.Ifc4.GeometricConstraintResource;    //IfcLocalPlacement


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
             Axis2Placement3D placement = null,
             double? refLatitude = null,
             double? refLongitude = null,
             double? refElevation = null,
             IfcElementCompositionEnum compositionType = IfcElementCompositionEnum.ELEMENT)
        {
            using (var txn = model.BeginTransaction("Create Site"))
            {
                var site = model.Instances.New<IfcSite>(s =>
                {
                    s.Name = name;
                    s.CompositionType = compositionType;
                    if (refLatitude.HasValue)
                    {
                        s.RefLatitude = IfcCompoundPlaneAngleMeasure.FromDouble(refLatitude.Value);
                    }
                    if (refLongitude.HasValue)
                    {
                        s.RefLongitude = IfcCompoundPlaneAngleMeasure.FromDouble(refLongitude.Value);
                    }
                    s.RefElevation = refElevation;

                    placement = placement ?? Axis2Placement3D.Standard;
                    s.ObjectPlacement = LocalPlacement.Create(model, placement);
                });

                txn.Commit();
                return site;
            }
        }
    }

    public class Geo
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
                case SurfaceType.TFS:
                    shape = TriangulatedFaceSet.CreateViaMesh(model, sitePlacement.Location, mesh, out representationType, out representationIdentifier);
                    break;
                case SurfaceType.SBSM:
                    shape = ShellBasedSurfaceModel.CreateViaMesh(model, sitePlacement.Location, mesh, out representationType, out representationIdentifier);
                    break;
                default:
                    shape = GeometricCurveSet.CreateViaMesh(model, sitePlacement.Location, mesh, breakDist, out representationType, out representationIdentifier);
                    break;
            }
            var repres = ShapeRepresentation.Create(model, shape, representationIdentifier, representationType);
            
            var terrain = Terrain.Create(model, "TIN", null, null, repres);

            using (var txn = model.BeginTransaction("Add Site to Project"))
            {
                site.AddElement(terrain);
                var lp = terrain.ObjectPlacement as IfcLocalPlacement;
                lp.PlacementRelTo = site.ObjectPlacement;
                project.AddSite(site);

                model.OwnerHistoryAddObject.CreationDate = DateTime.Now;
                model.OwnerHistoryAddObject.LastModifiedDate = model.OwnerHistoryAddObject.CreationDate;

                txn.Commit();
            }

            return model;
        }

        public static IfcStore CreateViaTin(
             string projectName,
             string editorsFamilyName,
             string editorsGivenName,
             string editorsOrganisationName,
             //double? minDist,
             IfcLabel siteName,
             Axis2Placement3D sitePlacement,
             Tin tin,
             Dictionary<int, Line3> breaklines,
             SurfaceType surfaceType,
             double? breakDist = null,
              double? refLatitude = null,
              double? refLongitude = null,
              double? refElevation = null)
        {
            var model = InitModel.Create(projectName, editorsFamilyName, editorsGivenName, editorsOrganisationName, out var project);
            //var model = createandInitModel(projectName, editorsFamilyName, editorsGivenName, editorsOrganisationName, out var project);

            var site = Site.Create(model, siteName, sitePlacement, refLatitude, refLongitude, refElevation);
            RepresentationType representationType;
            RepresentationIdentifier representationIdentifier;
            IfcGeometricRepresentationItem shape;
            switch (surfaceType)
            {
                case SurfaceType.TFS:
                    shape = TriangulatedFaceSet.CreateViaTin(model, sitePlacement.Location, tin, out representationType, out representationIdentifier);
                    break;
                case SurfaceType.SBSM:
                    shape = ShellBasedSurfaceModel.CreateViaTin(model, sitePlacement.Location, tin, out representationType, out representationIdentifier);
                    break;
                default:
                    shape = GeometricCurveSet.CreateViaTin(model, sitePlacement.Location, tin, breakDist, out representationType, out representationIdentifier);
                    break;
            }
            var repres = ShapeRepresentation.Create(model, shape, representationIdentifier, representationType);
            //var terrain = createTerrain(model, "TIN", mesh.Id, null, repres);
            var terrain = Terrain.Create(model, "TIN", null, null, repres);

            using (var txn = model.BeginTransaction("Add Site to Project"))
            {
                site.AddElement(terrain);
                var lp = terrain.ObjectPlacement as IfcLocalPlacement;
                lp.PlacementRelTo = site.ObjectPlacement;
                project.AddSite(site);

                model.OwnerHistoryAddObject.CreationDate = DateTime.Now;
                model.OwnerHistoryAddObject.LastModifiedDate = model.OwnerHistoryAddObject.CreationDate;

                txn.Commit();
            }

            return model;
        }
    }
}
