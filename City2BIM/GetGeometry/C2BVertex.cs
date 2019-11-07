using System.Collections.Generic;

namespace City2BIM.GetGeometry
{
    /// <summary>
    ///  Vertex or Node (Topological Point)
    /// </summary>
    public class C2BVertex
    {
        private C2BPoint position;
        HashSet<string> planes;

        /// <summary>
        ///  Create Vertex (Topological Point)
        /// </summary>
        /// /// <param name="position"> Coordinate in XYZ </param>
        /// /// <param name="plane"> Plane to add </param>
        public C2BVertex(C2BPoint position)
        {
            this.position = position;
            this.planes = new HashSet<string>();
        }

        /// <summary>
        ///  Add plane to vertex
        /// </summary>
        /// <param name="plane"> Plane to add </param>
        public void AddPlane(string plane)
        {
            planes.Add(plane);
        }

        public C2BPoint Position
        {
            get { return position; }
            set { position = value; }
        }

        public HashSet<string> Planes
        {
            get { return planes; }
        }

        public C2BSolid Solid
        {
            get => default(C2BSolid);
            set
            {
            }
        }
    }
}
