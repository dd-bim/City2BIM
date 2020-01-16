﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using City2BIM.Geometry;
using City2BIM.Semantic;

namespace City2BIM.GmlRep
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

        public Dictionary<Xml_AttrRep, string> SurfaceAttributes
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