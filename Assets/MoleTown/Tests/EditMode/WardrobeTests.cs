using MoleTown;
using NUnit.Framework;
using UnityEngine;

public class WardrobeTests
{
    [Test]
    public void NewWardrobe_OwnsFreeItems_AndWearsDefaultCap()
    {
        var wardrobe = new Wardrobe();
        Assert.AreEqual(CosmeticDatabase.DefaultHat, wardrobe.Hat);
        Assert.AreEqual(CosmeticDatabase.NoOutfit, wardrobe.Outfit);
        Assert.IsTrue(wardrobe.Owns(CosmeticDatabase.NoHat));
        Assert.IsFalse(wardrobe.Owns("crown"));
    }

    [Test]
    public void Buy_NeedsEnoughCoins_AndEquipNeedsOwnership()
    {
        var wardrobe = new Wardrobe();
        var inventory = new Inventory(50);
        var crown = CosmeticDatabase.Get("crown");

        Assert.IsFalse(wardrobe.Equip("crown"), "没买的不能穿");
        Assert.IsFalse(wardrobe.TryBuy(crown, inventory), "钱不够");
        Assert.AreEqual(50, inventory.Coins);

        var bunny = CosmeticDatabase.Get("bunny");
        Assert.IsTrue(wardrobe.TryBuy(bunny, inventory));
        Assert.AreEqual(50 - bunny.price, inventory.Coins);
        Assert.IsTrue(wardrobe.TryBuy(bunny, inventory), "已拥有再买不扣钱");
        Assert.AreEqual(50 - bunny.price, inventory.Coins);

        Assert.IsTrue(wardrobe.Equip("bunny"));
        Assert.AreEqual("bunny", wardrobe.Hat);
        Assert.AreEqual(CosmeticDatabase.NoOutfit, wardrobe.Outfit, "戴帽子不影响衣服");
    }

    [Test]
    public void Save_RoundTrip_AndRejectsInvalidData()
    {
        var wardrobe = new Wardrobe();
        var inventory = new Inventory(100);
        wardrobe.TryBuy(CosmeticDatabase.Get("cape"), inventory);
        wardrobe.Equip("cape");

        var data = new SaveData();
        wardrobe.WriteTo(data);
        var loaded = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));
        var restored = new Wardrobe();
        restored.ReadFrom(loaded);
        Assert.AreEqual("cape", restored.Outfit);
        Assert.IsTrue(restored.Owns("cape"));

        // 旧存档没有这些字段；或存档被改成穿着没买的东西
        var bad = new Wardrobe();
        bad.ReadFrom(new SaveData { hat = "crown", outfit = "cap" });
        Assert.AreEqual(CosmeticDatabase.DefaultHat, bad.Hat);
        Assert.AreEqual(CosmeticDatabase.NoOutfit, bad.Outfit);
    }

    [Test]
    public void WhackRules_ScoreNeverNegative()
    {
        Assert.AreEqual(1, WhackRules.ApplyHit(0, WhackKind.Bug));
        Assert.AreEqual(4, WhackRules.ApplyHit(1, WhackKind.GoldBug));
        Assert.AreEqual(0, WhackRules.ApplyHit(1, WhackKind.Baby));
        Assert.AreEqual(3, WhackRules.ApplyHit(5, WhackKind.Baby));
    }
}
