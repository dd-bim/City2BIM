using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OSGeo.OGR;


namespace BIMGISInteropLibs.OGR
{
    public class OGRALKISReader
    {
        private DataSource ds;

        public OGRALKISReader(string filePath)
        {
            var nasDriver = Ogr.GetDriverByName("NAS");
            ds = nasDriver.Open(filePath, 0);
        }

        private List<string> LayerToProcess = new List<string>
        {
            //parcels
            "AX_Flurstueck",

            //buildings
            "AX_Gebaeude",

            //group "Siedlung"
            "AX_Wohnbauflaeche",
            "AX_IndustrieUndGewerbeflaeche",
            "AX_Halde",
            "AX_Bergbaubetrieb",
            "AX_TagebauGrubeSteinbruch",
            "AX_FlaecheGemischterNutzung",
            "AX_FlaecheBesondererFunktionalerPraegung",
            "AX_SportFreizeitUndErholungsflaeche",
            "AX_Friedhof",

            //group "Verkehr"
            "AX_Strassenverkehr",
            "AX_Weg",
            "AX_Platz",
            "AX_Bahnverkehr",
            "AX_Flugverkehr",
            "AX_Schiffsverkehr",

            //group "Vegetation"
            "AX_Landwirtschaft",
            "AX_Wald",
            "AX_Gehoelz",
            "AX_Heide",
            "AX_Moor",
            "AX_Sumpf",
            "AX_UnlandVegetationsloseFlaeche",

            //group "Gewaesser"
            "AX_Fliessgewaesser",
            "AX_Hafenbecken",
            "AX_StehendesGewaesser",
            "AX_Meer"
        };

        public List<string> getLayerList()
        {
            List<string> layerList = new List<string>();

            for (int i = 0; i < ds.GetLayerCount(); i++)
            {
                layerList.Add(ds.GetLayerByIndex(i).GetName());
            }

            layerList = layerList.Where(item => LayerToProcess.Contains(item)).ToList();

            return layerList;
        }

        public Layer getLayerByName(string layerName)
        {
            return ds.GetLayerByName(layerName);
        }

        public List<string> getFieldNamesForLayer(Layer layer)
        {
            List<string> fieldNames = new List<string>();

            var featureDefn = layer.GetLayerDefn();

            for (int i=0; i<featureDefn.GetFieldCount(); i++)
            {
                fieldNames.Add(featureDefn.GetFieldDefn(i).GetName());
            }

            return fieldNames;
        }

        public List<GeoObject> getGeoObjectsForLayer(Layer layer, OGRSpatialFilter filter=null)
        {
            List<GeoObject> geoObjects = new List<GeoObject>();
            string usageType = layer.GetName();
            layer.ResetReading();

            if (filter != null)
            {
                layer.SetSpatialFilter(filter.Geom);
            }

            Feature currentFeature;

            while ((currentFeature = layer.GetNextFeature()) != null)
            {
                var properties = new Dictionary<string, string>();

                for (int i=0; i<currentFeature.GetFieldCount(); i++)
                {
                    string fieldName = currentFeature.GetFieldDefnRef(i).GetName();
                    string fieldValue = currentFeature.GetFieldAsString(i);
                    properties.Add(fieldName, fieldValue);
                }

                var geom = currentFeature.GetGeometryRef();

                //var segments = OGRUtils.getSegmentsFromGeometry(geom);

                //var geoObject = new GeoObject(usageType, currentFeature.GetFieldAsString("gml_id"), geom.GetGeometryType(), segments, properties);
                var geoObject = new GeoObject(usageType, currentFeature.GetFieldAsString("gml_id"), geom.GetGeometryType(), geom, properties);
                geoObjects.Add(geoObject);
            }

            return geoObjects;
        }

        public void destroy()
        {
            ds.Dispose();
        }
    }
}
