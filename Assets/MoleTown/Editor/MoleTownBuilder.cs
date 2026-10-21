// 鼹鼠小镇场景生成器：菜单 Tools/MoleTown/Build Scene，或命令行 -executeMethod MoleTownBuilder.BuildAndCapture
using System.IO;
using System.Linq;
using MoleTown;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class MoleTownBuilder
{
    const string ScenePath = "Assets/MoleTown/Scenes/MoleTown.unity";
    const string NavMeshPath = "Assets/MoleTown/Generated/MoleTownNavMesh.asset";
    const string PostProcessPath = "Assets/MoleTown/Generated/ToonPostProcess.asset";
    const string RendererPath = "Assets/Settings/ToonRenderer.asset";

    static readonly Color Grass = new(0.56f, 0.82f, 0.42f);
    static readonly Color Sand = new(0.95f, 0.86f, 0.64f);
    static readonly Color Stone = new(0.82f, 0.82f, 0.88f);
    static readonly Color Water = new(0.45f, 0.78f, 1f);
    static readonly Color Wood = new(0.82f, 0.58f, 0.38f);
    static readonly Color DarkWood = new(0.58f, 0.38f, 0.26f);
    static readonly Color Trunk = new(0.55f, 0.38f, 0.25f);
    static readonly Color Cream = new(1f, 0.94f, 0.82f);
    static readonly Color[] Leaves = { new(0.38f, 0.72f, 0.35f), new(0.45f, 0.78f, 0.38f), new(0.32f, 0.65f, 0.4f) };
    static readonly Color[] FlowerColors = { new(1f, 0.6f, 0.75f), new(1f, 0.88f, 0.35f), Color.white, new(0.75f, 0.6f, 1f), new(1f, 0.5f, 0.45f) };

    // 农田区域，花草避开这里
    static readonly Rect FarmArea = new(4.2f, -2f, 9.6f, 8f);
    static readonly Vector3 ShopPos = new(-9f, 0f, 4f);
    static readonly Vector3 TailorPos = new(9f, 0f, -7.5f);
    static readonly Vector3 GamePos = new(-7.5f, 0f, -6.5f);
    static readonly Rect TailorArea = new(5.8f, -10.5f, 7f, 5f);
    static readonly Rect GameArea = new(-11.5f, -11.2f, 9f, 8.5f);
    static readonly Vector3[] HousePositions = { new(-13f, 0f, 12f), new(-5f, 0f, 15f), new(6f, 0f, 15f), new(14f, 0f, 12f) };

    [MenuItem("Tools/MoleTown/Build Scene")]
    public static void BuildMenu()
    {
        Build();
        EditorSceneManager.OpenScene(ScenePath);
    }

    [MenuItem("Tools/MoleTown/Clear Save")]
    public static void ClearSave()
    {
        SaveSystem.Clear();
        Debug.Log("[MoleTown] 存档已清除");
    }

    public static void BuildAndCapture()
    {
        Build();
        CapturePreview();
    }

    public static void Build()
    {
        ConfigureUrp();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SetupLighting();
        SetupPostProcessing();

        var town = new GameObject("Town").transform;
        BuildGround(town);
        BuildPlaza(town);
        foreach (var pos in HousePositions) BuildMushroomHouse(town, pos);
        BuildFarm(town);
        BuildShop(town);
        BuildTailor(town);
        BuildMiniGame(town);
        BuildVillagers(town);
        BuildTrees(town);
        BuildFlowers(town);
        BakeNavMesh(town);

        var player = BuildPlayer();
        BuildCamera(player);
        new GameObject("GameManager").AddComponent<GameManager>();
        new GameObject("HUD").AddComponent<HUD>();

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("[MoleTown] 场景已生成：" + ScenePath);
    }

    // 深度边缘光要在不透明物体绘制时读到深度图，所以要求深度预渲染（Force Prepass）
    static void ConfigureUrp()
    {
        if (GraphicsSettings.defaultRenderPipeline is not UniversalRenderPipelineAsset urp)
        {
            Debug.LogWarning("[MoleTown] 项目没有使用 URP，MoleTown/Toon 着色器将无法显示");
            return;
        }
        urp.shadowDistance = 45f;
        urp.supportsCameraDepthTexture = true;
        urp.msaaSampleCount = 4;
        EditorUtility.SetDirty(urp);

        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (renderer)
        {
            renderer.copyDepthMode = CopyDepthMode.ForcePrepass;
            EditorUtility.SetDirty(renderer);
        }
        else Debug.LogWarning("[MoleTown] 找不到 " + RendererPath + "，深度边缘光可能读不到深度图");
    }

    // 轻度后期：高光泛光、略提饱和度、淡淡的暗角，保持色块干净
    static void SetupPostProcessing()
    {
        AssetDatabase.DeleteAsset(PostProcessPath);
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, PostProcessPath);

        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(1f);
        bloom.intensity.Override(0.6f);
        bloom.scatter.Override(0.6f);
        var color = profile.Add<ColorAdjustments>(true);
        color.contrast.Override(6f);
        color.saturation.Override(12f);
        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.18f);
        vignette.smoothness.Override(0.5f);
        foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
        EditorUtility.SetDirty(profile);

        var volume = new GameObject("Global Volume").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = profile;
    }

    static void SetupLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.82f, 0.9f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.72f, 0.78f, 0.8f);
        RenderSettings.ambientGroundColor = new Color(0.52f, 0.52f, 0.46f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 40f;
        RenderSettings.fogEndDistance = 90f;
        RenderSettings.fogColor = new Color(0.7f, 0.86f, 1f);

        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.95f, 0.86f);
        sun.intensity = 1f;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(48f, -38f, 0f);
    }

    static void BuildGround(Transform town)
    {
        var ground = Prim.Make(PrimitiveType.Plane, town, Vector3.zero, new(8f, 1f, 8f), Grass, 0f, keepCollider: true, name: "Ground");
        ground.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

        var paths = Prim.Group("Paths", town, Vector3.zero);
        PathStrip(paths, new(0f, 0f, -14f), new(3f, 30f));     // 南北主路
        PathStrip(paths, new(0f, 0f, 1f), new(36f, 3f));       // 东西主路
        PathStrip(paths, new(-9f, 0f, 1.5f), new(2.4f, 3f));   // 去杂货铺
        foreach (var h in HousePositions) PathStrip(paths, new(h.x, 0f, (h.z - 0.5f) * 0.5f), new(2f, h.z - 2.5f));   // 主路通到门口

        // 远处的小山丘增加层次
        var hills = Prim.Group("Hills", town, Vector3.zero);
        Prim.Make(PrimitiveType.Sphere, hills, new(-26f, -4f, 36f), new(34f, 16f, 18f), Leaves[2], 0f);
        Prim.Make(PrimitiveType.Sphere, hills, new(8f, -5f, 40f), new(40f, 18f, 18f), Leaves[0], 0f);
        Prim.Make(PrimitiveType.Sphere, hills, new(34f, -4f, 30f), new(26f, 14f, 22f), Leaves[1], 0f);
    }

    static void PathStrip(Transform parent, Vector3 center, Vector2 size)
    {
        var p = Prim.Make(PrimitiveType.Cube, parent, center + new Vector3(0f, 0.02f, 0f), new(size.x, 0.04f, size.y), Sand, 0f, name: "Path");
        p.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
    }

    static void BuildPlaza(Transform town)
    {
        var plaza = Prim.Group("Plaza", town, new(0f, 0f, 1f));
        Prim.Make(PrimitiveType.Cylinder, plaza, new(0f, 0.03f, 0f), new(9f, 0.03f, 9f), Sand, 0f, name: "Square");
        Prim.Make(PrimitiveType.Cylinder, plaza, new(0f, 0.35f, 0f), new(3.4f, 0.35f, 3.4f), Stone, 0.25f, name: "Basin");
        Prim.Make(PrimitiveType.Cylinder, plaza, new(0f, 0.68f, 0f), new(2.9f, 0.04f, 2.9f), Water, 0f, name: "Water");
        Prim.Make(PrimitiveType.Cylinder, plaza, new(0f, 1.1f, 0f), new(0.5f, 0.6f, 0.5f), Stone, 0.25f, name: "Pillar");
        Prim.Make(PrimitiveType.Sphere, plaza, new(0f, 1.75f, 0f), new(1.4f, 0.35f, 1.4f), Stone, 0.25f, name: "Bowl");
        Prim.Make(PrimitiveType.Sphere, plaza, new(0f, 2.05f, 0f), new(0.4f, 0.5f, 0.4f), Water, 0.15f, name: "Spout");
        // 广场边的长椅
        for (int s = -1; s <= 1; s += 2)
        {
            var bench = Prim.Group("Bench", plaza, new(3.6f * s, 0f, 2.6f));
            bench.localRotation = Quaternion.Euler(0f, 40f * s, 0f);
            Prim.Make(PrimitiveType.Cube, bench, new(0f, 0.45f, 0f), new(1.8f, 0.12f, 0.6f), Wood, 0.2f);
            Prim.Make(PrimitiveType.Cube, bench, new(0f, 0.8f, 0.26f), new(1.8f, 0.4f, 0.1f), Wood, 0.2f);
            Prim.Make(PrimitiveType.Cube, bench, new(-0.7f, 0.2f, 0f), new(0.12f, 0.4f, 0.5f), DarkWood, 0.2f);
            Prim.Make(PrimitiveType.Cube, bench, new(0.7f, 0.2f, 0f), new(0.12f, 0.4f, 0.5f), DarkWood, 0.2f);
        }
    }

    static void BuildMushroomHouse(Transform town, Vector3 pos)
    {
        var caps = new[] { new Color(0.95f, 0.35f, 0.35f), new Color(1f, 0.6f, 0.3f), new Color(0.7f, 0.5f, 0.9f), new Color(0.4f, 0.65f, 0.95f) };
        int index = System.Array.IndexOf(HousePositions, pos);
        var house = Prim.Group("MushroomHouse", town, pos);
        Prim.Make(PrimitiveType.Cylinder, house, new(0f, 1.4f, 0f), new(3f, 1.4f, 3f), Cream, 0.3f, name: "Stem");
        var cap = caps[index % caps.Length];
        Prim.Make(PrimitiveType.Sphere, house, new(0f, 3.2f, 0f), new(5.2f, 3f, 5.2f), cap, 0.35f, name: "Cap");
        // 帽子上的白点：在椭球表面取点
        for (int i = 0; i < 7; i++)
        {
            float az = i * 51f + index * 20f;
            float el = i == 6 ? 90f : 30f + (i % 2) * 18f;
            var dir = Quaternion.Euler(-el, az, 0f) * Vector3.forward;
            var p = new Vector3(dir.x * 2.6f, dir.y * 1.5f, dir.z * 2.6f) * 1.01f;
            Prim.Make(PrimitiveType.Sphere, house, new Vector3(0f, 3.2f, 0f) + p, new(0.8f, 0.35f, 0.8f), Color.white, 0f,
                Quaternion.FromToRotation(Vector3.up, dir).eulerAngles, name: "Dot");
        }
        Prim.Make(PrimitiveType.Cube, house, new(0f, 0.8f, -1.45f), new(1f, 1.6f, 0.2f), DarkWood, 0.2f, name: "Door");
        Prim.Make(PrimitiveType.Sphere, house, new(0.3f, 0.8f, -1.56f), Vector3.one * 0.1f, new Color(1f, 0.85f, 0.3f), 0f, name: "Knob");
        Prim.Make(PrimitiveType.Cylinder, house, new(-0.95f, 1.75f, -1.3f), new(0.6f, 0.05f, 0.6f), new Color(1f, 0.95f, 0.6f), 0.15f,
            new Vector3(90f, 0f, -20f), name: "Window");
    }

    static void BuildFarm(Transform town)
    {
        var farm = Prim.Group("Farm", town, Vector3.zero);
        int id = 0;
        for (int row = 0; row < 2; row++)
        for (int col = 0; col < 3; col++)
        {
            var plotGo = new GameObject($"FarmPlot_{id}");
            plotGo.transform.SetParent(farm, false);
            plotGo.transform.position = new Vector3(6.4f + col * 2.6f, 0f, 0.7f + row * 2.6f);
            var plot = plotGo.AddComponent<FarmPlot>();
            plot.plotId = $"plot_{id++}";
            plot.LoadFrom(new PlotSave());   // 生成土壤和图标
        }

        // 篱笆，西侧留出入口
        var fence = Prim.Group("Fence", farm, Vector3.zero);
        float x0 = FarmArea.xMin, x1 = FarmArea.xMax, z0 = FarmArea.yMin, z1 = FarmArea.yMax;
        FenceLine(fence, new(x0, 0f, z0), new(x1, 0f, z0));
        FenceLine(fence, new(x1, 0f, z0), new(x1, 0f, z1));
        FenceLine(fence, new(x1, 0f, z1), new(x0, 0f, z1));
        FenceLine(fence, new(x0, 0f, z1), new(x0, 0f, 2.8f));
        FenceLine(fence, new(x0, 0f, -0.6f), new(x0, 0f, z0));

        // 田边的水桶和稻草人
        var bucket = Prim.Group("Bucket", farm, new(x0 + 0.6f, 0f, z1 - 0.7f));
        Prim.Make(PrimitiveType.Cylinder, bucket, new(0f, 0.3f, 0f), new(0.6f, 0.3f, 0.6f), new Color(0.6f, 0.75f, 0.85f), 0.2f);
        Prim.Make(PrimitiveType.Cylinder, bucket, new(0f, 0.58f, 0f), new(0.5f, 0.02f, 0.5f), Water, 0f);
        var crow = Prim.Group("Scarecrow", farm, new(x1 - 0.8f, 0f, z1 - 0.8f));
        Prim.Make(PrimitiveType.Cylinder, crow, new(0f, 0.9f, 0f), new(0.1f, 0.9f, 0.1f), DarkWood, 0.15f);
        Prim.Make(PrimitiveType.Cylinder, crow, new(0f, 1.3f, 0f), new(0.08f, 0.6f, 0.08f), DarkWood, 0.15f, new Vector3(0f, 0f, 90f));
        Prim.Make(PrimitiveType.Sphere, crow, new(0f, 1.25f, 0f), new(0.5f, 0.6f, 0.35f), new Color(0.55f, 0.7f, 0.95f), 0.2f);
        Prim.Make(PrimitiveType.Sphere, crow, new(0f, 1.8f, 0f), Vector3.one * 0.45f, new Color(1f, 0.9f, 0.6f), 0.2f);
        Prim.Make(PrimitiveType.Cylinder, crow, new(0f, 2.02f, 0f), new(0.8f, 0.02f, 0.8f), new Color(0.95f, 0.8f, 0.4f), 0.15f);
    }

    static void FenceLine(Transform parent, Vector3 a, Vector3 b)
    {
        float length = Vector3.Distance(a, b);
        int posts = Mathf.Max(1, Mathf.RoundToInt(length / 1.2f));
        for (int i = 0; i <= posts; i++)
            Prim.Make(PrimitiveType.Cylinder, parent, Vector3.Lerp(a, b, i / (float)posts) + new Vector3(0f, 0.4f, 0f), new(0.16f, 0.4f, 0.16f), Wood, 0.15f, name: "Post");
        var mid = (a + b) * 0.5f;
        var rot = Quaternion.LookRotation(b - a).eulerAngles;
        foreach (float h in new[] { 0.35f, 0.65f })
            Prim.Make(PrimitiveType.Cube, parent, mid + new Vector3(0f, h, 0f), new(0.08f, 0.1f, length), Cream, 0.12f, rot, name: "Rail");
    }

    static void BuildShop(Transform town)
    {
        var shop = Prim.Group("Shop", town, ShopPos);
        Prim.Make(PrimitiveType.Cube, shop, new(0f, 0.5f, -0.6f), new(3.2f, 1f, 1f), Wood, 0.25f, name: "Counter");
        Prim.Make(PrimitiveType.Cube, shop, new(0f, 1.06f, -0.6f), new(3.4f, 0.12f, 1.2f), Cream, 0.2f, name: "CounterTop");
        Prim.Make(PrimitiveType.Cube, shop, new(0f, 1.4f, 1.4f), new(3.6f, 2.8f, 0.25f), DarkWood, 0.25f, name: "BackWall");
        Prim.Make(PrimitiveType.Cube, shop, new(0f, 0.25f, 0.4f), new(3f, 0.5f, 1.6f), DarkWood, 0f, name: "Floor");
        for (int s = -1; s <= 1; s += 2)
            Prim.Make(PrimitiveType.Cylinder, shop, new(1.7f * s, 1.45f, -1f), new(0.16f, 1.45f, 0.16f), Cream, 0.15f, name: "Post");
        // 红白条纹遮阳棚
        for (int i = 0; i < 6; i++)
        {
            var color = i % 2 == 0 ? new Color(0.95f, 0.4f, 0.4f) : Color.white;
            Prim.Make(PrimitiveType.Cube, shop, new(-1.6f + i * 0.64f, 2.95f, 0.15f), new(0.64f, 0.08f, 2.7f), color, 0.15f, new Vector3(-14f, 0f, 0f), name: "Awning");
        }
        // 招牌
        Prim.Make(PrimitiveType.Cube, shop, new(0f, 3.45f, 1.35f), new(2f, 0.6f, 0.12f), new Color(1f, 0.85f, 0.4f), 0.2f, name: "Sign");
        // 摆在旁边的货箱
        for (int i = 0; i < 2; i++)
        {
            var crate = Prim.Group("Crate", shop, new(2.5f, 0f, -0.4f + i * 1f));
            Prim.Make(PrimitiveType.Cube, crate, new(0f, 0.3f, 0f), new(0.8f, 0.6f, 0.8f), Wood, 0.2f);
            var fruit = CropDatabase.All[i].fruitColor;
            for (int k = 0; k < 3; k++)
                Prim.Make(PrimitiveType.Sphere, crate, new(-0.2f + k * 0.2f, 0.66f, (k % 2) * 0.12f - 0.06f), Vector3.one * 0.24f, fruit, 0.12f);
        }

        var keeper = new GameObject("Shopkeeper");
        keeper.transform.SetParent(shop, false);
        keeper.transform.localPosition = new Vector3(0f, 0.5f, 0.3f);
        keeper.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        MoleBuilder.Build(keeper.transform, new Color(0.62f, 0.5f, 0.4f), "straw", "overalls", outfitTint: new Color(0.45f, 0.65f, 0.42f));
        AddClickCollider(keeper, 0.75f, 1.6f);
        keeper.AddComponent<IdleBob>();
        var npc = keeper.AddComponent<ShopNPC>();
        npc.displayName = "阿土";
        npc.standPoint = Prim.Group("StandPoint", shop, new(0f, 0f, -2.1f));
    }

    static void BuildTailor(Transform town)
    {
        var tailor = Prim.Group("Tailor", town, TailorPos);
        Prim.Make(PrimitiveType.Cylinder, tailor, new(0f, 0.03f, 0.2f), new(4.6f, 0.03f, 3.6f), new Color(0.82f, 0.74f, 0.95f), 0f, name: "Rug");

        // 挂满衣服的衣架
        var rack = Prim.Group("Rack", tailor, new(0f, 0f, 1.3f));
        for (int s = -1; s <= 1; s += 2)
            Prim.Make(PrimitiveType.Cylinder, rack, new(1.4f * s, 0.9f, 0f), new(0.1f, 0.9f, 0.1f), DarkWood, 0.15f);
        Prim.Make(PrimitiveType.Cylinder, rack, new(0f, 1.78f, 0f), new(0.07f, 1.45f, 0.07f), DarkWood, 0.15f, new Vector3(0f, 0f, 90f));
        var shirts = CosmeticDatabase.InSlot(CosmeticSlot.Outfit).Where(c => c.price > 0).ToArray();
        for (int i = 0; i < shirts.Length; i++)
        {
            float x = -1.05f + i * 0.7f;
            Prim.Make(PrimitiveType.Cylinder, rack, new(x, 1.68f, 0f), new(0.03f, 0.08f, 0.03f), Stone, 0f);
            Prim.Make(PrimitiveType.Sphere, rack, new(x, 1.3f, 0f), new(0.55f, 0.65f, 0.2f), shirts[i].color, 0.2f, name: "Shirt");
        }

        // 落地镜
        var mirror = Prim.Group("Mirror", tailor, new(2.2f, 0f, 0.4f));
        mirror.localRotation = Quaternion.Euler(0f, -30f, 0f);
        Prim.Make(PrimitiveType.Cylinder, mirror, new(0f, 0.3f, 0f), new(0.12f, 0.3f, 0.12f), Wood, 0.15f);
        Prim.Make(PrimitiveType.Cylinder, mirror, new(0f, 1.25f, 0f), new(0.95f, 0.04f, 1.4f), Wood, 0.2f, new Vector3(-90f, 0f, 0f));
        Prim.Make(PrimitiveType.Cylinder, mirror, new(0f, 1.25f, -0.03f), new(0.8f, 0.03f, 1.25f), new Color(0.75f, 0.9f, 1f), 0f, new Vector3(-90f, 0f, 0f));

        // 帽架上放一顶魔法帽做招牌
        var stand = Prim.Group("HatStand", tailor, new(-2.2f, 0f, 0.4f));
        Prim.Make(PrimitiveType.Cylinder, stand, new(0f, 0.55f, 0f), new(0.1f, 0.55f, 0.1f), DarkWood, 0.15f);
        Prim.Make(PrimitiveType.Cylinder, stand, new(0f, 0.04f, 0f), new(0.6f, 0.04f, 0.6f), DarkWood, 0.15f);
        var dummy = Prim.Group("Visual", stand, new(0f, 0.1f, 0f));
        Prim.Group("HatSlot", dummy, Vector3.zero);
        MoleBuilder.ApplyHat(dummy, "wizard");

        var keeper = new GameObject("Tailor_NPC");
        keeper.transform.SetParent(tailor, false);
        keeper.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        MoleBuilder.Build(keeper.transform, new Color(0.72f, 0.58f, 0.52f), "bunny", "sweater", outfitTint: new Color(0.75f, 0.62f, 0.95f));
        AddClickCollider(keeper, 0.6f, 1.5f);
        keeper.AddComponent<IdleBob>();
        var npc = keeper.AddComponent<TailorNPC>();
        npc.displayName = "小棉";
        npc.standPoint = Prim.Group("StandPoint", tailor, new(0f, 0f, -1.8f));
    }

    static void BuildMiniGame(Transform town)
    {
        var gameGo = new GameObject("WhackGame");
        gameGo.transform.SetParent(town, false);
        gameGo.transform.localPosition = GamePos;
        var game = gameGo.AddComponent<WhackGame>();
        game.EnsureVisuals();

        // 告示牌：画一只瓢虫
        var sign = Prim.Group("Sign", town, GamePos + new Vector3(-2.2f, 0f, 3.7f));
        Prim.Make(PrimitiveType.Cylinder, sign, new(0f, 0.7f, 0f), new(0.12f, 0.7f, 0.12f), DarkWood, 0.15f);
        Prim.Make(PrimitiveType.Cube, sign, new(0f, 1.55f, 0f), new(1.3f, 0.8f, 0.1f), new Color(1f, 0.88f, 0.5f), 0.2f);
        Prim.Make(PrimitiveType.Sphere, sign, new(0f, 1.52f, -0.07f), new(0.45f, 0.4f, 0.1f), new Color(0.92f, 0.25f, 0.25f), 0.15f);
        Prim.Make(PrimitiveType.Sphere, sign, new(0f, 1.78f, -0.07f), new(0.22f, 0.18f, 0.08f), new Color(0.15f, 0.12f, 0.14f), 0f);

        var host = new GameObject("MiniGame_NPC");
        host.transform.SetParent(town, false);
        host.transform.localPosition = new Vector3(-3.4f, 0f, -8f);
        host.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        MoleBuilder.Build(host.transform, new Color(0.5f, 0.42f, 0.5f), "wizard", "cape");
        AddClickCollider(host, 0.6f, 1.8f);
        host.AddComponent<IdleBob>();
        var npc = host.AddComponent<MiniGameNPC>();
        npc.displayName = "跳跳";
        npc.game = game;
        npc.standPoint = Prim.Group("MiniGameStandPoint", town, new(-3.4f, 0f, -9.8f));
    }

    static void BuildVillagers(Transform town)
    {
        Villager(town, "豆豆", new(3.2f, 0f, -2f), new Color(0.6f, 0.44f, 0.34f), new Color(1f, 0.55f, 0.7f), new[]
        {
            "今天天气真好，最适合种草莓了！",
            "听说南瓜卖得最贵，就是长得慢。",
            "作物每长大一点都要重新浇水哦~",
            "阿土的杂货铺什么种子都有。",
        });
        Villager(town, "可可", new(-6.5f, 0f, 10.5f), new Color(0.5f, 0.42f, 0.46f), new Color(0.55f, 0.75f, 1f), new[]
        {
            "我家的蘑菇屋是红色的，好看吧？",
            "按 1、2、3 可以切换要种的种子。",
            "关掉游戏后作物也会继续长，不过还是要记得浇水。",
        });
    }

    static void Villager(Transform town, string displayName, Vector3 pos, Color fur, Color accent, string[] lines)
    {
        var go = new GameObject(displayName);
        go.transform.SetParent(town, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        MoleBuilder.Build(go.transform, fur, "flower", CosmeticDatabase.NoOutfit, hatTint: accent);
        AddClickCollider(go, 0.6f, 1.3f);
        go.AddComponent<IdleBob>();
        var talk = go.AddComponent<TalkNPC>();
        talk.displayName = displayName;
        talk.lines = lines;
    }

    static void AddClickCollider(GameObject go, float radius, float height)
    {
        var col = go.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, height * 0.5f, 0f);
        col.radius = radius;
        col.height = height;
    }

    static void BuildTrees(Transform town)
    {
        var trees = Prim.Group("Trees", town, Vector3.zero);
        var rng = new System.Random(7);
        int placed = 0;
        for (int attempt = 0; attempt < 400 && placed < 34; attempt++)
        {
            var p = new Vector3((float)rng.NextDouble() * 70f - 35f, 0f, (float)rng.NextDouble() * 50f - 22f);
            if (!IsFree(p, 4f) || Mathf.Abs(p.x) < 18f && p.z < 16f && p.z > -16f) continue;
            Tree(trees, p, 0.8f + (float)rng.NextDouble() * 0.5f, rng);
            placed++;
        }
        // 镇子里点缀几棵
        foreach (var p in new Vector3[] { new(-15f, 0f, -4f), new(15.5f, 0f, -4.5f), new(-10f, 0f, 9f), new(10f, 0f, 9.5f), new(-16f, 0f, 6f) })
            Tree(trees, p, 0.9f, rng);
    }

    static void Tree(Transform parent, Vector3 pos, float size, System.Random rng)
    {
        var tree = Prim.Group("Tree", parent, pos);
        tree.localScale = Vector3.one * size;
        Prim.Make(PrimitiveType.Cylinder, tree, new(0f, 1f, 0f), new(0.5f, 1f, 0.5f), Trunk, 0.2f);
        var leaf = Leaves[rng.Next(Leaves.Length)];
        Prim.Make(PrimitiveType.Sphere, tree, new(0f, 2.7f, 0f), Vector3.one * 2.6f, leaf, 0.3f);
        Prim.Make(PrimitiveType.Sphere, tree, new(0.6f, 3.5f, 0.2f), Vector3.one * 1.8f, leaf, 0.3f);
        Prim.Make(PrimitiveType.Sphere, tree, new(-0.6f, 3.3f, -0.3f), Vector3.one * 1.6f, leaf, 0.3f);
    }

    static void BuildFlowers(Transform town)
    {
        var flowers = Prim.Group("Flowers", town, Vector3.zero);
        var rng = new System.Random(11);
        int placed = 0;
        for (int attempt = 0; attempt < 800 && placed < 90; attempt++)
        {
            var p = new Vector3((float)rng.NextDouble() * 44f - 22f, 0f, (float)rng.NextDouble() * 34f - 16f);
            if (!IsFree(p, 1.2f)) continue;
            var color = FlowerColors[rng.Next(FlowerColors.Length)];
            if (rng.NextDouble() < 0.25)
            {
                Prim.Make(PrimitiveType.Sphere, flowers, p + new Vector3(0f, 0.25f, 0f), new(1.1f, 0.7f, 1.1f), Leaves[rng.Next(Leaves.Length)], 0.2f, name: "Bush");
            }
            else
            {
                Prim.Make(PrimitiveType.Sphere, flowers, p + new Vector3(0f, 0.15f, 0f), Vector3.one * 0.22f, color, 0.12f, name: "Flower");
                Prim.Make(PrimitiveType.Sphere, flowers, p + new Vector3(0f, 0.05f, 0f), new(0.3f, 0.06f, 0.3f), Leaves[0], 0f, name: "Leaf");
            }
            placed++;
        }
    }

    // 避开道路、广场、农田、商店和房子
    static bool IsFree(Vector3 p, float margin)
    {
        if (Mathf.Abs(p.x) < 1.5f + margin || Mathf.Abs(p.z - 1f) < 1.5f + margin) return false;
        if (new Vector2(p.x, p.z - 1f).magnitude < 4.5f + margin) return false;
        var farm = new Rect(FarmArea.x - margin, FarmArea.y - margin, FarmArea.width + margin * 2f, FarmArea.height + margin * 2f);
        if (farm.Contains(new Vector2(p.x, p.z))) return false;
        if (Vector3.Distance(p, ShopPos) < 3f + margin) return false;
        foreach (var area in new[] { TailorArea, GameArea })
            if (new Rect(area.x - margin, area.y - margin, area.width + margin * 2f, area.height + margin * 2f).Contains(new Vector2(p.x, p.z))) return false;
        foreach (var h in HousePositions)
        {
            if (Vector3.Distance(p, h) < 3f + margin) return false;
            if (Mathf.Abs(p.x - h.x) < 1f + margin && p.z < h.z && p.z > 0f) return false;
        }
        return true;
    }

    static void BakeNavMesh(Transform town)
    {
        var surface = town.gameObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        surface.BuildNavMesh();
        AssetDatabase.DeleteAsset(NavMeshPath);
        AssetDatabase.CreateAsset(surface.navMeshData, NavMeshPath);
    }

    static GameObject BuildPlayer()
    {
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0f, 0f, -4.5f);
        player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        MoleBuilder.Build(player.transform, new Color(0.56f, 0.38f, 0.28f), CosmeticDatabase.DefaultHat);
        Prim.SetLayerRecursive(player, 2);   // Ignore Raycast：点击射线穿过自己
        var agent = player.AddComponent<NavMeshAgent>();
        agent.radius = 0.45f;
        agent.height = 1.2f;
        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerAppearance>();
        return player;
    }

    static void BuildCamera(GameObject player)
    {
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.7f, 0.86f, 1f);
        cam.fieldOfView = 35f;
        cam.farClipPlane = 150f;
        camGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
        var follow = camGo.AddComponent<FollowCamera>();
        follow.target = player.transform;
        follow.SnapToTarget();
    }

    // ---------- 预览截图（只用于验证，不会保存进场景） ----------

    static void CapturePreview()
    {
        string[] crops = { "strawberry", "carrot", "pumpkin", "strawberry", "carrot", "pumpkin" };
        int[] stages = { 3, 3, 3, 1, 2, 0 };
        var plots = Object.FindObjectsByType<FarmPlot>(FindObjectsSortMode.None).OrderBy(p => p.plotId).ToArray();
        for (int i = 0; i < plots.Length; i++)
            plots[i].LoadFrom(new PlotSave { plotId = plots[i].plotId, cropId = crops[i], stage = stages[i], watered = i == 4, stageStart = GameClock.Now });

        var cam = Camera.main;
        var player = GameObject.Find("Player").transform;
        player.position = new Vector3(4f, 0f, 0.5f);
        player.rotation = Quaternion.Euler(0f, 120f, 0f);
        cam.GetComponent<FollowCamera>().SnapToTarget();
        Capture(cam, "MoleTown_play.png");

        cam.transform.SetPositionAndRotation(new Vector3(0f, 30f, -30f), Quaternion.Euler(42f, 0f, 0f));
        Capture(cam, "MoleTown_overview.png");

        var lineup = new GameObject("Lineup").transform;
        lineup.position = new Vector3(0f, 0f, -30f);
        var hats = CosmeticDatabase.InSlot(CosmeticSlot.Hat).Where(c => c.id != CosmeticDatabase.NoHat).ToArray();
        for (int i = 0; i < hats.Length; i++)
        {
            var m = Prim.Group(hats[i].id, lineup, new((i - (hats.Length - 1) * 0.5f) * 1.5f, 0f, 1.4f));
            m.localRotation = Quaternion.Euler(0f, 180f, 0f);
            MoleBuilder.Build(m, new Color(0.56f, 0.38f, 0.28f), hats[i].id);
        }
        var outfits = CosmeticDatabase.InSlot(CosmeticSlot.Outfit).Where(c => c.id != CosmeticDatabase.NoOutfit).ToArray();
        for (int i = 0; i < outfits.Length; i++)
        {
            var m = Prim.Group(outfits[i].id, lineup, new((i - (outfits.Length - 1) * 0.5f) * 1.6f, 0f, -0.6f));
            m.localRotation = Quaternion.Euler(0f, outfits[i].id == "cape" ? 150f : 180f, 0f);
            MoleBuilder.Build(m, new Color(0.56f, 0.38f, 0.28f), CosmeticDatabase.NoHat, outfits[i].id);
        }
        cam.transform.position = lineup.position + new Vector3(0f, 3.6f, -8.5f);
        cam.transform.LookAt(lineup.position + new Vector3(0f, 0.6f, 0.4f));
        Capture(cam, "MoleTown_wardrobe.png");

        EditorSceneManager.OpenScene(ScenePath);   // 丢弃预览改动
    }

    static void Capture(Camera cam, string file)
    {
        const int w = 1600, h = 900;
        // 线性读写：URP 后处理已经做过 sRGB 编码，再用 sRGB 贴图会重复编码、画面发白
        // 注意：批处理模式离屏渲染拿不到深度图，截图里没有深度边缘光（编辑器和游戏内正常）
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear) { antiAliasing = 4 };
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        File.WriteAllBytes(file, tex.EncodeToPNG());
        RenderTexture.active = null;
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Debug.Log("[MoleTown] 截图：" + Path.GetFullPath(file));
    }
}
