using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMGISInteropLibs.Geometry
{
    class C2BSegment
    {
        public bool isCurve;
        public C2BPoint startPoint;
        public C2BPoint endPoint;
        public C2BPoint midPoint;

        public C2BSegment(C2BPoint start, C2BPoint end, C2BPoint mid = null)
        {
            startPoint = start;
            endPoint = end;
            midPoint = mid;
        }
    }
}
