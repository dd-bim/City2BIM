using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;


namespace BimGisCad.Collections
{
    /// <summary>
    /// Liste für unsortierte Typen welche von IUnique erben. Garantiert wird das jedes Element (Id) nur einmal vertreten ist
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class UniqueCollection<T>:Dictionary<string,T>, ICollection<T> where T : Unique
    {
        private UniqueCollection(IDictionary<string, T> dictionary) : base(dictionary) { }

        private UniqueCollection()
        {
        }

        /// <summary>
        /// Element mittels Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public new T this[string id] => this[id];

        /// <summary>
        /// Prüfen auf Vorhandensein
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(T item) => this.ContainsKey(item.Id);

        /// <summary>
        /// Prüfen auf Vorhandensein
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool Contains(string id) => this.ContainsKey(id);

        /// <summary>
        /// Hinzufügen oder Ändern eines Elements
        /// </summary>
        /// <param name="item"></param>
        public void SetItem(T item) => base[item.Id] = item;

        /// <summary>
        /// Hinzufügen eines neuen Elements, oder false falls vorhanden
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Add(T item)
        {
            if(this.Contains(item.Id))
            { return false; }
            else
            {
                this.Add(item.Id, item);
                return true;
            }
        }

        /// <summary>
        /// Entfernt Element
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(T item) => this.Remove(item.Id);

        bool ICollection<T>.IsReadOnly { get; }

        void ICollection<T>.Add(T item) => this.Add(item);
        void ICollection<T>.CopyTo(T[] array, int arrayIndex) => this.Values.ToList().CopyTo(array, arrayIndex);
        bool ICollection<T>.Remove(T item) => this.Remove(item);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.Values.GetEnumerator();

        /// <summary>
        /// Builder aus vorhandener Collection
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static UniqueCollection<T> Create(ICollection<T> items) => new UniqueCollection<T>(items.ToDictionary(v => v.Id, v => v));

        /// <summary>
        /// Leere Liste
        /// </summary>
        /// <returns></returns>
        public static UniqueCollection<T> Create() => new UniqueCollection<T>();

    }
}
