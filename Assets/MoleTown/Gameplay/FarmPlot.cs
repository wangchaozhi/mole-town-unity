using System;
using UnityEngine;

namespace MoleTown
{
    // 一块田地：播种 → 浇水 → 生长一个阶段 → 再浇水 …… → 成熟收获
    [SelectionBase]
    public class FarmPlot : MonoBehaviour, IInteractable
    {
        public string plotId;

        public string CropId { get; private set; }
        public int Stage { get; private set; }
        public bool Watered { get; private set; }
        public double StageStart { get; private set; }

        public bool IsEmpty => string.IsNullOrEmpty(CropId);
        public bool IsRipe => !IsEmpty && Stage >= CropDatabase.RipeStage;
        public bool NeedsWater => !IsEmpty && !IsRipe && !Watered;
        public CropDef Crop => IsEmpty ? null : CropDatabase.Get(CropId);

        public float SecondsToNextStage => IsEmpty || IsRipe || !Watered
            ? 0f
            : Mathf.Max(0f, (float)(Crop.stageSeconds - (GameClock.Now - StageStart)));

        public event Action<FarmPlot> StateChanged;

        static readonly Color DrySoil = new(0.62f, 0.45f, 0.32f);
        static readonly Color WetSoil = new(0.42f, 0.29f, 0.22f);
        static readonly Color WaterBlue = new(0.35f, 0.7f, 1f);
        static readonly Color Gold = new(1f, 0.82f, 0.25f);
        static readonly Vector3[] Spots = { new(-0.45f, 0f, -0.45f), new(0.45f, 0f, -0.45f), new(-0.45f, 0f, 0.45f), new(0.45f, 0f, 0.45f) };

        Renderer soil;
        Transform cropRoot, waterIcon, ripeIcon;
        float popTime = 1f;
        float phase;

        public Vector3 InteractPosition => transform.position;
        public float InteractDistance => 1.8f;

        void Awake()
        {
            phase = UnityEngine.Random.value * 10f;
            RefreshVisuals();
        }

        void Update()
        {
            Tick();
            Animate();
        }

        public void Tick()
        {
            if (IsEmpty || IsRipe || !Watered) return;
            if (GameClock.Now - StageStart < Crop.stageSeconds) return;
            Stage++;
            Watered = false;
            StageStart = GameClock.Now;
            popTime = 0f;
            Changed();
        }

        public bool Plant(string cropId, Inventory inventory)
        {
            if (!IsEmpty || CropDatabase.Get(cropId) == null || !inventory.TryUseSeed(cropId)) return false;
            CropId = cropId;
            Stage = 0;
            Watered = false;
            StageStart = GameClock.Now;
            popTime = 0f;
            Changed();
            return true;
        }

        public bool Water()
        {
            if (!NeedsWater) return false;
            Watered = true;
            StageStart = GameClock.Now;
            Changed();
            return true;
        }

        public int Harvest(Inventory inventory)
        {
            if (!IsRipe) return 0;
            var crop = Crop;
            inventory.AddCrops(crop.id, crop.yield);
            CropId = null;
            Stage = 0;
            Watered = false;
            Changed();
            return crop.yield;
        }

        public string HoverHint
        {
            get
            {
                if (IsEmpty)
                {
                    var seed = GameManager.Instance ? CropDatabase.Get(GameManager.Instance.SelectedSeed) : null;
                    return seed != null ? $"空地 · 点击种下{seed.displayName}" : "空地";
                }
                if (IsRipe) return $"{Crop.displayName} · 可以收获啦！";
                if (NeedsWater) return $"{Crop.displayName} · 需要浇水";
                return $"{Crop.displayName} · 生长中，还需 {Mathf.CeilToInt(SecondsToNextStage)} 秒";
            }
        }

        public void Interact()
        {
            var gm = GameManager.Instance;
            var hud = GameUI.Current;
            if (!gm) return;

            if (IsEmpty)
            {
                var seed = CropDatabase.Get(gm.SelectedSeed);
                if (Plant(seed.id, gm.Inventory)) hud?.Toast($"种下了{seed.displayName}，记得浇水哦");
                else hud?.Toast($"没有{seed.displayName}种子了，去杂货铺买一些吧");
            }
            else if (IsRipe)
            {
                string cropName = Crop.displayName;
                int count = Harvest(gm.Inventory);
                hud?.Toast($"收获了 {cropName} ×{count}！");
            }
            else if (NeedsWater)
            {
                Water();
                hud?.Toast("浇好水了，快快长大吧~");
            }
            else
            {
                hud?.Toast($"{Crop.displayName}正在生长，还需 {Mathf.CeilToInt(SecondsToNextStage)} 秒");
            }
        }

        public PlotSave ToSave() => new()
        {
            plotId = plotId, cropId = CropId, stage = Stage, watered = Watered, stageStart = StageStart
        };

        public void LoadFrom(PlotSave save)
        {
            CropId = CropDatabase.Get(save.cropId) != null ? save.cropId : null;
            Stage = IsEmpty ? 0 : Mathf.Clamp(save.stage, 0, CropDatabase.RipeStage);
            Watered = !IsEmpty && save.watered;
            StageStart = save.stageStart;
            RefreshVisuals();
        }

        void Changed()
        {
            RefreshVisuals();
            StateChanged?.Invoke(this);
        }

        // ---------- 外观 ----------

        public void EnsureVisuals()
        {
            if (soil && cropRoot && waterIcon && ripeIcon) return;

            var col = GetComponent<BoxCollider>();
            if (!col) col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.15f, 0f);
            col.size = new Vector3(2f, 0.3f, 2f);

            soil = FindOrMake("Soil", () =>
                Prim.Make(PrimitiveType.Cube, transform, new(0f, 0.1f, 0f), new(1.9f, 0.2f, 1.9f), DrySoil, 0.2f, name: "Soil").transform)
                .GetComponent<Renderer>();
            cropRoot = FindOrMake("Crop", () => Prim.Group("Crop", transform, new(0f, 0.2f, 0f)));
            waterIcon = FindOrMake("WaterIcon", () =>
                Prim.Make(PrimitiveType.Sphere, transform, new(0f, 1.1f, 0f), new(0.28f, 0.36f, 0.28f), WaterBlue, 0.2f, name: "WaterIcon").transform);
            ripeIcon = FindOrMake("RipeIcon", () =>
            {
                var g = Prim.Group("RipeIcon", transform, new(0f, 1.2f, 0f));
                Prim.Make(PrimitiveType.Cube, g, Vector3.zero, new(0.32f, 0.32f, 0.08f), Gold, 0.15f, new Vector3(0f, 0f, 45f));
                return g;
            });
        }

        Transform FindOrMake(string childName, Func<Transform> make)
        {
            var t = transform.Find(childName);
            return t ? t : make();
        }

        void RefreshVisuals()
        {
            EnsureVisuals();
            soil.sharedMaterial = ToonMaterials.Get(Watered ? WetSoil : DrySoil, 0.2f, true);
            Prim.ClearChildren(cropRoot);
            if (!IsEmpty) BuildCrop(Crop, Stage);
            waterIcon.gameObject.SetActive(NeedsWater);
            ripeIcon.gameObject.SetActive(IsRipe);
        }

        void BuildCrop(CropDef crop, int stage)
        {
            if (crop.shape == CropShape.Gourd && stage >= 2)
            {
                BuildGourd(crop, stage);
                return;
            }
            foreach (var p in Spots)
            {
                switch (stage)
                {
                    case 0:
                        Prim.Make(PrimitiveType.Sphere, cropRoot, p + new Vector3(0f, 0.02f, 0f), new(0.3f, 0.1f, 0.3f), new Color(0.45f, 0.3f, 0.2f), 0.12f);
                        break;
                    case 1:
                        Leaves(p, crop.leafColor, 0.6f);
                        break;
                    case 2:
                        Leaves(p, crop.leafColor, 1f);
                        break;
                    default:
                        BuildRipe(p, crop);
                        break;
                }
            }
        }

        void Leaves(Vector3 p, Color leaf, float size)
        {
            Prim.Make(PrimitiveType.Cylinder, cropRoot, p + new Vector3(0f, 0.12f * size, 0f), new(0.04f, 0.12f * size, 0.04f), leaf, 0.1f);
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 120f;
                var dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                Prim.Make(PrimitiveType.Sphere, cropRoot, p + new Vector3(0f, 0.24f * size, 0f) + dir * (0.1f * size),
                    new Vector3(0.12f, 0.04f, 0.24f) * size, leaf, 0.12f, new Vector3(-25f, angle, 0f));
            }
        }

        void BuildRipe(Vector3 p, CropDef crop)
        {
            if (crop.shape == CropShape.Berry)
            {
                Leaves(p, crop.leafColor, 1f);
                for (int i = 0; i < 3; i++)
                {
                    var dir = Quaternion.Euler(0f, i * 120f + 60f, 0f) * Vector3.forward;
                    Prim.Make(PrimitiveType.Sphere, cropRoot, p + new Vector3(0f, 0.14f, 0f) + dir * 0.17f, new(0.16f, 0.18f, 0.16f), crop.fruitColor, 0.15f);
                }
            }
            else
            {
                // 胡萝卜：露出橙色的头和高高的叶子
                Prim.Make(PrimitiveType.Sphere, cropRoot, p + new Vector3(0f, 0.04f, 0f), new(0.24f, 0.3f, 0.24f), crop.fruitColor, 0.15f);
                for (int i = 0; i < 3; i++)
                {
                    float angle = i * 120f;
                    Prim.Make(PrimitiveType.Sphere, cropRoot, p + new Vector3(0f, 0.3f, 0f) + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 0.05f,
                        new(0.07f, 0.32f, 0.07f), crop.leafColor, 0.12f, new Vector3(15f, angle, 0f));
                }
            }
        }

        void BuildGourd(CropDef crop, int stage)
        {
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f + 45f;
                var dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                Prim.Make(PrimitiveType.Sphere, cropRoot, dir * 0.55f + new Vector3(0f, 0.05f, 0f), new(0.45f, 0.06f, 0.6f), crop.leafColor, 0.12f, new Vector3(-10f, angle, 0f));
            }
            if (stage < CropDatabase.RipeStage)
            {
                Prim.Make(PrimitiveType.Sphere, cropRoot, new(0f, 0.12f, 0f), Vector3.one * 0.3f, Color.Lerp(crop.fruitColor, crop.leafColor, 0.6f), 0.15f);
                return;
            }
            // 成熟南瓜：几个椭球拼出棱，再加个瓜蒂
            Prim.Make(PrimitiveType.Sphere, cropRoot, new(0f, 0.32f, 0f), new(0.95f, 0.62f, 0.95f), crop.fruitColor, 0.2f);
            for (int i = 0; i < 4; i++)
            {
                var dir = Quaternion.Euler(0f, i * 90f, 0f) * Vector3.forward;
                Prim.Make(PrimitiveType.Sphere, cropRoot, dir * 0.2f + new Vector3(0f, 0.32f, 0f), new(0.6f, 0.64f, 0.6f), crop.fruitColor, 0.2f);
            }
            Prim.Make(PrimitiveType.Cylinder, cropRoot, new(0f, 0.7f, 0f), new(0.08f, 0.1f, 0.08f), new Color(0.45f, 0.35f, 0.2f), 0.15f);
        }

        void Animate()
        {
            float t = Time.time + phase;
            if (waterIcon.gameObject.activeSelf)
                waterIcon.localPosition = new Vector3(0f, 1.1f + Mathf.Sin(t * 3f) * 0.08f, 0f);
            if (ripeIcon.gameObject.activeSelf)
            {
                ripeIcon.localPosition = new Vector3(0f, 1.2f + Mathf.Sin(t * 3f) * 0.08f, 0f);
                ripeIcon.localRotation = Quaternion.Euler(0f, t * 120f, 0f);
            }
            if (popTime < 1f)
            {
                popTime += Time.deltaTime * 4f;
                cropRoot.localScale = Vector3.one * (1f + Mathf.Sin(Mathf.Clamp01(popTime) * Mathf.PI) * 0.25f);
            }
        }
    }
}
