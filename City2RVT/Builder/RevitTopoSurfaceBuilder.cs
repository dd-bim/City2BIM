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

        public RevitTopoSurfaceBuilder(Document doc)
        {
            this.doc = doc;
        }

        public void CreateDTM(List<C2BPoint> terrainPoints)
        {
            var revDTMpts = new List<XYZ>();

            foreach(var pt in terrainPoints)
            {
                //Transformation for revit
                var unprojectedPt = Calc.GeorefCalc.CalcUnprojectedPoint(pt, true);

                revDTMpts.Add(Revit_Build.GetRevPt(unprojectedPt));
            }

            var dtmFile = GUI.Prop_NAS_settings.DtmFile;

            using (Transaction t = new Transaction(doc, "Create TopoSurface"))
            {
                t.Start();

                var surface = TopographySurface.Create(doc, revDTMpts);
                storeTerrainIDInExtensibleStorage(doc, surface.Id);
                surface.Pinned = true;
                
                t.Commit();
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