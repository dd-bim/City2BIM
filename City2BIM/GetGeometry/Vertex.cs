using System.Collections.Generic;

namespace City2BIM.GetGeometry
{
    /// <summary>
    ///  Vertex or Node (Topological Point)
    /// </summary>
    public class Vertex
    {
        private XYZ position;
        HashSet<string> planes;

        /// <summary>
        ///  Create Vertex (Topological Point)
        /// </summary>
        /// /// <param name="position"> Coordinate in XYZ </param>
        /// /// <param name="plane"> Plane to add </param>
        public Vertex(XYZ position, string plane)
        {
            this.position = position;
            this.planes = new HashSet<string>();
            this.planes.Add(plane);
        }

        /// <summary>
        ///  Add plane to vertex
        /// </summary>
        /// <param name="plane"> Plane to add </param>
        public void AddPlane(string plane)
        {
            planes.Add(plane);
        }

        public XYZ Position
        {
            get { return position; }
            set { position = value; }
        }

        public HashSet<string> Planes
        {
            get { return planes; }
        }

        public Solid Solid
        {
            get => default(Solid);
            set
            {
            }
        }
    }
}
