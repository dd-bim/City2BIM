using System;
using System.Collections.Generic;
using System.Text;
using BimGisCad.Collections;
using BimGisCad.Representation;

namespace BimGisCad
{
    /// <summary>
    /// Eigenständiges Objekt
    /// </summary>
    public class Object : Root
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="attributes"></param>
        public Object(string id, string name, UniqueCollection<Attribute> attributes = null) : base(id, name, attributes)
        {
            this.Representations = new Dictionary<Context, IRepresentation>();
        }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<Context,IRepresentation> Representations { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="attributes"></param>
        /// <returns></returns>
        public static Object Create(string id, string name, UniqueCollection<Attribute> attributes = null) => new Object(id, name, attributes);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="attributes"></param>
        /// <returns></returns>
        public static Object Create(string name, UniqueCollection<Attribute> attributes = null) => new Object(null, name, attributes);
    }
}
