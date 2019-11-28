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
        private Guid internalID;        
        private string surfaceId;
        private FaceType facetype;
        private Dictionary<XmlAttribute, string> surfaceAttributes;
        private List<C2BPoint> exteriorPts;
        private List<List<C2BPoint>> interiorPts;

        public Guid InternalID { get => internalID; }

        public GmlSurface()
        {
            this.internalID = Guid.NewGuid();
        }

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

        public Dictionary<XmlAttribute, string> SurfaceAttributes
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

        public List<List<C2BPoint>> InteriorPts { get => interiorPts; set => interiorPts = value; }
        public List<C2BPoint> ExteriorPts { get => exteriorPts; set => exteriorPts = value; }

        public enum FaceType { roof, wall, ground, closure, outerCeiling, outerFloor, unknown }
    }
}
