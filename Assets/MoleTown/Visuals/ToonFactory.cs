using System.Collections.Generic;
using MoleTown.Toon;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoleTown
{
    // 编辑器里搭场景时，材质和网格存成资源文件，场景才能正确保存引用；运行时直接在内存里创建
    static class GeneratedAssets
    {
        public const string Root = "Assets/MoleTown/Generated";

        public static bool Authoring
        {
            get
            {
#if UNITY_EDITOR
                return !Application.isPlaying;
#else
                return false;
#endif
            }
        }

#if UNITY_EDITOR
        public static T LoadOrCreate<T>(string folder, string file, System.Func<T> create) where T : Object
        {
            string path = $"{Root}/{folder}/{file}";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset) return asset;
            EnsureFolder($"{Root}/{folder}");
            asset = create();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }
#endif
    }

    public static class ToonMaterials
    {
        static readonly Dictionary<string, Material> cache = new();

        public static Material Get(Color color, float outline = 0.25f, bool smoothNormals = false)
        {
            string key = $"{ColorUtility.ToHtmlStringRGB(color)}_{Mathf.RoundToInt(outline * 100)}{(smoothNormals ? "_s" : "")}";
            if (cache.TryGetValue(key, out var mat) && mat) return mat;
#if UNITY_EDITOR
            if (GeneratedAssets.Authoring)
                return cache[key] = GeneratedAssets.LoadOrCreate("Materials", $"Toon_{key}.mat", () => ToonMaterial.Create(color, outline, smoothNormals));
#endif
            return cache[key] = ToonMaterial.Create(color, outline, smoothNormals);
        }

    }

    public static class MeshUtil
    {
        static readonly Dictionary<PrimitiveType, Mesh> cache = new();

        public static Mesh SmoothedPrimitive(PrimitiveType type, Mesh source)
        {
            if (cache.TryGetValue(type, out var mesh) && mesh) return mesh;
#if UNITY_EDITOR
            if (GeneratedAssets.Authoring)
                return cache[type] = GeneratedAssets.LoadOrCreate("Meshes", $"{type}_Smooth.asset", () => SmoothNormalUtility.CreateSmoothedCopy(source));
#endif
            return cache[type] = SmoothNormalUtility.CreateSmoothedCopy(source);
        }

    }

    // 用基础几何体快速拼模型的小工具
    public static class Prim
    {
        public static GameObject Make(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Color color,
            float outline = 0.25f, Vector3? euler = null, bool keepCollider = false, string name = null)
        {
            var go = GameObject.CreatePrimitive(type);
            if (name != null) go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            go.transform.localScale = scale;
            if (!keepCollider) DestroySafe(go.GetComponent<Collider>());

            // 立方体和圆柱是硬边法线，描边需要平滑法线才不会断开
            bool smooth = outline > 0f && (type == PrimitiveType.Cube || type == PrimitiveType.Cylinder);
            if (smooth)
            {
                var filter = go.GetComponent<MeshFilter>();
                filter.sharedMesh = MeshUtil.SmoothedPrimitive(type, filter.sharedMesh);
            }
            go.GetComponent<MeshRenderer>().sharedMaterial = ToonMaterials.Get(color, outline, smooth);
            return go;
        }

        public static Transform Group(string name, Transform parent, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            return go.transform;
        }

        public static void DestroySafe(Object obj)
        {
            if (!obj) return;
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }

        public static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var child = t.GetChild(i).gameObject;
                child.SetActive(false);   // 运行时 Destroy 会延迟到帧末，先隐藏避免重叠闪烁
                DestroySafe(child);
            }
        }

        public static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
        }
    }
}
