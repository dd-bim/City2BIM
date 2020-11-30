using System.Collections.Generic;
using City2BIM.Geometry;
using City2BIM.Semantic;

namespace City2BIM.CityGML
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
                return bldgId;
            }

            set
            {
                bldgId = value;
            }
        }

        public Dictionary<Xml_AttrRep, string> BldgAttributes
        {
            get
            {
                return bldgAttributes;
            }

            set
            {
                bldgAttributes = value;
            }
        }

        public List<CityGml_BldgPart> Parts
        {
            get
            {
                return parts;
            }

            set
            {
                parts = value;
            }
        }

        public C2BSolid BldgSolid
        {
            get
            {
                return bldgSolid;
            }

            set
            {
                bldgSolid = value;
            }
        }

        public List<CityGml_Surface> BldgSurfaces
        {
            get
            {
                return bldgSurfaces;
            }

            set
            {
                bldgSurfaces = value;
            }
        }

        public string Lod { get => lod; set => lod = value; }
        public List<Logging.LogPair> LogEntries { get => logEntries; set => logEntries = value; }
        public enum LodRep { LOD1, LOD2, LOD1_Fallback, LOD2_Fallback, unknown }
    }



}