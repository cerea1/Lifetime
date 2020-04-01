using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CerealDevelopment.LifetimeManagement
{
    public abstract class LifetimeListBase
    {
        internal abstract event Action<LifetimeListBase, ILifetime, int> ItemAdded;
        internal abstract event Action<LifetimeListBase, ILifetime, int> ItemRemoved;
        internal List<ILifetime> cache = new List<ILifetime>();
        internal List<LifetimeListBase> sublists = new List<LifetimeListBase>();

        internal abstract bool TryGetAtIndex(int index, out ILifetime cached);
        internal abstract void Initialize();
    }

    public sealed class LifetimeList<T> : LifetimeListBase, IEnumerable<T> where T : ILifetime
    {
        internal override event Action<LifetimeListBase, ILifetime, int> ItemAdded;
        internal override event Action<LifetimeListBase, ILifetime, int> ItemRemoved;
        public sealed class LifetimeListEnumerator : IEnumerator<T>
        {
            private LifetimeList<T> list;

            private int currentIndex = -1;

            private T current;

            private List<T> additiveItems = new List<T>();

            internal LifetimeListEnumerator(LifetimeList<T> list)
            {
                this.list = list;
            }

            internal void ItemRemoved(T item, int index)
            {
                if (!additiveItems.RemoveSwapBack(item))
                {
                    if (index <= currentIndex)
                    {
                        currentIndex--;
                    }
                }
            }

            internal void ItemAdded(T item, int index)
            {
                if (index <= currentIndex)
                {
                    additiveItems.Add(item);
                    currentIndex++;
                }
            }

            public T Current => current;

            public bool MoveNext()
            {
                if (additiveItems.Count > 0)
                {

                    current = additiveItems[0];
                    additiveItems.RemoveAtSwapBack(0);
                    return true;
                }
                else
                {
                    currentIndex++;
                    if (currentIndex < list.Count)
                    {
                        current = list[currentIndex];
                        return true;
                    }
                    else
                    {
                        current = default;
                        return false;
                    }
                }
            }

            public void Reset()
            {
                currentIndex = -1;
                additiveItems.Clear();
            }

            object IEnumerator.Current => current;

            public void Dispose()
            {
                list.DisposeEnumerator(this);
            }
        }

        internal LifetimeList()
        {
        }


        private List<LifetimeListEnumerator> enumerators = new List<LifetimeListEnumerator>();

        /// <summary>
        /// Count of objects in list
        /// </summary>
        public int Count
        {
            get
            {
                var result = cache.Count;
                for (int i = 0; i < sublists.Count; i++)
                {
                    result += sublists[i].cache.Count;
                }
                return result;
            }
        }

        /// <summary>
        /// Indexed access to list for iterations
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index]
        {
            get
            {
                if (TryGetAtIndex(index, out var cached))
                {
                    return (T)cached;
                }
                throw new IndexOutOfRangeException(typeof(T).FullName);
            }
        }

        internal void Add(T lifetime)
        {
            var index = cache.Count;
            cache.Add(lifetime);

            for (int i = 0; i < enumerators.Count; i++)
            {
                enumerators[i].ItemAdded(lifetime, index);
            }

            ItemAdded?.Invoke(this, lifetime, index);
        }

        internal void Remove(T lifetime)
        {
            var index = cache.IndexOf(lifetime);
            if (index >= 0)
            {
                cache.RemoveAtSwapBack(index);
                ItemRemoved?.Invoke(this, lifetime, index);
            }
        }

        /// <summary>
        /// List conversion
        /// </summary>
        /// <returns>List of contained objects</returns>
        public List<T> ToList()
        {
            var result = new List<T>(Count);
            for (int i = 0; i < cache.Count; i++)
            {
                result.Add((T)cache[i]);
            }
            for (int i = 0; i < sublists.Count; i++)
            {
                var sublist = sublists[i];
                for (int j = 0; j < sublist.cache.Count; j++)
                {
                    result.Add((T)sublist.cache[j]);
                }
            }
            return result;

        }

        internal override bool TryGetAtIndex(int index, out ILifetime cached)
        {
            if (index < cache.Count)
            {
                cached = cache[index];
                return true;
            }
            else
            {
                var indexForSublist = index - cache.Count;
                for (int i = 0; i < sublists.Count; i++)
                {
                    if (indexForSublist < sublists[i].cache.Count)
                    {
                        cached = sublists[i].cache[indexForSublist];
                        return true;
                    }
                    else
                    {
                        indexForSublist -= sublists[i].cache.Count;
                    }
                }
            }
            cached = default(ILifetime);
            return false;
        }


        public IEnumerator<T> GetEnumerator()
        {
            var enumerator = new LifetimeListEnumerator(this);
            enumerators.Add(enumerator);
            return enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            var enumerator = new LifetimeListEnumerator(this);
            enumerators.Add(enumerator);
            return enumerator;
        }

        private void DisposeEnumerator(LifetimeListEnumerator lifetimeListEnumerator)
        {
            enumerators.RemoveSwapBack(lifetimeListEnumerator);
        }

        internal override void Initialize()
        {
            for (int i = 0; i < sublists.Count; i++)
            {
                sublists[i].ItemAdded += OnItemAdded;
                sublists[i].ItemRemoved += OnItemRemoved;
            }
        }

        private void OnItemAdded(LifetimeListBase arg1, ILifetime arg2, int arg3)
        {
            var sublistIndex = sublists.IndexOf(arg1);
            var indexOffset = cache.Count;
            for (int i = 0; i < sublistIndex; i++)
            {
                indexOffset += sublists[i].cache.Count;
            }
            var lifetime = (T)arg2;
            for (int i = 0; i < enumerators.Count; i++)
            {
                enumerators[i].ItemAdded(lifetime, arg3);
            }
        }

        private void OnItemRemoved(LifetimeListBase arg1, ILifetime arg2, int arg3)
        {
            var sublistIndex = sublists.IndexOf(arg1);
            var indexOffset = cache.Count;
            for (int i = 0; i < sublistIndex; i++)
            {
                indexOffset += sublists[i].cache.Count;
            }
            var lifetime = (T)arg2;
            for (int i = 0; i < enumerators.Count; i++)
            {
                enumerators[i].ItemRemoved(lifetime, arg3);
            }
        }
    }
}
