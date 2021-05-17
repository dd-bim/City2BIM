using System;
using System.Collections.Generic;
using System.Text;

namespace BimGisCad
{
    /// <summary>
    /// Interface für Objekte mit Identifier
    /// </summary>
    public abstract class Unique: IEquatable<Unique>
    {
        /// <summary>
        /// Erzeugt Id basierend auf Microsoft GUID
        /// </summary>
        /// <returns></returns>
        public static string GetId() => Guid.NewGuid().ToString();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        protected Unique(string id)
        {
            this.Id = id ?? GetId();
        }


        /// <summary>
        /// Identifier
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Vergleich anhand der Id
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Unique other) => this.Id == other.Id;
    }
}
