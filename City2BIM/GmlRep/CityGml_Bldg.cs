using System.Collections.Generic;
using City2BIM.GetGeometry;
using City2BIM.GetSemantics;

namespace City2BIM.GmlRep
{
    public class CityGml_Bldg
    {
        private string bldgId;
        private Dictionary<Xml_AttrRep, string> bldgAttributes;
        private List<CityGml_BldgPart> parts;
        private List<CityGml_Surface> bldgSurfaces;
        private C2BSolid bldgSolid;
        private string lod;
        private List<Logging.LogPair> logEntries;

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

        public Dictionary<Xml_AttrRep, string> BldgAttributes
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

        public List<CityGml_BldgPart> Parts
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

        public List<CityGml_Surface> BldgSurfaces
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
        public List<Logging.LogPair> LogEntries { get => logEntries; set => logEntries = value; }
        public enum LodRep { LOD1, LOD2, LOD1_Fallback, LOD2_Fallback, unknown }
    }


    
}