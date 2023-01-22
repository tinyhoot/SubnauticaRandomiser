using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        private readonly List<LootTableEntry<T>> _entries;
        public int Count => _entries.Count;
        public bool IsReadOnly => false;

        public LootTable()
        {
            _entries = new List<LootTableEntry<T>>();
        }

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
            var entry = _entries.Find(entry => entry.Item.Equals(item));
            return _entries.Remove(entry);
        }

        public bool Remove(LootTableEntry<T> item)
        {
            return _entries.Remove(item);
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
        public readonly T Item;
        public readonly double DropRate;

        public LootTableEntry(T item, double odds)
        {
            if (odds < 0)
                throw new ArgumentOutOfRangeException(nameof(odds), "DropRate cannot be negative!");
            Item = item;
            DropRate = odds;
        }
    }
}