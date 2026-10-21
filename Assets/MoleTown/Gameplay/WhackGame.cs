using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

namespace MoleTown
{
    // 捉虫大作战：九个地洞随机冒出小虫，点中得分，限时 30 秒
    [SelectionBase]
    public class WhackGame : MonoBehaviour, IMiniGame
    {
        public float duration = 30f;
        public float cameraDistance = 11f;

        public bool Active { get; private set; }
        public int Score { get; private set; }
        public float TimeLeft { get; private set; }
        public event Action<WhackGame> Ended;

        public string Title => "捉虫大作战";
        public string Rules => "小虫子在偷吃广场的花草，快把它们赶走！\n\n· 红色瓢虫 +1 分　　· 金色瓢虫 +3 分\n· 别敲到冒头的鼹鼠宝宝，会扣 2 分\n"
                               + $"· 限时 {duration:0} 秒，每 1 分换 1 金豆";
        public int BestScore => GameManager.Instance ? GameManager.Instance.BestWhackScore : 0;

        const int Size = 3;
        const float Spacing = 1.7f;
        const float HiddenY = -0.75f;
        const float ShownY = 0.12f;
        const float PopTime = 0.15f;

        static readonly Color Frame = new(0.58f, 0.38f, 0.26f);
        static readonly Color Turf = new(0.5f, 0.76f, 0.38f);
        static readonly Color Dirt = new(0.62f, 0.45f, 0.32f);
        static readonly Color HoleColor = new(0.2f, 0.13f, 0.1f);

        class Hole
        {
            public Transform root;
            public readonly Dictionary<WhackKind, Transform> critters = new();
            public Transform current;
            public WhackKind kind;
            public bool up, hit;
            public float t, stay;
        }

        readonly List<Hole> holes = new();
        readonly Dictionary<Collider, Hole> byCollider = new();
        float spawnTimer;
        Camera cam;

        public Vector3 Center => transform.position;

        void Awake()
        {
            EnsureVisuals();
            BuildCritters();
        }

        public void EnsureVisuals()
        {
            if (transform.Find("Board")) return;
            var board = Prim.Group("Board", transform, Vector3.zero);
            float half = Size * Spacing * 0.5f + 0.5f;
            Prim.Make(PrimitiveType.Cube, board, new(0f, 0.1f, 0f), new(half * 2f + 0.3f, 0.2f, half * 2f + 0.3f), Frame, 0.25f);
            Prim.Make(PrimitiveType.Cube, board, new(0f, 0.13f, 0f), new(half * 2f - 0.2f, 0.2f, half * 2f - 0.2f), Turf, 0f);
            for (int i = 0; i < Size * Size; i++)
            {
                var pos = HolePosition(i);
                var hole = Prim.Group($"Hole_{i}", transform, pos);
                Prim.Make(PrimitiveType.Cylinder, hole, new(0f, 0.25f, 0f), new(1.15f, 0.04f, 1.15f), Dirt, 0.15f);
                Prim.Make(PrimitiveType.Cylinder, hole, new(0f, 0.28f, 0f), new(0.85f, 0.02f, 0.85f), HoleColor, 0f);
            }
        }

        static Vector3 HolePosition(int i)
        {
            int x = i % Size - 1, z = i / Size - 1;
            return new Vector3(x * Spacing, 0f, z * Spacing);
        }

        void BuildCritters()
        {
            holes.Clear();
            byCollider.Clear();
            for (int i = 0; i < Size * Size; i++)
            {
                var hole = new Hole { root = transform.Find($"Hole_{i}") };
                foreach (WhackKind kind in Enum.GetValues(typeof(WhackKind)))
                {
                    var critter = BuildCritter(hole.root, kind);
                    critter.gameObject.SetActive(false);
                    hole.critters[kind] = critter;
                    byCollider[critter.GetComponent<Collider>()] = hole;
                }
                holes.Add(hole);
            }
        }

        static Transform BuildCritter(Transform parent, WhackKind kind)
        {
            var root = new GameObject(kind.ToString()).transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(0f, HiddenY, 0f);
            root.localRotation = Quaternion.Euler(0f, 180f, 0f);   // 面朝镜头
            var col = root.gameObject.AddComponent<SphereCollider>();
            col.center = new Vector3(0f, 0.35f, 0f);
            col.radius = 0.5f;

            if (kind == WhackKind.Baby)
            {
                var v = MoleBuilder.Build(root, new Color(0.62f, 0.46f, 0.36f), CosmeticDatabase.NoHat, "bowtie");
                v.localScale = Vector3.one * 0.7f;
                return root;
            }

            // 圆滚滚的瓢虫，金色的分数更高
            var shell = kind == WhackKind.GoldBug ? new Color(1f, 0.8f, 0.2f) : new Color(0.92f, 0.25f, 0.25f);
            var dark = new Color(0.15f, 0.12f, 0.14f);
            Prim.Make(PrimitiveType.Sphere, root, new(0f, 0.3f, -0.05f), new(0.62f, 0.5f, 0.66f), shell, 0.25f);
            Prim.Make(PrimitiveType.Sphere, root, new(0f, 0.38f, 0.3f), new(0.36f, 0.32f, 0.3f), dark, 0.2f);
            for (int s = -1; s <= 1; s += 2)
            {
                Prim.Make(PrimitiveType.Sphere, root, new(0.08f * s, 0.43f, 0.43f), Vector3.one * 0.09f, Color.white, 0f);
                Prim.Make(PrimitiveType.Sphere, root, new(0.08f * s, 0.43f, 0.47f), Vector3.one * 0.045f, dark, 0f);
                Prim.Make(PrimitiveType.Sphere, root, new(0.15f * s, 0.48f, -0.05f), Vector3.one * 0.12f, dark, 0f);
                Prim.Make(PrimitiveType.Sphere, root, new(0.18f * s, 0.38f, -0.25f), Vector3.one * 0.1f, dark, 0f);
                Prim.Make(PrimitiveType.Cylinder, root, new(0.1f * s, 0.58f, 0.36f), new(0.025f, 0.1f, 0.025f), dark, 0f, new Vector3(20f, 0f, -25f * s));
            }
            if (kind == WhackKind.GoldBug)
                Prim.Make(PrimitiveType.Sphere, root, new(0f, 0.62f, -0.05f), Vector3.one * 0.12f, Color.white, 0.1f);
            return root;
        }

        // ---------- 流程 ----------

        public void Begin()
        {
            if (Active) return;
            Score = 0;
            TimeLeft = duration;
            spawnTimer = 0.4f;
            Active = true;
            foreach (var h in holes) Hide(h);
            if (GameManager.Instance) GameManager.Instance.InputLocked = true;
            FindAnyObjectByType<FollowCamera>()?.SetFocus(transform, cameraDistance);
            GameUI.Current?.ShowMiniGameBar(this);
        }

        void End()
        {
            Active = false;
            foreach (var h in holes) Hide(h);
            var gm = GameManager.Instance;
            int reward = WhackRules.Reward(Score);
            bool record = false;
            if (gm)
            {
                gm.InputLocked = false;
                if (reward > 0) gm.Inventory.AddCoins(reward);
                record = gm.RecordWhackScore(Score);
            }
            FindAnyObjectByType<FollowCamera>()?.ClearFocus();
            GameUI.Current?.ShowMiniGameBar(null);
            GameUI.Current?.ShowMiniGameResult(Score, reward, gm ? gm.BestWhackScore : Score, record);
            Ended?.Invoke(this);
        }

        void OnDisable()
        {
            if (Active && GameManager.Instance) GameManager.Instance.InputLocked = false;
        }

        void Update()
        {
            if (!Active) return;
            float dt = Time.deltaTime;
            TimeLeft -= dt;
            float progress = 1f - Mathf.Clamp01(TimeLeft / duration);

            spawnTimer -= dt;
            if (spawnTimer <= 0f)
            {
                spawnTimer = Mathf.Lerp(0.75f, 0.4f, progress);
                SpawnRandom(Mathf.Lerp(1.3f, 0.75f, progress));
            }

            foreach (var h in holes) Animate(h, dt);
            HandleClick();

            if (TimeLeft <= 0f) End();
        }

        void SpawnRandom(float stay)
        {
            int upCount = 0;
            foreach (var h in holes) if (h.up) upCount++;
            if (upCount >= 3) return;

            var free = holes.FindAll(h => !h.up);
            if (free.Count == 0) return;
            float r = Random.value;
            var kind = r < 0.12f ? WhackKind.GoldBug : r < 0.3f ? WhackKind.Baby : WhackKind.Bug;
            Show(free[Random.Range(0, free.Count)], kind, stay);
        }

        // 测试用：指定洞口冒出指定的东西
        public void ForceSpawn(int index, WhackKind kind, float stay = 5f) => Show(holes[index], kind, stay);

        void Show(Hole h, WhackKind kind, float stay)
        {
            Hide(h);
            h.kind = kind;
            h.current = h.critters[kind];
            h.current.gameObject.SetActive(true);
            h.current.localScale = Vector3.one;
            h.up = true;
            h.hit = false;
            h.t = 0f;
            h.stay = stay;
        }

        static void Hide(Hole h)
        {
            if (h.current)
            {
                h.current.localPosition = new Vector3(0f, HiddenY, 0f);
                h.current.gameObject.SetActive(false);
            }
            h.current = null;
            h.up = false;
            h.hit = false;
        }

        static void Animate(Hole h, float dt)
        {
            if (!h.up) return;
            h.t += dt;
            // 冒出 → 停留 → 缩回
            float k = Mathf.Min(1f, h.t / PopTime, (h.stay - h.t) / PopTime);
            if (h.t >= h.stay)
            {
                Hide(h);
                return;
            }
            k = Mathf.SmoothStep(0f, 1f, Mathf.Max(0f, k));
            h.current.localPosition = new Vector3(0f, Mathf.Lerp(HiddenY, ShownY, k), 0f);
            if (!h.hit)
            {
                float wiggle = Mathf.Sin(h.t * 18f) * 4f;
                h.current.localRotation = Quaternion.Euler(0f, 180f, wiggle);
            }
        }

        void HandleClick()
        {
            if (!Input.GetMouseButtonDown(0)) return;
            if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;
            if (!cam) cam = Camera.main;
            if (!cam) return;
            if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out var hit, 200f) &&
                byCollider.TryGetValue(hit.collider, out var hole))
                Hit(holes.IndexOf(hole));
        }

        public bool Hit(int index)
        {
            var h = holes[index];
            if (!Active || !h.up || h.hit) return false;
            h.hit = true;
            Score = WhackRules.ApplyHit(Score, h.kind);
            // 被敲中：压扁一下然后缩回
            h.current.localScale = new Vector3(1.25f, 0.6f, 1.25f);
            h.t = Mathf.Max(h.t, h.stay - PopTime);

            int points = WhackRules.Points(h.kind);
            string text = h.kind == WhackKind.Baby ? $"哎呀！{points}" : $"+{points}";
            var color = h.kind switch
            {
                WhackKind.GoldBug => new Color(1f, 0.85f, 0.2f),
                WhackKind.Baby => new Color(1f, 0.45f, 0.45f),
                _ => Color.white,
            };
            GameUI.Current?.FloatText(h.root.position + Vector3.up * 1.2f, text, color);
            return true;
        }
    }
}
