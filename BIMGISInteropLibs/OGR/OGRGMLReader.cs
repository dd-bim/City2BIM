using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OSGeo.OGR;

namespace BIMGISInteropLibs.OGR
{
    public class OGRGMLReader : OGRReader
    {
        //private DataSource ds;

        public OGRGMLReader(string filePath)
        {
            var nasDriver = Ogr.GetDriverByName("GML");
            ds = nasDriver.Open(filePath, 0);
        }

        public override List<string> getLayerList()
        {
            List<string> layerList = new List<string>();

            for (int i = 0; i < ds.GetLayerCount(); i++)
            {
                if (ds.GetLayerByIndex(i).GetGeomType() != wkbGeometryType.wkbUnknown || ds.GetLayerByIndex(i).GetGeomType() != wkbGeometryType.wkbNone)
                {
                    layerList.Add(ds.GetLayerByIndex(i).GetName());
                }

            }

            //layerList = layerList.Where(item => LayerToProcess.Contains(item)).ToList();

            return layerList;
        }
    }
}
