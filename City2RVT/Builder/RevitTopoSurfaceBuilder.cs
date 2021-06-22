using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.ExtensibleStorage;
using BIMGISInteropLibs.Geometry;

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
        /// <param name="terrainPoints"></param>
        public void createDTMviaPoints(BIMGISInteropLibs.RvtTerrain.Result result)
        {
            GUI.DTM2BIM.Terrain_ImportUI cmdTerrain = new GUI.DTM2BIM.Terrain_ImportUI();
            
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
        /// <param name="terrainPoints">point list (xyz)</param>
        /// <param name="terrainFaces"></param>
        public void createDTM(BIMGISInteropLibs.RvtTerrain.Result result)
        {
            //get points from result / exchange class
            var terrainPoints = result.dtmPoints;

            //
            List<PolymeshFacet> terrainFaces = new List<PolymeshFacet>();

            foreach(BIMGISInteropLibs.RvtTerrain.DtmFace f in result.terrainFaces)
            {
                PolymeshFacet pm = new PolymeshFacet(f.p1, f.p2, f.p3);
                terrainFaces.Add(pm);
            }

            //transform input points to revit
            var revDTMpts = transPts(terrainPoints);

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

                    //commit transaction
                    t.Commit();

                    //set to true
                    importSuccesful = true;

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
    }
}