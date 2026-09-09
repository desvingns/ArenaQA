using System;
using System.Collections.Generic;

namespace Arena.Economy
{
    [Serializable]
    public class LootEntry
    {
        public string Id;
        public int Weight;

        public LootEntry(string id, int weight)
        {
            Id = id;
            Weight = weight;
        }
    }

    public class LootTable
    {
        private readonly List<LootEntry> _entries = new List<LootEntry>();

        public LootTable(IEnumerable<LootEntry> entries)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            _entries.AddRange(entries);
        }

        public IReadOnlyList<LootEntry> Entries => _entries;

        public int TotalWeight
        {
            get
            {
                int total = 0;
                for (int i = 0; i < _entries.Count; i++)
                {
                    total += _entries[i].Weight;
                }

                return total;
            }
        }

        public string Pick(int roll)
        {
            int cumulative = 0;

            for (int i = 0; i < _entries.Count; i++)
            {
                cumulative += _entries[i].Weight;

                if (roll <= cumulative)
                {
                    return _entries[i].Id;
                }
            }

            return _entries[_entries.Count - 1].Id;
        }
    }
}
