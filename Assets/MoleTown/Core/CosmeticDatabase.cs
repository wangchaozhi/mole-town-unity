using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoleTown
{
    public enum CosmeticSlot { Hat, Outfit }

    [Serializable]
    public class CosmeticDef
    {
        public string id;
        public string displayName;
        public CosmeticSlot slot;
        public int price;          // 0 表示默认就有
        public Color color;
        public bool coversBelly;   // 穿上后挡住肚皮，隐藏肚皮避免穿模
    }

    public static class CosmeticDatabase
    {
        public const string NoHat = "hat_none";
        public const string NoOutfit = "outfit_none";
        public const string DefaultHat = "cap";

        public static readonly CosmeticDef[] All =
        {
            new CosmeticDef { id = NoHat, displayName = "不戴帽子", slot = CosmeticSlot.Hat, price = 0, color = new Color(0.85f, 0.85f, 0.85f) },
            new CosmeticDef { id = "cap", displayName = "红色棒球帽", slot = CosmeticSlot.Hat, price = 0, color = new Color(0.95f, 0.3f, 0.3f) },
            new CosmeticDef { id = "flower", displayName = "小花发夹", slot = CosmeticSlot.Hat, price = 20, color = new Color(1f, 0.55f, 0.7f) },
            new CosmeticDef { id = "straw", displayName = "草帽", slot = CosmeticSlot.Hat, price = 30, color = new Color(0.95f, 0.82f, 0.5f) },
            new CosmeticDef { id = "bunny", displayName = "兔耳朵", slot = CosmeticSlot.Hat, price = 40, color = Color.white },
            new CosmeticDef { id = "chef", displayName = "厨师帽", slot = CosmeticSlot.Hat, price = 50, color = new Color(0.98f, 0.98f, 0.96f) },
            new CosmeticDef { id = "wizard", displayName = "魔法帽", slot = CosmeticSlot.Hat, price = 80, color = new Color(0.5f, 0.35f, 0.85f) },
            new CosmeticDef { id = "crown", displayName = "小皇冠", slot = CosmeticSlot.Hat, price = 150, color = new Color(1f, 0.8f, 0.2f) },

            new CosmeticDef { id = NoOutfit, displayName = "不穿衣服", slot = CosmeticSlot.Outfit, price = 0, color = new Color(0.85f, 0.85f, 0.85f) },
            new CosmeticDef { id = "bowtie", displayName = "红领结", slot = CosmeticSlot.Outfit, price = 15, color = new Color(0.9f, 0.25f, 0.3f) },
            new CosmeticDef { id = "sweater", displayName = "黄毛衣", slot = CosmeticSlot.Outfit, price = 35, color = new Color(1f, 0.8f, 0.3f), coversBelly = true },
            new CosmeticDef { id = "overalls", displayName = "背带裤", slot = CosmeticSlot.Outfit, price = 45, color = new Color(0.35f, 0.55f, 0.9f), coversBelly = true },
            new CosmeticDef { id = "cape", displayName = "英雄披风", slot = CosmeticSlot.Outfit, price = 60, color = new Color(0.9f, 0.25f, 0.25f) },
        };

        public static CosmeticDef Get(string id)
        {
            foreach (var item in All)
                if (item.id == id) return item;
            return null;
        }

        public static IEnumerable<CosmeticDef> InSlot(CosmeticSlot slot) => All.Where(c => c.slot == slot);
    }
}
