using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoleTown
{
    [Serializable]
    public class CountEntry
    {
        public string id;
        public int count;
    }

    [Serializable]
    public class PlotSave
    {
        public string plotId;
        public string cropId;
        public int stage;
        public bool watered;
        public double stageStart;
    }

    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int coins;
        public List<CountEntry> seeds = new();
        public List<CountEntry> crops = new();
        public List<PlotSave> plots = new();
        public List<string> ownedCosmetics = new();
        public string hat;
        public string outfit;
        public int bestWhack;
    }

    public static class SaveSystem
    {
        public static string Key = "MoleTown.Save.v1";   // 测试时会换成独立的 key，避免覆盖玩家存档

        public static void Save(SaveData data)
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public static SaveData Load()
        {
            if (!PlayerPrefs.HasKey(Key)) return null;
            try { return JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key)); }
            catch (ArgumentException) { return null; }
        }

        public static void Clear() => PlayerPrefs.DeleteKey(Key);
    }
}
