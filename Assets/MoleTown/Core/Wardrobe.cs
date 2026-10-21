using System;
using System.Collections.Generic;

namespace MoleTown
{
    // 玩家拥有哪些服装、当前穿戴什么
    public class Wardrobe
    {
        readonly HashSet<string> owned = new();

        public string Hat { get; private set; } = CosmeticDatabase.DefaultHat;
        public string Outfit { get; private set; } = CosmeticDatabase.NoOutfit;
        public event Action Changed;

        public Wardrobe() => GrantFreeItems();

        void GrantFreeItems()
        {
            foreach (var item in CosmeticDatabase.All)
                if (item.price == 0) owned.Add(item.id);
        }

        public bool Owns(string id) => owned.Contains(id);

        public bool IsEquipped(string id) => Hat == id || Outfit == id;

        public bool TryBuy(CosmeticDef item, Inventory inventory)
        {
            if (Owns(item.id)) return true;
            if (!inventory.TrySpend(item.price)) return false;
            owned.Add(item.id);
            Changed?.Invoke();
            return true;
        }

        public bool Equip(string id)
        {
            var item = CosmeticDatabase.Get(id);
            if (item == null || !Owns(id)) return false;
            if (item.slot == CosmeticSlot.Hat) Hat = id;
            else Outfit = id;
            Changed?.Invoke();
            return true;
        }

        public void WriteTo(SaveData data)
        {
            data.ownedCosmetics = new List<string>(owned);
            data.hat = Hat;
            data.outfit = Outfit;
        }

        public void ReadFrom(SaveData data)
        {
            owned.Clear();
            GrantFreeItems();
            if (data.ownedCosmetics != null)
                foreach (var id in data.ownedCosmetics)
                    if (CosmeticDatabase.Get(id) != null) owned.Add(id);
            Hat = IsValid(data.hat, CosmeticSlot.Hat) ? data.hat : CosmeticDatabase.DefaultHat;
            Outfit = IsValid(data.outfit, CosmeticSlot.Outfit) ? data.outfit : CosmeticDatabase.NoOutfit;
            Changed?.Invoke();
        }

        bool IsValid(string id, CosmeticSlot slot)
        {
            var item = CosmeticDatabase.Get(id);
            return item != null && item.slot == slot && Owns(id);
        }
    }
}
