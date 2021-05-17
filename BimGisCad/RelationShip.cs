using System;
using System.Collections.Generic;
using System.Text;
using BimGisCad.Collections;

namespace BimGisCad
{
    /// <summary>
    /// Beziehung zwischen Objekten
    /// </summary>
    public class RelationShip:Root
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="attributes"></param>
        /// <param name="relating"></param>
        /// <param name="related"></param>
        public RelationShip(string id, string name, UniqueCollection<Attribute> attributes, Object relating, UniqueCollection<Object> related = null) :base(id, name, attributes)
        {
            this.Relating = relating;
            this.Related = related ?? UniqueCollection<Object>.Create();
        }

        /// <summary>
        /// 
        /// </summary>
        public Object Relating { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public UniqueCollection<Object> Related { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="attributes"></param>
        /// <param name="relating"></param>
        /// <param name="related"></param>
        /// <returns></returns>
        public static RelationShip Create(string id, string name, UniqueCollection<Attribute> attributes, Object relating, UniqueCollection<Object> related = null) => new RelationShip(id, name, attributes, relating, related);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="relating"></param>
        /// <param name="related"></param>
        /// <returns></returns>
        public static RelationShip Create(string name, Object relating, UniqueCollection<Object> related = null) => new RelationShip(null, name, null, relating, related);

    }
}
