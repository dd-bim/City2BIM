using System.Collections.Generic;
using City2BIM.GetGeometry;
using City2BIM.GetSemantics;

namespace City2BIM.GmlRep
{
    public class GmlBldg
    {
        private string bldgId;
        private Dictionary<XmlAttribute, string> bldgAttributes;
        private List<GmlBldgPart> parts;
        private List<GmlSurface> bldgSurfaces;
        private C2BSolid bldgSolid;
        private string lod;
        private List<BldgLog> logEntries;

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

        public Dictionary<XmlAttribute, string> BldgAttributes
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
        public List<BldgLog> LogEntries { get => logEntries; set => logEntries = value; }
        public enum LodRep { LOD1, LOD2, LOD1_Fallback, LOD2_Fallback, unknown }
    }

    public class BldgLog
    {
        private Logging.LogType type;
        private string message;

        public Logging.LogType Type { get => type; set => type = value; }
        public string Message { get => message; set => message = value; }

        public BldgLog(Logging.LogType type, string message)
        {
            this.type = type;
            this.message = message;
        }
    }
    
}