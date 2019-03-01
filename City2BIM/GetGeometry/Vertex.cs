using System.Collections.Generic;

namespace City2BIM.GetGeometry
{

    public class Vertex
    {
        private XYZ position;
        HashSet<string> planes;


        public Vertex(XYZ position, string plane)
        {
            this.position = position;
            this.planes = new HashSet<string>();
            this.planes.Add(plane);
        }

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
