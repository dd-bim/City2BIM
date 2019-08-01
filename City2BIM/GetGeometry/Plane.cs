using System.Collections.Generic;
using System.Linq;

namespace City2BIM.GetGeometry
    
{

    public class Plane
    {
        private string id;
        private int[] vertices;
        private XYZ normal;
        private XYZ centroid;
        private FaceType facetype;
        private RingType ringtype;

        public Plane(string id, List<int> vertices, XYZ normal, XYZ centroid, FaceType fType, RingType rType)
        {
            this.id = id;
            this.vertices = vertices.ToArray();
            this.normal = normal;
            this.centroid = centroid;
            this.Facetype = fType;
            this.Ringtype = rType;
        }

        public int[] Vertices
        {
            get { return vertices; }
            set
            {
                this.vertices = value;
            }
        }


        public string ID
        {
            get { return id; }

        }

        public XYZ Normal
        {
            get { return normal; }
            set
            {
                this.normal = value;
            }
        }

        public XYZ Centroid
        {
            get { return centroid; }
            set
            {
                this.centroid = value;
            }
        }

        public Solid Solid
        {
            get => default(Solid);
            set
            {
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

        public RingType Ringtype
        {
            get
            {
                return this.ringtype;
            }

            set
            {
                this.ringtype = value;
            }
        }

        public enum FaceType { roof, wall, ground, closure, unknown }

        public enum RingType { holeless, exterior, interior, unknown }
    }
}

