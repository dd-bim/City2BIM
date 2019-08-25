using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using City2BIM.GetGeometry;
using City2BIM.GetSemantics;

namespace City2BIM.GmlRep
{
    public class GmlSurface
    {
        private string surfaceId;
        private FaceType facetype;
        private List<C2BPoint> exterior;
        private List<C2BPoint> interior;
        private Dictionary<GmlAttribute, string> surfaceAttributes;
        private C2BPlane plane;

        public string SurfaceId
        {
            get
            {
                return this.surfaceId;
            }

            set
            {
                this.surfaceId = value;
            }
        }

        public Dictionary<GmlAttribute, string> SurfaceAttributes
        {
            get
            {
                return this.surfaceAttributes;
            }

            set
            {
                this.surfaceAttributes = value;
            }
        }

        public C2BPlane Plane
        {
            get
            {
                return this.plane;
            }

            set
            {
                this.plane = value;
            }
        }

        public FaceType Facetype
        {
            get
            {
                return this.facetype;
            }

            set
            {
                this.facetype = value;
            }
        }

        public List<C2BPoint> Exterior
        {
            get
            {
                return this.exterior;
            }

            set
            {
                this.exterior = value;
            }
        }

        public List<C2BPoint> Interior
        {
            get
            {
                return this.interior;
            }

            set
            {
                this.interior = value;
            }
        }

        public enum FaceType { roof, wall, ground, closure, unknown }
    }
}
