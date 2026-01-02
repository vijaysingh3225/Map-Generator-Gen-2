using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using WorldGen.Debug;

namespace WorldGen.Core
{
    public sealed class WorldGenerator : MonoBehaviour
    {
        public WorldGenSettings settings;
        public List<GenerationStepAsset> steps = new List<GenerationStepAsset>();

        public bool clearPreviousRuntimeRoot = true;
        public bool autoGenerateOnPlay = false;

        [SerializeField, HideInInspector] private string lastOutputPath;

        public string LastOutputPath => lastOutputPath;

        private const string RuntimeRootName = "WorldGen_RuntimeRoot";

        private void Start()
        {
            if (autoGenerateOnPlay && Application.isPlaying)
            {
                Generate();
            }
        }

        public void Generate()
        {
            if (settings == null)
            {
                UnityEngine.Debug.LogError("WorldGenerator: Missing settings (WorldGenSettings).");
                return;
            }

            var outputPath = DebugPaths.ResolveRunOutputPath(settings);
            DebugPaths.EnsureDirectory(outputPath);

            var ctx = new WorldContext(settings, settings.seed, outputPath);
            lastOutputPath = outputPath;

            try
            {
                DebugLog.Log(ctx, $"WorldGen run started. {settings.GetSettingsSummary()}");
                PrepareRuntimeRoot(ctx);
                RunSteps(ctx);
                ExportDensitySlicesIfEnabled(ctx);
                WriteOutputs(ctx);
                ExportTestPngIfEnabled(ctx);
                DebugLog.Log(ctx, "WorldGen run finished.");
            }
            catch (Exception ex)
            {
                DebugLog.Error(ctx, $"Generation failed: {ex}");
                // Best-effort: still write report/log to disk so failures are debuggable.
                try { WriteOutputs(ctx); } catch { /* ignored */ }
                // Swallow after logging so editor re-runs are painless; details are in report.txt.
                return;
            }
        }

        private void ExportDensitySlicesIfEnabled(WorldContext ctx)
        {
            if (ctx.settings == null || !ctx.settings.exportDensitySlices) return;
            if (ctx.density == null) return;

            var files = DebugSlices.ExportDensitySlices(ctx);
            ctx.densitySliceFiles.Clear();
            ctx.densitySliceFiles.AddRange(files);
        }

        private void PrepareRuntimeRoot(WorldContext ctx)
        {
            if (!clearPreviousRuntimeRoot)
            {
                ctx.runtimeRoot = GameObject.Find(RuntimeRootName) ?? new GameObject(RuntimeRootName);
                return;
            }

            var existing = GameObject.Find(RuntimeRootName);
            if (existing != null)
            {
                if (Application.isPlaying) Destroy(existing);
                else DestroyImmediate(existing);
            }

            ctx.runtimeRoot = new GameObject(RuntimeRootName);
        }

        private void RunSteps(WorldContext ctx)
        {
            if (steps == null || steps.Count == 0)
            {
                DebugLog.Warn(ctx, "No steps assigned. Nothing to do.");
                return;
            }

            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                if (step == null)
                {
                    ctx.stepReports.Add(new StepReport("<null step>", 0, "Skipped (null reference)"));
                    continue;
                }

                var name = string.IsNullOrWhiteSpace(step.StepName) ? step.name : step.StepName;
                DebugLog.Log(ctx, $"Step {i + 1}/{steps.Count}: {name} (starting)");

                var sw = Stopwatch.StartNew();
                string notes = null;
                try
                {
                    step.Generate(settings, ctx);
                }
                catch (Exception ex)
                {
                    notes = $"Exception: {ex.GetType().Name}";
                    DebugLog.Error(ctx, $"Step '{name}' threw: {ex}");
                    throw;
                }
                finally
                {
                    sw.Stop();
                    ctx.stepReports.Add(new StepReport(name, sw.Elapsed.TotalMilliseconds, notes));
                    DebugLog.Log(ctx, $"Step {i + 1}/{steps.Count}: {name} (done) in {sw.Elapsed.TotalMilliseconds:0.00} ms");
                }
            }
        }

        private void WriteOutputs(WorldContext ctx)
        {
            DebugPaths.EnsureDirectory(ctx.outputPath);

            var reportPath = Path.Combine(ctx.outputPath, "report.txt");
            File.WriteAllText(reportPath, BuildReportText(ctx));

            var runJsonPath = Path.Combine(ctx.outputPath, "run.json");
            File.WriteAllText(runJsonPath, BuildRunJson(ctx));
        }

        private string BuildReportText(WorldContext ctx)
        {
            var sb = new StringBuilder(2048);
            sb.AppendLine("WorldGen Report");
            sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"OutputPath: {ctx.outputPath}");
            sb.AppendLine($"Settings: {ctx.settings.GetSettingsSummary()}");
            sb.AppendLine();

            sb.AppendLine("Density:");
            sb.AppendLine($"  gridSize: {ctx.settings.gridSize}");
            if (ctx.density == null)
            {
                sb.AppendLine("  (no density field)");
            }
            else
            {
                if (!ctx.hasDensityStats)
                {
                    // Best effort: density may exist even if a step didn't compute stats.
                    DebugSlices.EnsureDensityStats(ctx);
                }

                if (ctx.hasDensityStats)
                {
                    var ds = ctx.densityStats;
                    sb.AppendLine($"  count: {ds.count}");
                    sb.AppendLine($"  min/max: {ds.min:0.###} / {ds.max:0.###}");
                    sb.AppendLine($"  mean/std: {ds.mean:0.###} / {ds.std:0.###}");
                    sb.AppendLine($"  p01/p10/p50/p90/p99: {ds.p01:0.###} / {ds.p10:0.###} / {ds.p50:0.###} / {ds.p90:0.###} / {ds.p99:0.###}");
                    sb.AppendLine($"  displayMin/displayMax: {ds.displayMin:0.###} / {ds.displayMax:0.###}");
                }

                if (ctx.densitySliceFiles != null && ctx.densitySliceFiles.Count > 0)
                {
                    sb.AppendLine("  exportedSlices:");
                    for (int i = 0; i < ctx.densitySliceFiles.Count; i++)
                    {
                        sb.AppendLine($"    - {ctx.densitySliceFiles[i]}");
                    }
                }
                else
                {
                    sb.AppendLine("  exportedSlices: (none)");
                }
            }

            sb.AppendLine();
            sb.AppendLine("Steps:");
            if (ctx.stepReports.Count == 0)
            {
                sb.AppendLine("  (none)");
            }
            else
            {
                for (int i = 0; i < ctx.stepReports.Count; i++)
                {
                    var r = ctx.stepReports[i];
                    sb.AppendLine($"  - {r.stepName}: {r.ms:0.00} ms{(string.IsNullOrWhiteSpace(r.notes) ? "" : $" ({r.notes})")}");
                    if (r.counters != null && r.counters.Count > 0)
                    {
                        foreach (var kvp in r.counters)
                        {
                            sb.AppendLine($"      * {kvp.Key}: {kvp.Value}");
                        }
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine("Run Log:");
            sb.AppendLine(ctx.runLog.ToString());

            return sb.ToString();
        }

        [Serializable]
        private sealed class RunMetadata
        {
            public int seed;
            public Vector3 worldSize;
            public float voxelSize;
            public string outputPath;
            public string outputRoot;
            public RunIdMode runIdMode;
            public string[] steps;
        }

        private string BuildRunJson(WorldContext ctx)
        {
            var meta = new RunMetadata
            {
                seed = ctx.seed,
                worldSize = ctx.settings.worldSize,
                voxelSize = ctx.settings.voxelSize,
                outputPath = ctx.outputPath,
                outputRoot = ctx.settings.outputRoot,
                runIdMode = ctx.settings.runIdMode,
                steps = GetStepNames(),
            };

            return JsonUtility.ToJson(meta, true);
        }

        private string[] GetStepNames()
        {
            if (steps == null || steps.Count == 0) return Array.Empty<string>();
            var names = new string[steps.Count];
            for (int i = 0; i < steps.Count; i++)
            {
                var s = steps[i];
                names[i] = s == null ? "<null>" : (string.IsNullOrWhiteSpace(s.StepName) ? s.name : s.StepName);
            }
            return names;
        }

        private void ExportTestPngIfEnabled(WorldContext ctx)
        {
            if (ctx.settings == null || !ctx.settings.exportTestPng) return;

            const int w = 256;
            const int h = 256;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
            try
            {
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        var gx = x / (float)(w - 1);
                        var gy = y / (float)(h - 1);
                        var checker = ((x / 16 + y / 16) % 2) == 0 ? 0.15f : 0.0f;
                        var c = new Color(gx, gy, 0.25f + checker, 1f);
                        tex.SetPixel(x, y, c);
                    }
                }
                tex.Apply(false, false);

                var pngPath = Path.Combine(ctx.outputPath, "test.png");
                DebugPng.WritePng(tex, pngPath);
                DebugLog.Log(ctx, $"Exported test PNG: {pngPath}");
            }
            finally
            {
                if (Application.isPlaying) Destroy(tex);
                else DestroyImmediate(tex);
            }
        }
    }
}


