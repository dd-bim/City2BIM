using System;
using System.Collections.Generic;
using System.Text;
using BimGisCad.Collections;

namespace BimGisCad
{
    /// <summary>
    /// Oberstes Modell Objekt
    /// </summary>
    public abstract class Root:Unique
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="attributes"></param>
        public Root(string id, string name, UniqueCollection<Attribute> attributes = null):base(id)
        {
            this.Name = name;
            this.Attributes = attributes ?? UniqueCollection<Attribute>.Create();
        }

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Attribute (in der Regel alles was nicht Geometrie und Topologie ist)
        /// </summary>
        public UniqueCollection<Attribute> Attributes { get; }
    }
}
