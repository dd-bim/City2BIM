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

        //public GmlBldg(XElement gmlBldg, HashSet<GmlAttribute> attributes, Dictionary<string, XNamespace> nsp)
        //{
        //    this.BldgId = gmlBldg.Attribute(nsp["gml"] + "id").Value;

        //    this.BldgAttributes = new ReadSemValues().ReadAttributeValuesBldg(gmlBldg, attributes, nsp);

        //}

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
    }
}