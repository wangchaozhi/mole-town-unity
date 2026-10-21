using UnityEngine;

namespace MoleTown.Toon
{
    // 创建和配置 MoleTown/Toon 材质的入口，属性名集中在这里
    public static class ToonMaterial
    {
        public const string ShaderName = "MoleTown/Toon";
        public const string DepthRimKeyword = "_DEPTH_RIM";

        public static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        public static readonly int ShadowColor = Shader.PropertyToID("_ShadowColor");
        public static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");
        public static readonly int OutlineWidth = Shader.PropertyToID("_OutlineWidth");
        public static readonly int UseSmoothNormal = Shader.PropertyToID("_UseSmoothNormal");
        public static readonly int RimColor = Shader.PropertyToID("_RimColor");
        public static readonly int DepthRim = Shader.PropertyToID("_DepthRim");
        public static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        public static readonly Color DefaultShadowTint = new(0.72f, 0.68f, 0.86f);

        public static Shader Shader => Shader.Find(ShaderName);

        // outlineWidth 为 0 时不画描边；smoothNormals 表示网格的 tangent 里存了平滑法线
        public static Material Create(Color baseColor, float outlineWidth = 0.25f, bool smoothNormals = false, bool depthRim = true)
        {
            var mat = new Material(Shader);
            mat.SetColor(BaseColor, baseColor);
            mat.SetColor(ShadowColor, DefaultShadowTint);
            mat.SetFloat(OutlineWidth, outlineWidth);
            // 描边用本色压暗，比纯黑描边更柔和
            mat.SetColor(OutlineColor, new Color(baseColor.r * 0.32f, baseColor.g * 0.26f, baseColor.b * 0.3f, 1f));
            mat.SetFloat(UseSmoothNormal, smoothNormals ? 1f : 0f);
            mat.SetColor(RimColor, new Color(1f, 1f, 1f, 0.3f));
            SetDepthRim(mat, depthRim);
            return mat;
        }

        public static void SetDepthRim(Material mat, bool enabled)
        {
            mat.SetFloat(DepthRim, enabled ? 1f : 0f);
            if (enabled) mat.EnableKeyword(DepthRimKeyword);
            else mat.DisableKeyword(DepthRimKeyword);
        }
    }
}
