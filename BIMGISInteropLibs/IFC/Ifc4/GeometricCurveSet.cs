﻿using System.Collections.Generic;
using System.Linq;

//embed BimGisCad
using BimGisCad.Representation.Geometry.Elementary; //provides Vector
using BimGisCad.Representation.Geometry.Composed;   //provides TIN
using BimGisCad.Collections;                        //provides MESH

//embed Xbim                                    //below selected examples that show why these are included
using Xbim.Ifc4.GeometryResource;               //IfcAxis2Placement3D
using Xbim.Ifc4.GeometricModelResource;         //IfcShellBasedSurfaceModel or IfcGeometricCurveSet

//Transfer class for the reader (IFCTerrain)
using BIMGISInteropLibs.IfcTerrain;

//embed logging
using BIMGISInteropLibs.Logging; //access to log writer
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

namespace BIMGISInteropLibs.IFC.Ifc4
{
    /// <summary>
    /// Classes for creating IfcGCS via TIN / MESH
    /// </summary>
    public class GeometricCurveSet
    {
        /// <summary>
        /// Create IfcGCS shape representation (MESH)
        /// </summary>
        public static IfcGeometricCurveSet CreateViaMesh(Xbim.Ifc.IfcStore model, Vector3 origin, Result result,
            double? breakDist,
            out RepresentationType representationType,
            out RepresentationIdentifier representationIdentifier)
        {
            //get MESH from result class
            Mesh mesh = result.Mesh;

            //logging
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "IfcGCS shape representation creation started..."));

            //begin a transaction
            using (var txn = model.BeginTransaction("Create DTM (IfcGCS)"))
            {
                // IfcCartesianPoints create //TODO: filter points that are not included in the DTM
                var cps = mesh.Points.Select(p => model.Instances.New<IfcCartesianPoint>(c => c.SetXYZ(p.X - origin.X, p.Y - origin.Y, p.Z - origin.Z))).ToList();

                //passing to results (points)
                int numPoints = cps.Count;
                result.wPoints = numPoints;

                //passing to results (faces)
                int numEdges = 0;

                // DTM
                var dtm = model.Instances.New<IfcGeometricCurveSet>(g =>
                {
                    var edges = new HashSet<TupleIdx>();
                    g.Elements.AddRange(cps);
                    if (breakDist is double dist)
                    {
                        //Auxiliary function to create points on edge
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

                        //possibly create break lines
                        foreach (var edge in mesh.FixedEdges)
                        {
                            addEdgePoints(mesh.Points[edge.Idx1], mesh.Points[edge.Idx2]);
                            edges.Add(edge);
                        }

                        //Edges of the faces (if present and without duplication)
                        foreach (var edge in mesh.EdgeIndices.Keys)
                        {
                            if (!edges.Contains(TupleIdx.Flipped(edge)) && edges.Add(edge))
                            { addEdgePoints(mesh.Points[edge.Idx1], mesh.Points[edge.Idx2]); }
                        }

                    }
                    else
                    {
                        //possibly create break lines
                        foreach (var edge in mesh.FixedEdges)
                        {
                            g.Elements.Add(model.Instances.New<IfcPolyline>(p => p.Points.AddRange(new[] { cps[edge.Idx1], cps[edge.Idx2] })));
                            edges.Add(edge);
                        }

                        //Edges of the faces (if present and without duplication)
                        foreach (var edge in mesh.EdgeIndices.Keys)
                        {
                            if (!edges.Contains(TupleIdx.Flipped(edge)) && edges.Add(edge))
                            { 
                                g.Elements.Add(model.Instances.New<IfcPolyline>(p => 
                                p.Points.AddRange(new[] 
                                { 
                                    cps[edge.Idx1], 
                                    cps[edge.Idx2] 
                                })));
                            }
                            //count up processed edge
                            numEdges++;
                        }
                    }
                });
                //pass for processing results (log)
                result.wFaces = numEdges / 3;

                //write to remaining output parameter
                representationIdentifier = RepresentationIdentifier.SurveyPoints;
                representationType = RepresentationType.GeometricCurveSet;

                //logging
                LogWriter.Entries.Add(new LogPair(LogType.verbose, "IfcGCS shape representation created."));

                //commit transaction
                txn.Commit();
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
        public static IfcGeometricCurveSet CreateViaTin(Xbim.Ifc.IfcStore model, Vector3 origin, Result result,
            double? breakDist,
            out RepresentationType representationType,
            out RepresentationIdentifier representationIdentifier)
        {
            //get TIN from result class
            Tin tin = result.Tin;

            //logging
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "IfcGCS shape representation creation started..."));

            //begin a transaction
            using (var txn = model.BeginTransaction("Create DTM"))
            {
                //IfcCartesianPoints create //TODO: filter points that are not included in the DTM
                var cps = tin.Points.Select(p => model.Instances.New<IfcCartesianPoint>(c => c.SetXYZ(p.X - origin.X, p.Y - origin.Y, p.Z - origin.Z))).ToList();

                //passing to results (points)
                int numPoints = cps.Count;
                result.wPoints = numPoints;

                //passing to results (faces)
                int numFaces = 0;

                //create IfcGCS instance
                var dtm = model.Instances.New<IfcGeometricCurveSet>(g =>
                {
                    //Create buffer for triangle edges
                    var edges = new HashSet<TupleIdx>();

                    //read out each triangle
                    foreach (var tri in tin.TriangleVertexPointIndizes())
                    {
                        //first edge
                        g.Elements.Add(model.Instances.New<IfcPolyline>(p => p.Points.AddRange(new[] { cps[tri[0]], cps[tri[1]] })));
                    
                        //next edge
                        g.Elements.Add(model.Instances.New<IfcPolyline>(p => p.Points.AddRange(new[] { cps[tri[1]], cps[tri[2]] })));
                        
                        //last edge
                        g.Elements.Add(model.Instances.New<IfcPolyline>(p => p.Points.AddRange(new[] { cps[tri[2]], cps[tri[0]] })));
                            
                        //count up processed edge
                        numFaces++;
                    }
                });
                //pass for processing results (log)
                result.wFaces = numFaces;

                //write two remaining output parameter
                representationIdentifier = RepresentationIdentifier.SurveyPoints;
                representationType = RepresentationType.GeometricCurveSet;

                //logging
                LogWriter.Entries.Add(new LogPair(LogType.verbose, "IfcGCS shape representation created."));

                //commit transaction - ACID (otherwise rollback)
                txn.Commit();
                return dtm;
            }
        }
    }
}
