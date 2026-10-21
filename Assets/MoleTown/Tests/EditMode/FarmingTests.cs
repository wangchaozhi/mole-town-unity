using MoleTown;
using NUnit.Framework;
using UnityEngine;

public class FarmingTests
{
    GameObject go;
    FarmPlot plot;
    Inventory inventory;
    CropDef strawberry;

    [SetUp]
    public void SetUp()
    {
        GameClock.Offset = 0;
        go = new GameObject("TestPlot");
        plot = go.AddComponent<FarmPlot>();
        plot.plotId = "test";
        inventory = new Inventory();
        strawberry = CropDatabase.Get("strawberry");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(go);
        GameClock.Offset = 0;
    }

    void AdvanceStage(CropDef crop)
    {
        GameClock.Offset += crop.stageSeconds + 0.1;
        plot.Tick();
    }

    [Test]
    public void Plant_NeedsSeed()
    {
        Assert.IsFalse(plot.Plant("strawberry", inventory));
        inventory.AddSeeds("strawberry", 1);
        Assert.IsTrue(plot.Plant("strawberry", inventory));
        Assert.AreEqual(0, inventory.SeedCount("strawberry"));
        Assert.IsTrue(plot.NeedsWater);
        Assert.IsFalse(plot.Plant("strawberry", inventory), "已经种了东西的田不能再种");
    }

    [Test]
    public void Crop_OnlyGrowsWhenWatered_AndCanBeHarvested()
    {
        inventory.AddSeeds("strawberry", 1);
        plot.Plant("strawberry", inventory);

        AdvanceStage(strawberry);
        Assert.AreEqual(0, plot.Stage, "没浇水不会长");

        for (int stage = 1; stage <= CropDatabase.RipeStage; stage++)
        {
            Assert.IsTrue(plot.Water());
            plot.Tick();
            Assert.AreEqual(stage - 1, plot.Stage, "时间没到不会长");
            AdvanceStage(strawberry);
            Assert.AreEqual(stage, plot.Stage);
        }

        Assert.IsTrue(plot.IsRipe);
        Assert.IsFalse(plot.Water(), "成熟后不需要浇水");
        Assert.AreEqual(strawberry.yield, plot.Harvest(inventory));
        Assert.AreEqual(strawberry.yield, inventory.CropCount("strawberry"));
        Assert.IsTrue(plot.IsEmpty);
    }

    [Test]
    public void Shop_BuyAndSell()
    {
        inventory.AddCoins(20);
        Assert.IsTrue(inventory.TryBuySeeds(strawberry, 3));
        Assert.AreEqual(5, inventory.Coins);
        Assert.AreEqual(3, inventory.SeedCount("strawberry"));
        Assert.IsFalse(inventory.TryBuySeeds(strawberry, 2), "钱不够时买不了");
        Assert.AreEqual(5, inventory.Coins);

        var pumpkin = CropDatabase.Get("pumpkin");
        inventory.AddCrops("pumpkin", 2);
        inventory.AddCrops("strawberry", 3);
        int expected = 2 * pumpkin.sellPrice + 3 * strawberry.sellPrice;
        Assert.AreEqual(expected, inventory.SellAll());
        Assert.AreEqual(5 + expected, inventory.Coins);
        Assert.AreEqual(0, inventory.SellAll());
    }

    [Test]
    public void Save_RoundTripsThroughJson()
    {
        inventory.AddCoins(42);
        inventory.AddSeeds("carrot", 2);
        inventory.AddCrops("pumpkin", 1);
        plot.Plant("carrot", inventory);
        plot.Water();

        var data = new SaveData();
        inventory.WriteTo(data);
        data.plots.Add(plot.ToSave());
        var loaded = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));

        var restored = new Inventory();
        restored.ReadFrom(loaded);
        Assert.AreEqual(42, restored.Coins);
        Assert.AreEqual(1, restored.SeedCount("carrot"));
        Assert.AreEqual(1, restored.CropCount("pumpkin"));

        var otherGo = new GameObject("RestoredPlot");
        try
        {
            var other = otherGo.AddComponent<FarmPlot>();
            other.LoadFrom(loaded.plots[0]);
            Assert.AreEqual("carrot", other.CropId);
            Assert.IsTrue(other.Watered);
            Assert.AreEqual(plot.StageStart, other.StageStart, 0.001);
        }
        finally
        {
            Object.DestroyImmediate(otherGo);
        }
    }

    [Test]
    public void Offline_GrowsAtMostOneStagePerWatering()
    {
        inventory.AddSeeds("strawberry", 1);
        plot.Plant("strawberry", inventory);
        plot.Water();
        var save = plot.ToSave();

        GameClock.Offset += 3600;   // 离线一小时
        plot.LoadFrom(save);
        plot.Tick();
        plot.Tick();
        Assert.AreEqual(1, plot.Stage);
        Assert.IsTrue(plot.NeedsWater);
    }
}
