using System.IO;
using UnityEditor;
using UnityEngine;

namespace MoleTown.Toon.Editor
{
    // 菜单 Tools/Toon/Bake Smooth Normals：给选中模型的网格生成一份带平滑法线的副本并替换
    public static class SmoothNormalBaker
    {
        const string OutputFolder = "Assets/SmoothNormalMeshes";

        [MenuItem("Tools/Toon/Bake Smooth Normals")]
        static void Bake()
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("请先在场景中选中模型。");
                return;
            }
            if (!AssetDatabase.IsValidFolder(OutputFolder))
                AssetDatabase.CreateFolder("Assets", Path.GetFileName(OutputFolder));

            int count = 0;
            foreach (var go in Selection.gameObjects)
            {
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (!mf.sharedMesh) continue;
                    Undo.RecordObject(mf, "Bake Smooth Normals");
                    mf.sharedMesh = SaveCopy(mf.sharedMesh);
                    count++;
                }
                foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (!smr.sharedMesh) continue;
                    Undo.RecordObject(smr, "Bake Smooth Normals");
                    smr.sharedMesh = SaveCopy(smr.sharedMesh);
                    count++;
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"已烘焙 {count} 个网格，输出到 {OutputFolder}");
        }

        static Mesh SaveCopy(Mesh source)
        {
            var mesh = SmoothNormalUtility.CreateSmoothedCopy(source);
            AssetDatabase.CreateAsset(mesh, AssetDatabase.GenerateUniqueAssetPath($"{OutputFolder}/{mesh.name}.asset"));
            return mesh;
        }
    }
}
