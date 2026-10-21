using System.Collections.Generic;
using UnityEngine;

namespace MoleTown.Toon
{
    // 硬边模型（立方体、圆柱）同一位置有多个法线不同的顶点，描边外扩时会裂开。
    // 把同位置顶点的法线取平均写进 tangent，描边 Pass 勾选 Use Smooth Normal 后就用它外扩。
    // 注意：会覆盖原来的切线，依赖法线贴图的材质不要和它共用网格。
    public static class SmoothNormalUtility
    {
        public static Mesh CreateSmoothedCopy(Mesh source)
        {
            var mesh = Object.Instantiate(source);
            mesh.name = source.name + "_Smooth";
            WriteSmoothNormalsToTangents(mesh);
            return mesh;
        }

        public static void WriteSmoothNormalsToTangents(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var sums = new Dictionary<Vector3, Vector3>();
            for (int i = 0; i < vertices.Length; i++)
            {
                sums.TryGetValue(vertices[i], out var sum);
                sums[vertices[i]] = sum + normals[i];
            }
            var tangents = new Vector4[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                var n = sums[vertices[i]].normalized;
                tangents[i] = new Vector4(n.x, n.y, n.z, 0f);
            }
            mesh.tangents = tangents;
        }
    }
}
