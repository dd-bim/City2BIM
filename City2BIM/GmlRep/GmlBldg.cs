using System.Collections.Generic;
using City2BIM.GetGeometry;
using City2BIM.GetSemantics;

namespace City2BIM.GmlRep
{
    public class GmlBldg
    {
        private string bldgId;
        private Dictionary<GmlAttribute, string> bldgAttributes;
        private List<GmlBldgPart> parts;
        private List<GmlSurface> bldgSurfaces;
        private C2BSolid bldgSolid;
        private string lod;

        public string BldgId
        {
            get
            {
                return this.bldgId;
            }

            set
            {
                this.bldgId = value;
            }
        }

        public Dictionary<GmlAttribute, string> BldgAttributes
        {
            get
            {
                return this.bldgAttributes;
            }

            set
            {
                this.bldgAttributes = value;
            }
        }

        public List<GmlBldgPart> Parts
        {
            get
            {
                return this.parts;
            }

            set
            {
                this.parts = value;
            }
        }

        public C2BSolid BldgSolid
        {
            get
            {
                return this.bldgSolid;
            }

            set
            {
                this.bldgSolid = value;
            }
        }

        public List<GmlSurface> BldgSurfaces
        {
            get
            {
                return this.bldgSurfaces;
            }

            set
            {
                this.bldgSurfaces = value;
            }
        }

        public string Lod { get => lod; set => lod = value; }

        public enum LodRep { LOD1, LOD2, LOD1_Fallback, LOD2_Fallback, unknown }

    }
}