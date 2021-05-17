using System;
using System.Collections.Generic;
using System.Text;

namespace BimGisCad
{
    /// <summary>
    /// Klasse für Attribute
    /// </summary>
    public class Attribute : Unique
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public Attribute(string key, object value):base(key)
        {
            this.Value = value;
        }

        /// <summary>
        /// Schlüssel
        /// </summary>
        public string Key => this.Id;

        /// <summary>
        /// Wert
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Builder
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Attribute Create(string key, object value) => new Attribute(key, value);

        /// <summary>
        /// Builder
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static Attribute Create(Attribute attribute) => new Attribute(attribute.Key, attribute.Value);

        /// <summary>
        /// Builder
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Attribute CreateWithValue(Attribute attribute, object value) => new Attribute(attribute.Key, value);
    }

}
