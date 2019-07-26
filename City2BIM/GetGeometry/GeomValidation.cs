using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace City2BIM.GetGeometry
{
    class GeomValidation
    {
        /// <summary>
        /// Checks polygon conditions (Start = End)
        /// </summary>
        /// <param name="points">PointList Polygon</param>
        /// <param name="lowerCorner">Coordinate XYZ lower Corner</param>
        /// <returns>List of Polygon Points</returns>
        private bool SameStartAndEndPt(List<XYZ> polygon)
        {
            var start = polygon.First();
            var end = polygon.Last();

            if(start.X != end.X || start.Y != end.Y || start.Z != end.Z)
                return false;

            return true;
        }

        /// <summary>
        /// Checks polygon conditions (Redundant Points?)
        /// </summary>
        /// <param name="points">PointList Polygon</param>
        /// <returns>List of Polygon Points</returns>
        private bool NoRedundantPts(List<XYZ> polygon)
        {
            foreach(var pt in polygon)
            {
                var samePts = from p in polygon
                              where (pt != p && pt.X == p.X && pt.Y == p.Y && pt.Z == p.Z)
                              select p;

                if(pt == polygon.First() && samePts.Count() > 1)
                    return false;

                if(pt == polygon.Last() && samePts.Count() > 1)
                    return false;

                if(pt != polygon.First() && pt != polygon.Last() && samePts.Count() > 0)
                    return false;
            }

            return true;
        }

    }
}
