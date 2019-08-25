using System.Collections.Generic;
using City2BIM.GetGeometry;
using City2BIM.GetSemantics;

namespace City2BIM.GmlRep
{
    public class GmlBldgPart
    {
        private string bldgPartId;
        private Dictionary<GmlAttribute, string> bldgPartAttributes;
        private List<GmlSurface> partSurfaces;
        private C2BSolid partSolid;

        public string BldgPartId
        {
            get
            {
                return this.bldgPartId;
            }

            set
            {
                this.bldgPartId = value;
            }
        }

        public C2BSolid PartSolid
        {
            get
            {
                return this.partSolid;
            }

            set
            {
                this.partSolid = value;
            }
        }

        public Dictionary<GmlAttribute, string> BldgPartAttributes
        {
            get
            {
                return this.bldgPartAttributes;
            }

            set
            {
                this.bldgPartAttributes = value;
            }
        }

        internal List<GmlSurface> PartSurfaces
        {
            get
            {
                return this.partSurfaces;
            }

            set
            {
                this.partSurfaces = value;
            }
        }

        //public GmlBldgPart(XElement gmlBldgPart) { }

        //public void CreateSolid()
    }
}