using System.Collections.Generic;
using System.Linq;

namespace City2BIM.GetGeometry
    
{

    public class C2BPlane
    {
        private string id;
        private int[] vertices;
        private C2BPoint normal;
        private C2BPoint centroid;
        //private FaceType facetype;
        //private RingType ringtype;

        public C2BPlane(string id, List<int> vertices, C2BPoint normal, C2BPoint centroid/*, FaceType fType, RingType rType*/)
        {
            this.id = id;
            this.vertices = vertices.ToArray();
            this.normal = normal;
            this.centroid = centroid;
            //this.Facetype = fType;
            //this.Ringtype = rType;
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

        //public FaceType Facetype
        //{
        //    get
        //    {
        //        return this.facetype;
        //    }

        //    set
        //    {
        //        this.facetype = value;
        //    }
        //}

        //public RingType Ringtype
        //{
        //    get
        //    {
        //        return this.ringtype;
        //    }

        //    set
        //    {
        //        this.ringtype = value;
        //    }
        //}

        //public enum FaceType { roof, wall, ground, closure, unknown }

        //public enum RingType { holeless, exterior, interior, unknown }
    }
}

