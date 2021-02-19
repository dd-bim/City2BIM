using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//embed BimGisCad
using BimGisCad.Representation.Geometry.Elementary;
using BimGisCad.Representation.Geometry.Composed;
using BimGisCad.Collections;                    //provides MESH --> will be removed

//embed Xbim                                    //below selected examples that show why these are included
using Xbim.Ifc;                                 //IfcStore
using Xbim.Ifc4.GeometryResource;               //IfcAxis2Placement3D
using Xbim.Ifc4.GeometricModelResource;         //IfcShellBasedSurfaceModel or IfcGeometricCurveSet


namespace BIMGISInteropLibs.IFC.Ifc4
{
    /// <summary>
    /// Class for creating GCS via MESH (which will be removed) and TIN
    /// </summary>
    public class GeometricCurveSet
    {
        /// <summary>
        /// Create GCS using MESH (will be removed)
        /// </summary>
        /// <param name="model"></param>
        /// <param name="origin"></param>
        /// <param name="mesh"></param>
        /// <param name="breakDist"></param>
        /// <param name="representationType"></param>
        /// <param name="representationIdentifier"></param>
        /// <returns></returns>
        public static IfcGeometricCurveSet CreateViaMesh(Xbim.Ifc.IfcStore model, Vector3 origin, Mesh mesh,
            double? breakDist,
            out RepresentationType representationType,
            out RepresentationIdentifier representationIdentifier)
        {

            //begin a transaction
            using (var txn = model.BeginTransaction("Create DTM"))
            {

                // CartesianPoints erzeugen
                var cps = mesh.Points.Select(p => model.Instances.New<IfcCartesianPoint>(c => c.SetXYZ(p.X - origin.X, p.Y - origin.Y, p.Z - origin.Z))).ToList();

                // DTM
                var dtm = model.Instances.New<IfcGeometricCurveSet>(g =>
                {
                    var edges = new HashSet<TupleIdx>();
                    g.Elements.AddRange(cps);
                    if (breakDist is double dist)
                    {
                        // Hilfsfunktion zum Punkte auf Kante erzeugen
                        void addEdgePoints(Point3 start, Point3 dest)
                        {
                            var dir = dest - start;
                            double len = Vector3.Norm(dir);
                            double fac = len / dist;
                            if (fac > 1.0)
                            {
                                start -= origin;
                                dir /= len;
                                double currLen = dist;
                                while (currLen < len)
                                {
                                    var p = start + (dir * currLen);
                                    g.Elements.Add(model.Instances.New<IfcCartesianPoint>(c => c.SetXYZ(p.X, p.Y, p.Z)));
                                    currLen += dist;
                                }
                            }
                        }

                        // evtl. Bruchlinien erzeugen
                        foreach (var edge in mesh.FixedEdges)
                        {
                            addEdgePoints(mesh.Points[edge.Idx1], mesh.Points[edge.Idx2]);
                            edges.Add(edge);
                        }

                        // Kanten der Faces (falls vorhanden und ohne Doppelung)
                        foreach (var edge in mesh.EdgeIndices.Keys)
                        {
                            if (!edges.Contains(TupleIdx.Flipped(edge)) && edges.Add(edge))
                            { addEdgePoints(mesh.Points[edge.Idx1], mesh.Points[edge.Idx2]); }
                        }

                    }
                    else
                    {
                        // evtl. Bruchlinien erzeugen
                        foreach (var edge in mesh.FixedEdges)
                        {
                            g.Elements.Add(model.Instances.New<IfcPolyline>(p => p.Points.AddRange(new[] { cps[edge.Idx1], cps[edge.Idx2] })));
                            edges.Add(edge);
                        }

                        // Kanten der Faces (falls vorhanden und ohne Doppelung)
                        foreach (var edge in mesh.EdgeIndices.Keys)
                        {
                            if (!edges.Contains(TupleIdx.Flipped(edge)) && edges.Add(edge))
                            { g.Elements.Add(model.Instances.New<IfcPolyline>(p => p.Points.AddRange(new[] { cps[edge.Idx1], cps[edge.Idx2] }))); }
                        }
                    }
                });

                txn.Commit();
                representationIdentifier = RepresentationIdentifier.SurveyPoints;
                representationType = RepresentationType.GeometricCurveSet;
                return dtm;
            }
        }

        /// <summary>
        /// Creates a DTM via IfcGCS (processing a tin)
        /// </summary>
        /// <param name="model">Location for all information that will be inserted into the IFC file</param>
        /// <param name="origin">Provided by IfcLocalPlacement</param>
        /// <param name="tin">Provided by the different terrain readers</param>
        /// <param name="breakDist"></param>
        /// <param name="representationType">Output - do not change</param>
        /// <param name="representationIdentifier">Output - do not change</param>
        /// <returns>Shape which is written in the IFC file</returns>
        public static IfcGeometricCurveSet CreateViaTin(Xbim.Ifc.IfcStore model, Vector3 origin, Tin tin,
            double? breakDist,
            out RepresentationType representationType,
            out RepresentationIdentifier representationIdentifier)
        {
            //init Logger
            //Logger logger = LogManager.GetCurrentClassLogger();

            //begin a transaction
            using (var txn = model.BeginTransaction("Create DTM"))
            {
                //IfcCartesianPoints create //TODO: filter points that are not included in the DTM
                var cps = tin.Points.Select(p => model.Instances.New<IfcCartesianPoint>(c => c.SetXYZ(p.X - origin.X, p.Y - origin.Y, p.Z - origin.Z))).ToList();

                //create IfcGCS instance
                var dtm = model.Instances.New<IfcGeometricCurveSet>(g =>
                {
                    //Create buffer for triangle edges
                    var edges = new HashSet<TupleIdx>();
                    g.Elements.AddRange(cps);
                    if (breakDist is double dist)
                    {
                        /* EDIT - is still the functionality from MESH
                        // Hilfsfunktion zum Punkte auf Kante erzeugen
                        void addEdgePoints(Point3 start, Point3 dest)
                        {
                            var dir = dest - start;
                            double len = Vector3.Norm(dir);
                            double fac = len / dist;
                            if (fac > 1.0)
                            {
                                start -= origin;
                                dir /= len;
                                double currLen = dist;
                                while (currLen < len)
                                {
                                    var p = start + (dir * currLen);
                                    g.Elements.Add(model.Instances.New<IfcCartesianPoint>(c => c.SetXYZ(p.X, p.Y, p.Z)));
                                    currLen += dist;
                                }
                            }
                        }
                        /*
                        // evtl. Bruchlinien erzeugen
                        foreach (var edge in mesh.FixedEdges)
                        {
                            addEdgePoints(mesh.Points[edge.Idx1], mesh.Points[edge.Idx2]);
                            edges.Add(edge);
                        }

                        // Kanten der Faces (falls vorhanden und ohne Doppelung)
                        foreach (var edge in mesh.EdgeIndices.Keys)
                        {
                            if (!edges.Contains(TupleIdx.Flipped(edge)) && edges.Add(edge))
                            { addEdgePoints(mesh.Points[edge.Idx1], mesh.Points[edge.Idx2]); }
                        }
                        */

                    }
                    else
                    {
                        //read out each triangle
                        foreach (var tri in tin.TriangleVertexPointIndizes())
                        {
                            //first edge
                            g.Elements.Add(model.Instances.New<IfcPolyline>(p => p.Points.AddRange(new[] { cps[tri[0]], cps[tri[1]] })));
                            //next edge
                            g.Elements.Add(model.Instances.New<IfcPolyline>(p => p.Points.AddRange(new[] { cps[tri[1]], cps[tri[2]] })));
                            //last edge
                            g.Elements.Add(model.Instances.New<IfcPolyline>(p => p.Points.AddRange(new[] { cps[tri[2]], cps[tri[0]] })));
                        }
                    }
                });
                //Number of triangle edges - used for logging only
                int numEdges = dtm.Elements.Count - cps.Count;

                //ersetzen
                //logger.Debug("Processed: " + cps.Count + " points; " + numEdges + " edges (of " + numEdges / 3 + " triangels)"); //nach dem commit von txn loggen .. nur für Debugging hier stehen lassen

                //commit transaktion - ACID (otherwise rollback)
                txn.Commit();
                representationIdentifier = RepresentationIdentifier.SurveyPoints;
                representationType = RepresentationType.GeometricCurveSet;
                return dtm;
            }
        }
    }
}
