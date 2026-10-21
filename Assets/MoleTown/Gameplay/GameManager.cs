using System;
using UnityEngine;

namespace MoleTown
{
    // 持有背包和当前选中的种子，负责读写存档
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public int startingCoins = 30;
        public int startingStrawberrySeeds = 4;

        public Inventory Inventory { get; private set; }
        public Wardrobe Wardrobe { get; private set; }
        public int BestWhackScore { get; private set; }
        // 小游戏进行中时锁住角色移动，点击交给小游戏处理
        public bool InputLocked { get; set; }
        public string SelectedSeed { get; private set; } = CropDatabase.All[0].id;
        public event Action SelectionChanged;

        FarmPlot[] plots;
        bool dirty;
        float lastSave;

        void Awake()
        {
            Instance = this;
            Inventory = new Inventory();
            Wardrobe = new Wardrobe();
            plots = FindObjectsByType<FarmPlot>(FindObjectsSortMode.None);
            Load();
            Inventory.Changed += MarkDirty;
            Wardrobe.Changed += MarkDirty;
            foreach (var plot in plots) plot.StateChanged += _ => MarkDirty();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            for (int i = 0; i < CropDatabase.All.Length && i < 9; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) SelectSeed(CropDatabase.All[i].id);

            if (dirty && Time.unscaledTime - lastSave > 1f) Save();
        }

        public void SelectSeed(string id)
        {
            if (SelectedSeed == id || CropDatabase.Get(id) == null) return;
            SelectedSeed = id;
            SelectionChanged?.Invoke();
        }

        void MarkDirty() => dirty = true;

        // 返回是否刷新了最高分
        public bool RecordWhackScore(int score)
        {
            if (score <= BestWhackScore) return false;
            BestWhackScore = score;
            MarkDirty();
            return true;
        }

        public void Save()
        {
            var data = new SaveData();
            Inventory.WriteTo(data);
            Wardrobe.WriteTo(data);
            data.bestWhack = BestWhackScore;
            foreach (var plot in plots) data.plots.Add(plot.ToSave());
            SaveSystem.Save(data);
            dirty = false;
            lastSave = Time.unscaledTime;
        }

        void Load()
        {
            var data = SaveSystem.Load();
            if (data == null)
            {
                Inventory.AddCoins(startingCoins);
                Inventory.AddSeeds(CropDatabase.All[0].id, startingStrawberrySeeds);
                return;
            }
            Inventory.ReadFrom(data);
            Wardrobe.ReadFrom(data);
            BestWhackScore = data.bestWhack;
            foreach (var save in data.plots)
            {
                var plot = Array.Find(plots, p => p.plotId == save.plotId);
                if (plot) plot.LoadFrom(save);
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        void OnApplicationQuit() => Save();
    }
}
