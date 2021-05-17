using System;
using System.Collections.Generic;
using System.Text;

namespace BimGisCad.Collections
{
    struct Tuple<T, U> where U : IComparable<U>
    {
        public readonly T Item;
        public readonly U Priority;

        public Tuple(T item, U priority)
        {
            this.Item = item;
            this.Priority = priority;
        }
    }

    /// <summary>
    /// Queue um Werte nach zugehörigen Prioritätswerten zu sortieren
    /// </summary>
    /// <typeparam name="T">Datentyp</typeparam>
    /// <typeparam name="U">Prioritätstyp (muss sortierbar sein, z.B. int oder double)</typeparam>
    public class PriorityQueue<T, U> where U : IComparable<U>
    {
        private readonly List<Tuple<T, U>> data;

        /// <summary>
        /// Konstruktur
        /// </summary>
        public PriorityQueue()
        {
            this.data = new List<Tuple<T, U>>();
        }

        /// <summary>
        /// Fügt einen Wert hinzu
        /// </summary>
        /// <param name="item">Datenwert</param>
        /// <param name="priority">Prioritätswert</param>
        public void Enqueue(T item, U priority)
        {
            this.data.Add(new Tuple<T, U>(item, priority));
            int ci = this.data.Count - 1; // child index; start at end
            while(ci > 0)
            {
                int pi = (ci - 1) / 2; // parent index
                if(this.data[ci].Priority.CompareTo(this.data[pi].Priority) >= 0)
                {
                    break; // child item is larger than (or equal) parent so we're done
                }

                var temp = this.data[ci];
                this.data[ci] = this.data[pi];
                this.data[pi] = temp;
                ci = pi;
            }
        }

        /// <summary>
        /// Gibt Datenwert mit niedrigster Priorität zurück, und entfernt ihn aus der Liste
        /// </summary>
        /// <param name="priority">Prioritätswert</param>
        /// <returns>Datenwert</returns>
        public T Dequeue(out U priority)
        {
            // assumes pq is not empty; up to calling code
            int li = this.data.Count - 1; // last index (before removal)
            var frontItem = this.data[0];   // fetch the front
            this.data[0] = this.data[li];
            this.data.RemoveAt(li);

            --li; // last index (after removal)
            int pi = 0; // parent index. start at front of pq
            while(true)
            {
                int ci = (pi * 2) + 1; // left child index of parent
                if(ci > li)
                {
                    break;  // no children so done
                }

                int rc = ci + 1;     // right child
                if(rc <= li && this.data[rc].Priority.CompareTo(this.data[ci].Priority) < 0) // if there is a rc (ci + 1), and it is smaller than left child, use the rc instead
                {
                    ci = rc;
                }

                if(this.data[pi].Priority.CompareTo(this.data[ci].Priority) <= 0)
                {
                    break; // parent is smaller than (or equal to) smallest child so done
                }

                // swap parent and child
                var temp = this.data[ci];
                this.data[ci] = this.data[pi];
                this.data[pi] = temp;

                pi = ci;
            }
            priority = frontItem.Priority;
            return frontItem.Item;
        }

        /// <summary>
        /// Datenwert mit niedrigster Priorität
        /// </summary>
        public T PeekItem => this.data[0].Item;

        /// <summary>
        /// Niedrigste Priorität
        /// </summary>
        public U PeekPriority => this.data[0].Priority;

        /// <summary>
        /// Anzahl der Elemente
        /// </summary>
        public int Count => this.data.Count;

    }
}
