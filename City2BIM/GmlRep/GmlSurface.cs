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
        private Dictionary<GmlAttribute, string> surfaceAttributes;
        private C2BPlane planeExt;
        private List<C2BPlane> planeInt;

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

        public C2BPlane PlaneExt
        {
            get
            {
                return this.planeExt;
            }

            set
            {
                this.planeExt = value;
            }
        }

        public List<C2BPlane> PlaneInt
        {
            get
            {
                return this.planeInt;
            }

            set
            {
                this.planeInt = value;
            }
        }

        public enum FaceType { roof, wall, ground, closure, unknown }
    }
}
