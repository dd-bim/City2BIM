using System.Collections.Generic;

namespace City2BIM.GetGeometry

{
    public class C2BPlane
    {
        private string id;
        private int[] vertices;
        private List<int[]> innerVertices;
        private C2BPoint normal;
        private C2BPoint centroid;
        private List<C2BEdge> edges;

        public C2BPlane(string id, List<int> vertices, List<List<int>> innerVertices, C2BPoint normal, C2BPoint centroid, List<C2BEdge> edges)
        {
            this.id = id;
            this.vertices = vertices.ToArray();
            
            List<int[]> innerV = new List<int[]>();

            foreach (var innerPoly in innerVertices)
            {
                int[] innerPolyA = innerPoly.ToArray();
                innerV.Add(innerPolyA);
            }
            this.innerVertices = innerV;

            this.normal = normal;
            this.centroid = centroid;
            this.edges = edges;
        }

        public C2BPlane(string _id, List<int> _vertices, List<int[]> _innerVertices, C2BPoint _normal, C2BPoint _centroid, List<C2BEdge> _edges)
        {
            this.id = _id;
            this.vertices = _vertices.ToArray();
            this.innerVertices = _innerVertices;
            this.normal = _normal;
            this.centroid = _centroid;
            this.edges = _edges;
        }

        public int[] Vertices
        {
            get { return vertices; }
            set
            {
                this.vertices = value;
            }
        }

        public List<int[]> InnerVertices
        {
            get { return innerVertices; }
            set
            {
                this.innerVertices = value;
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

        public List<C2BEdge> Edges { get => edges; set => edges = value; }
    }
}