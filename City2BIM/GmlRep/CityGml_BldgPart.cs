using System.Collections.Generic;
using City2BIM.Geometry;
using City2BIM.Semantic;

namespace City2BIM.GmlRep
{
    public class CityGml_BldgPart
    {
        private string bldgPartId;
        private Dictionary<Xml_AttrRep, string> bldgPartAttributes;
        private List<CityGml_Surface> partSurfaces;
        private C2BSolid partSolid;
        private string lod;

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

        public Dictionary<Xml_AttrRep, string> BldgPartAttributes
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

        public string Lod { get => lod; set => lod = value; }

        public List<CityGml_Surface> PartSurfaces
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