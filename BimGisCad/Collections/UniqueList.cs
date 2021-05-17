using System.Collections;
using System.Collections.Generic;

namespace BimGisCad.Collections
{
    /// <summary>
    ///  Geordnete (indizierte) Liste für Typen welche von IUnique erben. Garantiert wird das jedes
    ///  Element (Id) nur einmal vertreten ist und Reihenfolge bleibt
    /// </summary>
    /// <typeparam name="T">  </typeparam>
    public sealed class UniqueList<T> : IList<T> where T : Unique
    {
        private readonly Dictionary<string, int> ids;
        private readonly List<T> items;

        private UniqueList(Dictionary<string, int> ids, List<T> items)
        {
            this.ids = ids;
            this.items = items;
        }

        /// <summary>
        /// Element mit Identifier
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public T this[string id] => this.items[this.ids[id]];

        /// <summary>
        /// Element mit Index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index]
        {
            get
            {
                return this.items[index];
            }
            set
            {
                int oldIndex;
                if(this.ids.TryGetValue(value.Id, out oldIndex))
                {
                    if(oldIndex == index)
                    {
                        this.items[index] = value;
                        return;
                    }
                    else
                    {
                        this.RemoveAt(oldIndex);
                    }
                }
                this.ids.Remove(this.items[index].Id);
                this.ids.Add(value.Id, index);
                this.items[index] = value;
            }
        }

        /// <summary>
        /// Element, wenn vorhanden
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(string id, out T value)
        {
            int index;
            if(this.ids.TryGetValue(id, out index))
            {
                value = this.items[index];
                return true;
            }
            else
            {
                value = default(T);
                return false;
            }
        }

        /// <summary>
        /// Index eines Elements, oder -1
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(T item)
        {
            int index;
            return this.ids.TryGetValue(item.Id, out index) ? index : -1;
        }

        /// <summary>
        /// Vorhanden?
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(T item) => this.ids.ContainsKey(item.Id);

        /// <summary>
        /// Vorhanden?
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool Contains(string id) => this.ids.ContainsKey(id);

        /// <summary>
        /// Fügt neues Element hinzu oder ändert Element mit selber Id
        /// </summary>
        /// <param name="item"></param>
        public void SetItem(T item)
        {
            int index;
            if(this.ids.TryGetValue(item.Id, out index))
            {
                this.items[index] = item;
            }
            else
            {
                this.ids.Add(item.Id, this.items.Count);
                this.items.Add(item);
            }
        }

        /// <summary>
        /// Fügt neues Element hinzu, falls nicht vorhanden
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Add(T item)
        {
            if(this.Contains(item.Id))
            { return false; }
            else
            {
                this.ids.Add(item.Id, this.items.Count);
                this.items.Add(item);
                return true;
            }
        }

        /// <summary>
        /// Ändert Element an Position, falls nicht vorhanden
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Insert(int index, T item)
        {
            if(index < 0 || this.Contains(item))
            { return false; }
            if(index >= this.items.Count)
            {
                this.ids.Add(item.Id, this.items.Count);
                this.items.Add(item);
                return true;
            }

            this.ids.Add(item.Id, index);
            this.items.Insert(index, item);
            for(int i = index + 1; i < this.items.Count; i++)
            {
                this.ids[this.items[i].Id] = i;
            }
            return true;
        }

        /// <summary>
        /// Leert Liste
        /// </summary>
        public void Clear()
        {
            this.items.Clear();
            this.ids.Clear();
        }

        /// <summary>
        /// Entfernt Element
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(T item)
        {
            int index;
            if(this.ids.TryGetValue(item.Id, out index))
            {
                this.ids.Remove(item.Id);
                this.items.RemoveAt(index);
                for(int i = index; i < this.items.Count; i++)
                {
                    this.ids[this.items[i].Id] = i;
                }
                return true;
            }
            else
            { return false; }
        }

        /// <summary>
        /// Entfernt Element an Position
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool RemoveAt(int index)
        {
            if(index < this.items.Count && index > -1)
            {
                this.ids.Remove(this.items[index].Id);
                this.items.RemoveAt(index);
                for(int i = index; i < this.items.Count; i++)
                {
                    this.ids[this.items[i].Id] = i;
                }
                return true;
            }
            else
            { return false; }
        }

        /// <summary>
        /// Anzahl der Elemente
        /// </summary>
        public int Count => this.items.Count;

        /// <summary>
        /// Builder leer
        /// </summary>
        /// <returns></returns>
        public static UniqueList<T> Create() => new UniqueList<T>(new Dictionary<string, int>(), new List<T>());

        /// <summary>
        /// Builder aus vorhandener Liste
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static UniqueList<T> Create(IList<T> items) => new UniqueList<T>(new Dictionary<string, int>(), new List<T>());

        /// <summary>
        /// Builder aus vorhandener Aufzählung
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static UniqueList<T> Create(IEnumerable<T> items)
        {
            var ids = new Dictionary<string, int>();
            var _items = new List<T>();
            int i = 0;
            foreach(var item in items)
            {
                if(!ids.ContainsKey(item.Id))
                {
                    ids.Add(item.Id, i++);
                    _items.Add(item);
                }
            }
            return new UniqueList<T>(ids, _items);
        }

        bool ICollection<T>.IsReadOnly { get; }

        /// <summary>
        /// Enumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator() => this.items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        void ICollection<T>.Add(T item) => this.Add(item);

        void ICollection<T>.CopyTo(T[] array, int arrayIndex) => this.items.CopyTo(array, arrayIndex);

        bool ICollection<T>.Remove(T item) => this.Remove(item);

        void IList<T>.Insert(int index, T item) => this.Insert(index, item);

        void IList<T>.RemoveAt(int index) => this.RemoveAt(index);
    }
}