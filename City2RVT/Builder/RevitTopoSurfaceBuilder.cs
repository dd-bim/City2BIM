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
        public void createDTMviaPoints(List<C2BPoint> terrainPoints)
        {
            //init new list
            var revDTMpts = new List<XYZ>();

            //loop throgh every point in list
            foreach(var pt in terrainPoints)
            {
                //Transformation for revit
                var unprojectedPt = Calc.GeorefCalc.CalcUnprojectedPoint(pt, true);

                //add to new list (as projected point)
                revDTMpts.Add(Revit_Build.GetRevPt(unprojectedPt));
            }

            //get filename
            var dtmFile = GUI.Prop_NAS_settings.DtmFile;

            //transaction for surface / dtm creation
            using (Transaction t = new Transaction(doc, "Create TopoSurface"))
            {
                //start transaction
                t.Start();
                try
                {
                    var surface = TopographySurface.Create(doc, revDTMpts);
                    storeTerrainIDInExtensibleStorage(doc, surface.Id);
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