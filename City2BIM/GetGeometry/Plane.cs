using System.Collections.Generic;

namespace City2BIM.GetGeometry
    
{

    public class Plane
    {
        private string id;
        private int[] vertices;
        private XYZ normal;
        private XYZ centroid;
        private FaceType facetype;

        public Plane(string id, List<int> vertices, XYZ normal, XYZ centroid)
        {
            this.id = id;
            this.vertices = vertices.ToArray();
            this.normal = normal;
            this.centroid = centroid;
        }

        public int[] Vertices
        {
            get { return vertices; }
        }


        public string ID
        {
            get { return id; }
        }

        public XYZ Normal
        {
            get { return normal; }
        }

        public XYZ Centroid
        {
            get { return centroid; }
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

        public enum FaceType { roof, wall, ground, closure }
    }
}

