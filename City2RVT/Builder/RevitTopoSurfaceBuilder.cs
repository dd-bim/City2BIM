using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.ExtensibleStorage;
using BIMGISInteropLibs.Geometry;

using BIMGISInteropLibs.RvtTerrain;

using Autodesk.Revit.UI; //Task dialog

namespace City2RVT.Builder
{
    internal class RevitTopoSurfaceBuilder
    {
        private readonly Document doc;

        /// <summary>
        /// return this value for error handling of import 
        /// </summary>
        public bool terrainImportSuccesful { get { return importSuccesful; } }

        /// <summary>
        /// value of import succesful or not
        /// </summary>
        private bool importSuccesful { set; get; } = false;

        public RevitTopoSurfaceBuilder(Document doc)
        {
            this.doc = doc;
        }

        /// <summary>
        /// function to create DTM via points only
        /// </summary>
        public void createDTMviaPoints(BIMGISInteropLibs.RvtTerrain.Result result)
        {
            //transform input points to revit
            var revDTMpts = transPts(result.dtmPoints);

            //transaction for surface / dtm creation
            using (Transaction t = new Transaction(doc, "Create TopoSurface"))
            {
                //start transaction
                t.Start();
                try
                {
                    //create surf var via points
                    var surface = TopographySurface.Create(doc, revDTMpts);
                    
                    //data storage
                    storeTerrainIDInExtensibleStorage(doc, surface.Id);
                    
                    //pin surface
                    surface.Pinned = true;

                    //commit transaction
                    t.Commit();

                    //set to true
                    importSuccesful = true;

                    result.numPoints = revDTMpts.Count;

                    return;
                }
                catch(Exception ex)
                {
                    //TODO logging

                    //set to false
                    importSuccesful = false;


                    //write exception to console
                    Console.WriteLine(ex);

                    //roll back cause of error
                    t.RollBack();

                    return;
                }
            }
        }

        /// <summary>
        /// method to create DTM using point list and list of facets
        /// </summary>
        public void createDTM(BIMGISInteropLibs.RvtTerrain.Result result)
        {
            //get points from result / exchange class
            var terrainPoints = result.dtmPoints;

            //init facet list
            List<PolymeshFacet> terrainFaces = new List<PolymeshFacet>();

            foreach(BIMGISInteropLibs.RvtTerrain.DtmFace f in result.terrainFaces)
            {
                PolymeshFacet pm = new PolymeshFacet(f.p1, f.p2, f.p3);
                terrainFaces.Add(pm);
            }


            dynamic revDTMpts = null; //initalize

            try
            {
                //transform input points to revit
                revDTMpts = transPts(terrainPoints);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error - Transform points", ex.Message);
            }
            

            //transaction for surface / dtm creation
            using (Transaction t = new Transaction(doc, "Create TopoSurface"))
            {
                //start transaction
                t.Start();
                try
                {
                    //create surf var via points & faces
                    var surface = TopographySurface.Create(doc, revDTMpts, terrainFaces);

                    //data storage
                    storeTerrainIDInExtensibleStorage(doc, surface.Id);

                    //pin surface
                    surface.Pinned = true;

                    if (surface.IsValidObject)
                    {
                        //commit transaction
                        t.Commit();

                        //set to true
                        importSuccesful = true;

                        result.numPoints = revDTMpts.Count;
                        result.numFacets = terrainFaces.Count;
                    }
                    else
                    {
                        //rollback transaction - dtm will not be created
                        t.RollBack();

                        //set to false
                        importSuccesful = false;
                    }

                    return;
                }
                catch (Exception ex)
                {
                    //TODO logging

                    //set to false
                    importSuccesful = false;


                    //write exception to console
                    Console.WriteLine(ex);

                    //roll back cause of error
                    t.RollBack();

                    return;
                }
            }


        }

        /// <summary>
        /// function to transform points to revit crs
        /// </summary>
        private List<XYZ> transPts(List<C2BPoint> terrainPoints)
        {
            //init new list
            var revDTMpts = new List<XYZ>();

            //loop throgh every point in list
            foreach (var pt in terrainPoints)
            {
                //Transformation for revit
                var unprojectedPt = Calc.GeorefCalc.CalcUnprojectedPoint(pt, true);

                //add to new list (as projected point)
                revDTMpts.Add(Revit_Build.GetRevPt(unprojectedPt));
            }

            return revDTMpts;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="terrainID"></param>
        private static void storeTerrainIDInExtensibleStorage(Document doc, ElementId terrainID)
        {
            using (SubTransaction trans = new SubTransaction(doc))
            {
                trans.Start();
                Schema terrainIDSchema = utils.getSchemaByName("HTWDD_TerrainID");

                if (terrainIDSchema == null)
                {
                    SchemaBuilder sb = new SchemaBuilder(Guid.NewGuid());
                    sb.SetSchemaName("HTWDD_TerrainID");
                    sb.SetReadAccessLevel(AccessLevel.Public);
                    sb.SetWriteAccessLevel(AccessLevel.Public);

                    FieldBuilder fb = sb.AddSimpleField("terrainID", typeof(ElementId));
                    fb.SetDocumentation("Field stores element id of TopoGraphySurface of imported DTM");

                    terrainIDSchema = sb.Finish();
                }

                Entity ent = new Entity(terrainIDSchema);
                Field terrainIDField = terrainIDSchema.GetField("terrainID");
                ent.Set<ElementId>(terrainIDField, terrainID);

                DataStorage terrainIDStorage = DataStorage.Create(doc);
                terrainIDStorage.SetEntity(ent);

                trans.Commit();
            }
        }
        /* Spatial Filter
        
        public OGRSpatialFilter SpatialFilter { get => spatialFilter; }

        private OGRSpatialFilter spatialFilter { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public void setSpatialFilter(bool isSquare, Document doc, double distance)
        {
            //
            var pbp = utils.getProjectBasePointMeter(doc);
        
            //
            if (isSquare)
            {
                this.spatialFilter = new OGRRectangularSpatialFilter(pbp.X, pbp.Y, distance, distance);
            }
            else if (!isSquare)
            {
                this.spatialFilter = new OGRCircularSpatialFilter(pbp.X, pbp.Y, distance);
            }
            else
            {
                this.spatialFilter = null;
            }
            return;
        }
        */
    }
}