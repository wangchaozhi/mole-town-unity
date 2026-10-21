using UnityEngine;

namespace MoleTown
{
    // 用球体拼出圆滚滚的小鼹鼠：大粉鼻子、小黑眼睛、腮红、粉色小爪
    // 帽子和衣服挂在 HatSlot / OutfitSlot 下，可以随时替换
    public static class MoleBuilder
    {
        static readonly Color Nose = new(1f, 0.58f, 0.64f);
        static readonly Color Paw = new(1f, 0.76f, 0.74f);
        static readonly Color Eye = new(0.12f, 0.08f, 0.1f);
        static readonly Color Blush = new(1f, 0.62f, 0.68f);
        static readonly Color Gold = new(1f, 0.82f, 0.25f);

        public static Transform Build(Transform parent, Color fur, string hatId = CosmeticDatabase.NoHat, string outfitId = CosmeticDatabase.NoOutfit,
            Color? hatTint = null, Color? outfitTint = null)
        {
            var v = Prim.Group("Visual", parent, Vector3.zero);
            var belly = Color.Lerp(fur, Color.white, 0.45f);

            Prim.Make(PrimitiveType.Sphere, v, new(0f, 0.55f, 0f), new(1f, 0.95f, 0.9f), fur, 0.3f, name: "Body");
            Prim.Make(PrimitiveType.Sphere, v, new(0f, 0.48f, 0.2f), new(0.68f, 0.62f, 0.55f), belly, 0f, name: "Belly");
            Prim.Make(PrimitiveType.Sphere, v, new(0f, 0.66f, 0.44f), new(0.34f, 0.27f, 0.28f), Nose, 0.2f, name: "Nose");

            for (int s = -1; s <= 1; s += 2)
            {
                Prim.Make(PrimitiveType.Sphere, v, new(0.17f * s, 0.83f, 0.37f), new(0.09f, 0.13f, 0.06f), Eye, 0f, name: "Eye");
                Prim.Make(PrimitiveType.Sphere, v, new(0.155f * s, 0.86f, 0.405f), Vector3.one * 0.035f, Color.white, 0f, name: "EyeShine");
                Prim.Make(PrimitiveType.Sphere, v, new(0.3f * s, 0.64f, 0.34f), new(0.15f, 0.08f, 0.05f), Blush, 0f, name: "Blush");
                Prim.Make(PrimitiveType.Sphere, v, new(0.4f * s, 0.92f, -0.02f), new(0.2f, 0.2f, 0.12f), fur, 0.2f, name: "Ear");
                Prim.Make(PrimitiveType.Sphere, v, new(0.5f * s, 0.45f, 0.15f), Vector3.one * 0.2f, Paw, 0.2f, name: "Hand");
                Prim.Make(PrimitiveType.Sphere, v, new(0.22f * s, 0.07f, 0.12f), new(0.26f, 0.12f, 0.32f), Paw, 0.2f, name: "Foot");
            }

            Prim.Group("HatSlot", v, Vector3.zero);
            Prim.Group("OutfitSlot", v, Vector3.zero);
            ApplyHat(v, hatId, hatTint);
            ApplyOutfit(v, outfitId, outfitTint);
            return v;
        }

        public static void ApplyHat(Transform visual, string id, Color? tint = null)
        {
            var slot = visual.Find("HatSlot");
            Prim.ClearChildren(slot);
            var item = CosmeticDatabase.Get(id);
            if (item == null || item.slot != CosmeticSlot.Hat || id == CosmeticDatabase.NoHat) return;

            var c = tint ?? item.color;
            var g = Prim.Group("Hat_" + id, slot, Vector3.zero);
            switch (id)
            {
                case "cap":
                    Prim.Make(PrimitiveType.Sphere, g, new(0f, 1f, -0.02f), new(0.66f, 0.34f, 0.62f), c, 0.25f);
                    Prim.Make(PrimitiveType.Cylinder, g, new(0f, 0.99f, 0.24f), new(0.46f, 0.015f, 0.32f), c, 0.2f);
                    break;
                case "flower":
                    var flower = Prim.Group("Flower", g, new(0.22f, 1.02f, 0f));
                    Prim.Make(PrimitiveType.Sphere, flower, Vector3.zero, Vector3.one * 0.12f, new Color(1f, 0.88f, 0.3f), 0.15f);
                    for (int i = 0; i < 5; i++)
                    {
                        var dir = Quaternion.Euler(0f, i * 72f, 0f) * Vector3.forward;
                        Prim.Make(PrimitiveType.Sphere, flower, dir * 0.1f, new(0.12f, 0.05f, 0.12f), c, 0.15f);
                    }
                    break;
                case "straw":
                    Prim.Make(PrimitiveType.Cylinder, g, new(0f, 1f, 0f), new(0.95f, 0.02f, 0.95f), c, 0.2f);
                    Prim.Make(PrimitiveType.Cylinder, g, new(0f, 1.1f, 0f), new(0.48f, 0.1f, 0.48f), c, 0.2f);
                    Prim.Make(PrimitiveType.Cylinder, g, new(0f, 1.06f, 0f), new(0.5f, 0.03f, 0.5f), new Color(0.85f, 0.3f, 0.3f), 0f);
                    break;
                case "bunny":
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Prim.Make(PrimitiveType.Sphere, g, new(0.18f * s, 1.25f, -0.05f), new(0.17f, 0.46f, 0.1f), c, 0.2f, new Vector3(0f, 0f, -12f * s));
                        Prim.Make(PrimitiveType.Sphere, g, new(0.18f * s, 1.25f, -0.01f), new(0.09f, 0.34f, 0.04f), Blush, 0f, new Vector3(0f, 0f, -12f * s));
                    }
                    break;
                case "chef":
                    Prim.Make(PrimitiveType.Cylinder, g, new(0f, 1.06f, 0f), new(0.56f, 0.1f, 0.56f), c, 0.2f);
                    Prim.Make(PrimitiveType.Sphere, g, new(0f, 1.32f, 0f), new(0.66f, 0.4f, 0.66f), c, 0.2f);
                    for (int s = -1; s <= 1; s += 2)
                        Prim.Make(PrimitiveType.Sphere, g, new(0.2f * s, 1.28f, 0f), Vector3.one * 0.38f, c, 0.2f);
                    break;
                case "wizard":
                    // 用逐层变细的圆柱拼一个尖顶
                    Prim.Make(PrimitiveType.Cylinder, g, new(0f, 1f, 0f), new(1f, 0.02f, 1f), c, 0.2f);
                    float[] radii = { 0.56f, 0.44f, 0.32f, 0.2f };
                    for (int i = 0; i < radii.Length; i++)
                        Prim.Make(PrimitiveType.Cylinder, g, new(0f, 1.1f + i * 0.17f, -0.02f * i), new(radii[i], 0.09f, radii[i]), c, 0.2f);
                    Prim.Make(PrimitiveType.Sphere, g, new(0f, 1.8f, -0.1f), Vector3.one * 0.14f, Gold, 0.15f);
                    Prim.Make(PrimitiveType.Sphere, g, new(0.12f, 1.25f, 0.2f), Vector3.one * 0.08f, Gold, 0f);
                    Prim.Make(PrimitiveType.Sphere, g, new(-0.1f, 1.45f, 0.14f), Vector3.one * 0.06f, Gold, 0f);
                    break;
                case "crown":
                    Prim.Make(PrimitiveType.Cylinder, g, new(0f, 1.06f, 0f), new(0.5f, 0.08f, 0.5f), c, 0.2f);
                    for (int i = 0; i < 6; i++)
                    {
                        var dir = Quaternion.Euler(0f, i * 60f, 0f) * Vector3.forward;
                        Prim.Make(PrimitiveType.Sphere, g, new Vector3(0f, 1.18f, 0f) + dir * 0.22f, new(0.1f, 0.16f, 0.1f), c, 0.12f);
                    }
                    Prim.Make(PrimitiveType.Sphere, g, new(0f, 1.06f, 0.25f), Vector3.one * 0.09f, new Color(0.9f, 0.2f, 0.35f), 0.1f);
                    break;
            }
            Prim.SetLayerRecursive(g.gameObject, visual.gameObject.layer);
        }

        public static void ApplyOutfit(Transform visual, string id, Color? tint = null)
        {
            var slot = visual.Find("OutfitSlot");
            Prim.ClearChildren(slot);
            var item = CosmeticDatabase.Get(id);
            bool valid = item != null && item.slot == CosmeticSlot.Outfit;
            var belly = visual.Find("Belly");
            if (belly) belly.gameObject.SetActive(!(valid && item.coversBelly));
            if (!valid || id == CosmeticDatabase.NoOutfit) return;

            var c = tint ?? item.color;
            var g = Prim.Group("Outfit_" + id, slot, Vector3.zero);
            switch (id)
            {
                case "bowtie":
                    Prim.Make(PrimitiveType.Sphere, g, new(0f, 0.44f, 0.5f), Vector3.one * 0.08f, c, 0.12f);
                    for (int s = -1; s <= 1; s += 2)
                        Prim.Make(PrimitiveType.Sphere, g, new(0.1f * s, 0.44f, 0.48f), new(0.14f, 0.1f, 0.06f), c, 0.12f, new Vector3(0f, 0f, 15f * s));
                    break;
                case "sweater":
                    // 比身体略大的椭球包住下半身，看起来像穿了件衣服
                    Prim.Make(PrimitiveType.Sphere, g, new(0f, 0.42f, 0f), new(1.06f, 0.72f, 0.96f), c, 0.25f);
                    Prim.Make(PrimitiveType.Sphere, g, new(0f, 0.12f, 0f), new(0.98f, 0.18f, 0.9f), Color.Lerp(c, Color.white, 0.5f), 0.15f);
                    Prim.Make(PrimitiveType.Sphere, g, new(0.16f, 0.42f, 0.47f), new(0.12f, 0.12f, 0.04f), new Color(0.95f, 0.45f, 0.5f), 0f);
                    break;
                case "overalls":
                    Prim.Make(PrimitiveType.Sphere, g, new(0f, 0.36f, 0f), new(1.07f, 0.62f, 0.97f), c, 0.25f);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Prim.Make(PrimitiveType.Cube, g, new(0.2f * s, 0.66f, 0.4f), new(0.08f, 0.24f, 0.03f), c, 0.1f, new Vector3(-22f, 0f, 0f));
                        Prim.Make(PrimitiveType.Sphere, g, new(0.2f * s, 0.56f, 0.46f), Vector3.one * 0.06f, Gold, 0f);
                    }
                    break;
                case "cape":
                    Prim.Make(PrimitiveType.Sphere, g, new(0f, 0.6f, -0.2f), new(1.08f, 0.98f, 0.62f), c, 0.25f);
                    Prim.Make(PrimitiveType.Sphere, g, new(0f, 0.95f, -0.12f), new(0.8f, 0.14f, 0.5f), Gold, 0.15f);
                    break;
            }
            Prim.SetLayerRecursive(g.gameObject, visual.gameObject.layer);
        }
    }
}
