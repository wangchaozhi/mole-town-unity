using System.Collections;
using System.Linq;
using MoleTown;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// 加载真实场景，验证寻路、种地、商店能在运行时串起来
public class TownPlayTests
{
    string originalKey;

    [SetUp]
    public void SetUp()
    {
        originalKey = SaveSystem.Key;
        SaveSystem.Key = "MoleTown.Save.playtest";
        SaveSystem.Clear();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        // 先销毁 GameManager，免得退出时的自动存档在 key 恢复后写进玩家的正式存档
        if (GameManager.Instance) Object.Destroy(GameManager.Instance.gameObject);
        yield return null;
        SaveSystem.Clear();
        SaveSystem.Key = originalKey;
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = b.y = 0f;
        return Vector3.Distance(a, b);
    }

    static IEnumerator WalkTo(NavMeshAgent agent, Vector3 target, float within)
    {
        Assert.IsTrue(NavMesh.SamplePosition(target, out var hit, 3f, NavMesh.AllAreas), "目标附近没有可行走区域");
        agent.SetDestination(hit.position);
        float elapsed = 0f;
        while (elapsed < 15f && FlatDistance(agent.transform.position, target) > within)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator LoadTown()
    {
        SceneManager.LoadScene("MoleTown");
        yield return null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator Wardrobe_BuyAndWearShowsOnPlayer()
    {
        yield return LoadTown();
        var gm = GameManager.Instance;
        var player = Object.FindAnyObjectByType<PlayerController>();
        var hatSlot = player.transform.Find("Visual/HatSlot");
        Assert.IsNotNull(hatSlot.Find("Hat_cap"), "默认应戴着红帽子");

        // 在裁缝铺打开衣柜，买草帽和背带裤并穿上
        Object.FindAnyObjectByType<TailorNPC>().Interact();
        Assert.IsTrue(HUD.Instance.IsWardrobeOpen);
        gm.Inventory.AddCoins(100);
        int coins = gm.Inventory.Coins;
        var straw = CosmeticDatabase.Get("straw");
        var overalls = CosmeticDatabase.Get("overalls");
        Assert.IsTrue(gm.Wardrobe.TryBuy(straw, gm.Inventory) && gm.Wardrobe.Equip("straw"));
        Assert.IsTrue(gm.Wardrobe.TryBuy(overalls, gm.Inventory) && gm.Wardrobe.Equip("overalls"));
        yield return null;

        Assert.AreEqual(coins - straw.price - overalls.price, gm.Inventory.Coins);
        Assert.IsNotNull(hatSlot.Find("Hat_straw"), "草帽没有出现在角色头上");
        Assert.IsNotNull(player.transform.Find("Visual/OutfitSlot/Outfit_overalls"), "背带裤没有穿上");
        Assert.IsFalse(player.transform.Find("Visual/Belly").gameObject.activeSelf, "穿背带裤时应隐藏肚皮");
        Assert.AreEqual(2, player.GetComponentsInChildren<Transform>().Count(t => t.name.StartsWith("Hat_") || t.name.StartsWith("Outfit_")),
            "旧的帽子没有被替换掉");

        HUD.Instance.CloseWardrobe();
        Assert.IsFalse(HUD.Instance.IsModalOpen);

        gm.Save();
        var saved = SaveSystem.Load();
        Assert.AreEqual("straw", saved.hat);
        Assert.AreEqual("overalls", saved.outfit);
    }

    [UnityTest]
    public IEnumerator WhackGame_ScoresRewardsAndUnlocks()
    {
        yield return LoadTown();
        var gm = GameManager.Instance;
        var game = Object.FindAnyObjectByType<WhackGame>();
        Object.FindAnyObjectByType<MiniGameNPC>().Interact();
        Assert.IsTrue(HUD.Instance.IsMiniGameDialogOpen, "应先弹出规则说明");

        int coins = gm.Inventory.Coins;
        game.duration = 1.5f;
        HUD.Instance.SendMessage("CloseMiniGameDialogs");
        game.Begin();
        Assert.IsTrue(game.Active);
        Assert.IsTrue(gm.InputLocked, "小游戏中应锁住角色移动");

        game.ForceSpawn(0, WhackKind.Bug);
        game.ForceSpawn(1, WhackKind.GoldBug);
        game.ForceSpawn(2, WhackKind.Baby);
        Assert.IsTrue(game.Hit(0));
        Assert.IsFalse(game.Hit(0), "同一只虫不能重复得分");
        Assert.IsTrue(game.Hit(1));
        Assert.AreEqual(4, game.Score);
        Assert.IsTrue(game.Hit(2));
        Assert.AreEqual(2, game.Score);

        float waited = 0f;
        while (game.Active && waited < 5f)
        {
            waited += Time.deltaTime;
            yield return null;
        }
        Assert.IsFalse(game.Active, "时间到了游戏没有结束");
        Assert.IsFalse(gm.InputLocked);
        Assert.GreaterOrEqual(gm.Inventory.Coins, coins + 2);
        Assert.AreEqual(game.Score, gm.Inventory.Coins - coins);
        Assert.AreEqual(game.Score, gm.BestWhackScore);
        Assert.IsTrue(HUD.Instance.IsMiniGameDialogOpen, "应显示结算面板");
    }

    [UnityTest]
    public IEnumerator Player_CanFarmAndShop()
    {
        yield return LoadTown();

        var gm = GameManager.Instance;
        Assert.IsNotNull(gm, "GameManager 没有初始化");
        Assert.IsNotNull(HUD.Instance, "HUD 没有初始化");
        Assert.AreEqual(gm.startingCoins, gm.Inventory.Coins);

        var player = Object.FindAnyObjectByType<PlayerController>();
        var agent = player.GetComponent<NavMeshAgent>();
        Assert.IsTrue(agent.isOnNavMesh, "玩家不在 NavMesh 上");
        Assert.AreEqual(2, Object.FindObjectsByType<TalkNPC>(FindObjectsSortMode.None).Length, "聊天 NPC 组件丢失");
        Assert.IsNotNull(Object.FindAnyObjectByType<ShopNPC>(), "杂货铺 NPC 组件丢失");
        Assert.IsNotNull(Object.FindAnyObjectByType<TailorNPC>(), "裁缝铺 NPC 组件丢失");
        Assert.IsNotNull(Object.FindAnyObjectByType<MiniGameNPC>(), "小游戏 NPC 组件丢失");

        // 走到田边种草莓
        var plot = Object.FindObjectsByType<FarmPlot>(FindObjectsSortMode.None).OrderBy(p => p.plotId).First();
        yield return WalkTo(agent, plot.InteractPosition, plot.InteractDistance);
        Assert.LessOrEqual(FlatDistance(player.transform.position, plot.InteractPosition), plot.InteractDistance + 0.5f, "走不到田边");
        plot.Interact();
        Assert.AreEqual("strawberry", plot.CropId);
        Assert.AreEqual(gm.startingStrawberrySeeds - 1, gm.Inventory.SeedCount("strawberry"));
        plot.Interact();
        Assert.IsTrue(plot.Watered);

        // 走到杂货铺买种子
        var shop = Object.FindAnyObjectByType<ShopNPC>();
        yield return WalkTo(agent, shop.InteractPosition, shop.InteractDistance);
        Assert.LessOrEqual(FlatDistance(player.transform.position, shop.InteractPosition), shop.InteractDistance + 0.5f, "走不到杂货铺");
        shop.Interact();
        Assert.IsTrue(HUD.Instance.IsShopOpen);
        Assert.IsTrue(gm.Inventory.TryBuySeeds(CropDatabase.Get("carrot"), 1));

        // 存档写入
        gm.Save();
        var saved = SaveSystem.Load();
        Assert.IsNotNull(saved);
        Assert.AreEqual(gm.Inventory.Coins, saved.coins);
        Assert.IsTrue(saved.plots.Any(p => p.cropId == "strawberry" && p.watered));
    }
}
