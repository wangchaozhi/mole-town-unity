// 打包入口：CI（GameCI unity-builder 的 buildMethod）和本地菜单共用
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MoleTown.Editor
{
    public static class BuildScript
    {
        const string ProductName = "MoleTown";

        [MenuItem("Tools/MoleTown/Build Player (当前平台)")]
        static void BuildCurrentPlatform()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            if (Run(target, DefaultPath(target), null))
                EditorUtility.RevealInFinder(DefaultPath(target));
        }

        // 命令行：-executeMethod MoleTown.Editor.BuildScript.Build
        //   -customBuildTarget <BuildTarget>  不传时用当前激活的平台（-buildTarget）
        //   -customBuildPath <输出路径>        不传时输出到 Builds/<平台>/
        //   -buildVersion <版本号>             写入 PlayerSettings.bundleVersion
        public static void Build()
        {
            var args = ParseArgs();
            var target = EditorUserBuildSettings.activeBuildTarget;
            if (args.TryGetValue("customBuildTarget", out var targetName))
            {
                if (!Enum.TryParse(targetName, out target))
                {
                    Debug.LogError($"[Build] 不认识的平台：{targetName}");
                    EditorApplication.Exit(1);
                    return;
                }
            }
            args.TryGetValue("customBuildPath", out var path);
            args.TryGetValue("buildVersion", out var version);
            bool ok = Run(target, string.IsNullOrEmpty(path) ? DefaultPath(target) : path, version);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        static bool Run(BuildTarget target, string path, string version)
        {
            if (!string.IsNullOrEmpty(version)) PlayerSettings.bundleVersion = version;

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            foreach (var missing in scenes.Where(p => !File.Exists(p)))
                Debug.LogWarning($"[Build] 构建设置里的场景不存在，已跳过：{missing}");
            scenes = scenes.Where(File.Exists).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[Build] 构建设置里没有场景，先运行 Tools/MoleTown/Build Scene");
                return false;
            }

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                target = target,
                targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                locationPathName = path,
                options = BuildOptions.None,
            });
            var summary = report.summary;
            Debug.Log($"[Build] {target} {PlayerSettings.bundleVersion} → {path}：{summary.result}，" +
                      $"{summary.totalSize / 1048576f:0.0} MB，{summary.totalErrors} 个错误，耗时 {summary.totalTime.TotalSeconds:0} 秒");
            return summary.result == BuildResult.Succeeded;
        }

        static string DefaultPath(BuildTarget target) => target switch
        {
            BuildTarget.StandaloneWindows64 => $"Builds/Windows/{ProductName}.exe",
            BuildTarget.StandaloneOSX => $"Builds/macOS/{ProductName}.app",
            BuildTarget.StandaloneLinux64 => $"Builds/Linux/{ProductName}",
            _ => $"Builds/{target}/{ProductName}",
        };

        static Dictionary<string, string> ParseArgs()
        {
            var result = new Dictionary<string, string>();
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith("-")) continue;
                string key = args[i].TrimStart('-');
                string value = i + 1 < args.Length && !args[i + 1].StartsWith("-") ? args[i + 1] : "";
                result[key] = value;
            }
            return result;
        }
    }
}
