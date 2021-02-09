using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BIMGISInteropLibs.Geometry;
using BIMGISInteropLibs.Semantic;

namespace BIMGISInteropLibs.CityGML
{
    public class CityGml_Surface
    {
        private Guid internalID;
        private string surfaceId;
        private FaceType facetype;
        private Dictionary<Xml_AttrRep, string> surfaceAttributes;
        private List<C2BPoint> exteriorPts;
        private List<List<C2BPoint>> interiorPts;

        public Guid InternalID { get => internalID; }

        public CityGml_Surface()
        {
            internalID = Guid.NewGuid();
        }

        public string SurfaceId
        {
            get
            {
                return surfaceId;
            }

            set
            {
                surfaceId = value;
            }
        }

        public Dictionary<Xml_AttrRep, string> SurfaceAttributes
        {
            get
            {
                return surfaceAttributes;
            }

            set
            {
                surfaceAttributes = value;
            }
        }

        public FaceType Facetype
        {
            get
            {
                return facetype;
            }

            set
            {
                facetype = value;
            }
        }

        public List<List<C2BPoint>> InteriorPts { get => interiorPts; set => interiorPts = value; }
        //public List<List<C2BPoint>> InteriorPts { get => interiorPts; set => interiorPts = null; }

        public List<C2BPoint> ExteriorPts { get => exteriorPts; set => exteriorPts = value; }

        public enum FaceType { roof, wall, ground, closure, outerCeiling, outerFloor, unknown }
    }
}
