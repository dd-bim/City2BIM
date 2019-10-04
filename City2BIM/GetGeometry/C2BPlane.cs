using System.Collections.Generic;

namespace City2BIM.GetGeometry

{
    public class C2BPlane
    {
        private string id;
        private int[] vertices;
        private C2BPoint normal;
        private C2BPoint centroid;
        private List<C2BPoint> polygonPts;
        private List<C2BEdge> edges;

        public C2BPlane(string id)
        {
            this.id = id;
        }

        public C2BPlane(string id, List<int> vertices, C2BPoint normal, C2BPoint centroid, List<C2BEdge> edges)
        {
            this.id = id;
            this.vertices = vertices.ToArray();
            this.normal = normal;
            this.centroid = centroid;
            this.edges = edges;
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
            set
            {
                this.id = value;
            }
        }

        public C2BPoint Normal
        {
            get { return normal; }
            set
            {
                this.normal = value;
            }
        }

        public C2BPoint Centroid
        {
            get { return centroid; }
            set
            {
                this.centroid = value;
            }
        }

        public C2BSolid Solid
        {
            get => default(C2BSolid);
            set
            {
            }
        }

        public List<C2BPoint> PolygonPts
        {
            get
            {
                return this.polygonPts;
            }

            set
            {
                this.polygonPts = value;
            }
        }

        internal List<C2BEdge> Edges { get => edges; set => edges = value; }
    }
}