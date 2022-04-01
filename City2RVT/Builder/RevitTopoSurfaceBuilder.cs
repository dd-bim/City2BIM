using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.ExtensibleStorage;
using BIMGISInteropLibs.Geometry;

using Autodesk.Revit.UI; //Task dialog
using geom = NetTopologySuite.Geometries;


using BIMGISInteropLibs.IfcTerrain; // for dtm processing result handling

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
        public void createDTMviaPoints(BIMGISInteropLibs.IfcTerrain.Result result)
        {
            //transform input points to revit
            var revDTMpts = transPts(result.pointList);

            //transaction for surface / dtm creation
            using (Transaction t = new Transaction(doc, "Create TopoSurface"))
            {
                //start transaction
                t.Start();
                try
                {
                    //create surf var via points
                    var surface = TopographySurface.Create(doc, revDTMpts);

                    if (surface.IsValidObject)
                    {
                        //data storage
                        storeTerrainIDInExtensibleStorage(doc, surface.Id);

                        //pin surface
                        surface.Pinned = true;

                        //commit transaction
                        t.Commit();

                        //set to true
                        importSuccesful = true;

                        //for logging / user feedback
                        GUI.Cmd_ReadTerrain.numPoints = revDTMpts.Count;

                        return;
                    }
                    else
                    {
                        t.RollBack();

                        return;
                    }
                    
                }
                catch(Exception ex)
                {
                    //TODO logging

                    //Task dialog
                    TaskDialog.Show("DGM error", ex.Message);

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
        public void createDTM(BIMGISInteropLibs.IfcTerrain.Result result)
        {
            //init facet list
            List<PolymeshFacet> terrainFaces = new List<PolymeshFacet>();

            foreach(var face in result.triMap)
            {
                PolymeshFacet pm = new PolymeshFacet(face.triValues[0], face.triValues[1], face.triValues[2]);
                terrainFaces.Add(pm);
            }


            dynamic revDTMpts = null; //initalize

            try
            {
                //transform triangulated nts coords to revit
                revDTMpts = transCoords(result.coordinateList);
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

                    if (surface.IsValidObject)
                    {
                        //data storage
                        storeTerrainIDInExtensibleStorage(doc, surface.Id);

                        //pin surface
                        surface.Pinned = true;

                        //commit transaction
                        t.Commit();

                        //set to true
                        importSuccesful = true;

                        //set for logging / user feedback
                        GUI.Cmd_ReadTerrain.numPoints = revDTMpts.Count;
                        GUI.Cmd_ReadTerrain.numFacets = terrainFaces.Count;

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
                    TaskDialog.Show("DTM error", ex.Message);

                    //write exception to console
                    Console.WriteLine(ex);

                    //set to false
                    importSuccesful = false;
                    
                    //roll back cause of error
                    t.RollBack();

                    return;
                }
            }


        }

        /// <summary>
        /// function to transform points to revit crs
        /// </summary>
        private List<XYZ> transPts(List<geom.Point> terrainPoints)
        {
            //init new list
            var revDTMpts = new List<XYZ>();

            //loop throgh every point in list
            foreach (var pt in terrainPoints)
            {
                //create point (otherwise clac point won't work)
                C2BPoint c2bPoint = new C2BPoint(pt.X, pt.Y, pt.Z);

                //Transformation for revit
                var unprojectedPt = Calc.GeorefCalc.CalcUnprojectedPoint(c2bPoint, true);

                //add to new list (as projected point)
                revDTMpts.Add(Revit_Build.GetRevPt(unprojectedPt));
            }

            return revDTMpts;
        }

        /// <summary>
        /// function to transform NTS coordinates to revit crs
        /// </summary>
        private List<XYZ> transCoords(List<geom.Coordinate> coordinateList)
        {
            //init new list
            var revDTMpts = new List<XYZ>();

            //loop through
            foreach (var coord in coordinateList)
            {
                C2BPoint c2bpoint = new C2BPoint(coord.X, coord.Y, coord.Z);
                var unprojectedPt = Calc.GeorefCalc.CalcUnprojectedPoint(c2bpoint, true);

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

                //search for exsiting terrainIDs and delete them
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                IList<Element> dataStorageList = collector.OfClass(typeof(DataStorage)).ToElements();

                if (dataStorageList.Count > 0)
                {
                    foreach(var ds in dataStorageList)
                    {
                        var existingEnt = ds.GetEntity(terrainIDSchema);
                        if (existingEnt.IsValid())
                        {
                            ds.DeleteEntity(terrainIDSchema);
                        }
                    }
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