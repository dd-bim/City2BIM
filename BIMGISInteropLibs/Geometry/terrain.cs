using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Logging
using BIMGISInteropLibs.Logging;
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages
using NetTopologySuite.Geometries;

namespace BIMGISInteropLibs.Geometry
{
    public class terrain {

        /// <summary>
        /// add point to create a unique coord list
        /// </summary>
        public static int addPoint(HashSet<Point> points, Point p)
        {
            if (!points.Contains(p))
            {
                points.Add(p);
                p.UserData = points.Count-1;
                return (int)p.UserData;
            }

            points.TryGetValue(p, out var pt);
            return (int)pt.UserData;
        }
    }
}

