using System;
using System.Collections;
using System.Collections.Generic;

namespace Somno
{
    /// <summary>
    /// Represents a view over a generic collection.
    /// </summary>
    internal class CollectionAccessor<T> : IEnumerable<T>
    {
        readonly IList<T> collection;
        public readonly int Length;

        /// <summary>
        /// Creates a view over the given <paramref name="collection"/>.
        /// </summary>
        /// <param name="collection">The collection to use.</param>
        /// <param name="length">The amount of elements to expose to consumers of the <see cref="CollectionAccessor{T}"/>.</param>
        public CollectionAccessor(IList<T> collection, int length)
        {
            if (collection.Count < length) {
                throw new InvalidOperationException($"Cannot create a view of {length} elements over a collection that is {collection.Count} elements in size.");
            }

            this.collection = collection;
            Length = length;
        }

        /// <summary>
        /// Creates an empty <see cref="CollectionAccessor{T}"/> instance.
        /// </summary>
        public CollectionAccessor()
        {
            collection = null!;
            Length = 0;
        }

        public T this[int idx] {
            get {
                if (idx >= Length) {
                    throw new IndexOutOfRangeException($"The index {idx} is out of range; length was {Length}.");
                }

                return collection[idx];
            }
            set {
                if (idx >= Length) {
                    throw new IndexOutOfRangeException($"The index {idx} is out of range; length was {Length}.");
                }

                collection[idx] = value;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Length; i++)
                yield return collection[i];
        }

        IEnumerator IEnumerable.GetEnumerator() {
            for (int i = 0; i < Length; i++)
                yield return collection[i];
        }
    }
}
