using System;
using System.Collections.Generic;

namespace MoleTown
{
    public class Inventory
    {
        public int Coins { get; private set; }
        public event Action Changed;

        readonly Dictionary<string, int> seeds = new();
        readonly Dictionary<string, int> crops = new();

        public Inventory(int coins = 0) { Coins = coins; }

        public int SeedCount(string id) => seeds.TryGetValue(id, out var n) ? n : 0;
        public int CropCount(string id) => crops.TryGetValue(id, out var n) ? n : 0;

        public void AddCoins(int amount) { Coins += amount; Changed?.Invoke(); }
        public void AddSeeds(string id, int count) { seeds[id] = SeedCount(id) + count; Changed?.Invoke(); }
        public void AddCrops(string id, int count) { crops[id] = CropCount(id) + count; Changed?.Invoke(); }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || Coins < amount) return false;
            Coins -= amount;
            Changed?.Invoke();
            return true;
        }

        public bool TryUseSeed(string id)
        {
            if (SeedCount(id) <= 0) return false;
            seeds[id]--;
            Changed?.Invoke();
            return true;
        }

        public bool TryBuySeeds(CropDef crop, int count)
        {
            int cost = crop.seedPrice * count;
            if (count <= 0 || Coins < cost) return false;
            Coins -= cost;
            seeds[crop.id] = SeedCount(crop.id) + count;
            Changed?.Invoke();
            return true;
        }

        public int SellCrops(CropDef crop)
        {
            int count = CropCount(crop.id);
            if (count == 0) return 0;
            crops[crop.id] = 0;
            int earned = count * crop.sellPrice;
            Coins += earned;
            Changed?.Invoke();
            return earned;
        }

        public int SellAllValue()
        {
            int total = 0;
            foreach (var crop in CropDatabase.All) total += CropCount(crop.id) * crop.sellPrice;
            return total;
        }

        public int SellAll()
        {
            int total = SellAllValue();
            if (total == 0) return 0;
            foreach (var crop in CropDatabase.All) crops[crop.id] = 0;
            Coins += total;
            Changed?.Invoke();
            return total;
        }

        public void WriteTo(SaveData data)
        {
            data.coins = Coins;
            data.seeds.Clear();
            data.crops.Clear();
            foreach (var kv in seeds) data.seeds.Add(new CountEntry { id = kv.Key, count = kv.Value });
            foreach (var kv in crops) data.crops.Add(new CountEntry { id = kv.Key, count = kv.Value });
        }

        public void ReadFrom(SaveData data)
        {
            Coins = data.coins;
            seeds.Clear();
            crops.Clear();
            foreach (var e in data.seeds) seeds[e.id] = e.count;
            foreach (var e in data.crops) crops[e.id] = e.count;
            Changed?.Invoke();
        }
    }
}
