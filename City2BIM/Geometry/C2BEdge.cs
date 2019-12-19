using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace City2BIM.Geometry
{
    public class C2BEdge
    {
        private int start;
        private int end;
        private C2BPoint planeNormal;
        private string planeId;

        public int Start { get => start; set => start = value; }
        public int End { get => end; set => end = value; }
        public C2BPoint PlaneNormal { get => planeNormal; set => planeNormal = value; }
        public string PlaneId { get => planeId; set => planeId = value; }

        public C2BEdge(int start, int end, string id, C2BPoint normal)
        {
            this.End = end;
            this.Start = start;
            this.PlaneId = id;
            this.PlaneNormal = normal;
        }
    }
}
