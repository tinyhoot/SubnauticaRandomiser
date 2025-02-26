using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SubnauticaRandomiser.Interfaces;

namespace SubnauticaRandomiser.Objects
{
    /// <summary>
    /// Represents a loot drop table with odds for each possible drop. Each item can only appear in the table once.<br/>
    /// Odds do not necessarily have to sum up to 1.0, but are still relational to one another.
    /// </summary>
    [Serializable]
    internal class LootTable<T> : ICollection<LootTableEntry<T>>
    {
        // This is not a dictionary because T isn't necessarily easily hashable, i.e. may not be suitable as a key.
        private readonly List<LootTableEntry<T>> _entries = new List<LootTableEntry<T>>();
        public int Count => _entries.Count;
        public bool IsReadOnly => false;

        public IEnumerator<LootTableEntry<T>> GetEnumerator()
        {
            return _entries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Adds an entry to the loot table. Has no effect if an entry with the same item already exists.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <param name="odds">The odds with which to drop the item.</param>
        public void Add(T item, double odds)
        {
            if (Contains(item))
                return;
            _entries.Add(new LootTableEntry<T>(item, odds));
        }

        /// <summary>
        /// Adds an entry to the loot table. Has no effect if an entry with the same item already exists.
        /// </summary>
        public void Add(LootTableEntry<T> item)
        {
            if (Contains(item))
                return;
            _entries.Add(item);
        }

        public void Clear()
        {
            _entries.Clear();
        }

        public bool Contains(T item)
        {
            return _entries.Any(entry => entry.Item.Equals(item));
        }

        public bool Contains(LootTableEntry<T> item)
        {
            return Contains(item.Item);
        }

        public void CopyTo(LootTableEntry<T>[] array, int arrayIndex)
        {
            _entries.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Get a random item from the table.
        /// </summary>
        /// <param name="random">A random instance to choose an item with.</param>
        /// <exception cref="IndexOutOfRangeException">Raised if there are no entries in the table.</exception>
        public T Drop(IRandomHandler random)
        {
            if (_entries.Count == 0)
                throw new IndexOutOfRangeException("Cannot drop from a table with no entries!");
            
            double total = TotalWeights();
            double chosenWeight = total * random.NextDouble();
            double currentWeight = 0;
            // Add up the weights of the entries until the random value is exceeded, and then choose the last one.
            foreach (var entry in _entries)
            {
                currentWeight += entry.DropRate;
                if (chosenWeight <= currentWeight)
                    return entry.Item;
            }
            
            // Fallback, just in case the above somehow fails.
            return _entries.Last().Item;
        }

        /// <summary>
        /// Removes the first occurrence of an item from the table.
        /// </summary>
        public bool Remove(T item)
        {
            int idx = _entries.FindIndex(entry => entry.Item.Equals(item));
            if (idx < 0)
                return false;
            
            _entries.RemoveAt(idx);
            return true;
        }

        public bool Remove(LootTableEntry<T> item)
        {
            return Remove(item.Item);
        }

        /// <summary>
        /// Calculates the total of all drop rates summed together.
        /// </summary>
        public double TotalWeights()
        {
            double total = 0;
            foreach (var entry in _entries)
            {
                total += entry.DropRate;
            }

            return total;
        }
    }

    [Serializable]
    public struct LootTableEntry<T>
    {
        [JsonProperty] public readonly T Item;
        [JsonProperty] public readonly double DropRate;

        public LootTableEntry(T item, double odds)
        {
            if (odds < 0)
                throw new ArgumentOutOfRangeException(nameof(odds), "DropRate cannot be negative!");
            Item = item;
            DropRate = odds;
        }
    }
}