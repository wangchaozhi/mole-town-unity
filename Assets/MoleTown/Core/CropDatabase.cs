using System;
using UnityEngine;

namespace MoleTown
{
    public enum CropShape { Berry, Root, Gourd }

    [Serializable]
    public class CropDef
    {
        public string id;
        public string displayName;
        public int seedPrice;
        public int sellPrice;
        public int yield;
        public float stageSeconds;   // 每个生长阶段需要的秒数（浇水后开始计时）
        public Color fruitColor;
        public Color leafColor;
        public CropShape shape;
    }

    public static class CropDatabase
    {
        // 0 = 刚播种, 1 = 发芽, 2 = 长大, 3 = 成熟
        public const int RipeStage = 3;

        public static readonly CropDef[] All =
        {
            new CropDef { id = "strawberry", displayName = "草莓", seedPrice = 5, sellPrice = 4, yield = 2, stageSeconds = 6,
                fruitColor = new Color(0.95f, 0.25f, 0.3f), leafColor = new Color(0.35f, 0.72f, 0.3f), shape = CropShape.Berry },
            new CropDef { id = "carrot", displayName = "胡萝卜", seedPrice = 10, sellPrice = 9, yield = 2, stageSeconds = 10,
                fruitColor = new Color(1f, 0.55f, 0.15f), leafColor = new Color(0.3f, 0.66f, 0.25f), shape = CropShape.Root },
            new CropDef { id = "pumpkin", displayName = "南瓜", seedPrice = 20, sellPrice = 50, yield = 1, stageSeconds = 15,
                fruitColor = new Color(1f, 0.62f, 0.12f), leafColor = new Color(0.28f, 0.6f, 0.3f), shape = CropShape.Gourd },
        };

        public static CropDef Get(string id)
        {
            foreach (var crop in All)
                if (crop.id == id) return crop;
            return null;
        }
    }
}
