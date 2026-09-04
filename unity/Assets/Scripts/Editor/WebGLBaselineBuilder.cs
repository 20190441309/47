#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Patch47.EditorTools
{
    /// WebGL 基线构建:把首屏 ≤15MB 预算从假设变成实测(设计备忘 2026-09-03 决策,提前于 M4 验证)。
    /// 命令行:Tuanjie.exe -batchmode -projectPath ... -executeMethod Patch47.EditorTools.WebGLBaselineBuilder.Build -quit
    /// 输出:unity/Builds/WebGLBaseline(gitignored)+ Builds/WebGLBaseline-SIZE.txt 体积报告。
    public static class WebGLBaselineBuilder
    {
        public static void Build()
        {
            var scene = "Assets/Scenes/Ch1_Greybox.unity";
            if (!File.Exists(scene)) throw new FileNotFoundException($"缺少场景:{scene}(先运行 GreyboxSceneBuilder)");

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                throw new Exception("切换 WebGL 平台失败——编辑器可能未安装 WebGL 构建模块");
            }

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { scene },
                locationPathName = "Builds/WebGLBaseline",
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"WebGL 构建失败:{report.summary.result},错误数 {report.summary.totalErrors}");
            }

            WriteSizeReport("Builds/WebGLBaseline");
            Debug.Log("[WebGLBaselineBuilder] 构建完成,体积报告:Builds/WebGLBaseline-SIZE.txt");
        }

        private static void WriteSizeReport(string dir)
        {
            var builder = new StringBuilder();
            long total = 0;
            foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                var size = new FileInfo(file).Length;
                total += size;
                builder.AppendLine($"{size,12:N0}  {file}");
            }
            builder.Insert(0, $"total: {total / 1024f / 1024f:F2} MB(Brotli 压缩后)\n");
            File.WriteAllText($"{dir}-SIZE.txt", builder.ToString());
        }
    }
}
#endif
