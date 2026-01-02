using System;
using System.Collections.Generic;
using System.IO;
using WorldGen.Core;

namespace WorldGen.Debug
{
    public static class DebugSlices
    {
        public static void EnsureDensityStats(WorldContext ctx)
        {
            EnsureDensityStatsInternal(ctx);
        }

        public static List<string> ExportDensitySlices(WorldContext ctx, string prefix = "")
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (ctx.settings == null) throw new ArgumentNullException(nameof(ctx.settings));
            if (ctx.density == null) throw new InvalidOperationException("ctx.density is null");

            EnsureDensityStatsInternal(ctx);

            prefix ??= string.Empty;
            var exported = new List<string>();
            var vmin = ctx.densityStats.displayMin;
            var vmax = ctx.densityStats.displayMax;

            var outDir = ctx.outputPath;
            Directory.CreateDirectory(outDir);

            // XZ slices at selected Y.
            var ys = ctx.settings.debugSliceYs ?? Array.Empty<int>();
            var xz = new float[ctx.density.sizeX * ctx.density.sizeZ];
            foreach (var y in ys)
            {
                if (y < 0 || y >= ctx.density.sizeY)
                {
                    DebugLog.Warn(ctx, $"Skipping XZ slice: y={y} out of range [0..{ctx.density.sizeY - 1}]");
                    continue;
                }

                ctx.density.GetSliceXZ(y, xz);
                var file = $"{prefix}density_xz_y{y:D3}.png";
                var path = Path.Combine(outDir, file);
                DebugPng.ExportGrayscaleFloatSlice(path, xz, ctx.density.sizeX, ctx.density.sizeZ, vmin, vmax);
                exported.Add(file);
                ctx.densitySliceFiles.Add(file);
            }

            // XY slices at selected Z.
            var zs = ctx.settings.debugSliceZs ?? Array.Empty<int>();
            var xy = new float[ctx.density.sizeX * ctx.density.sizeY];
            foreach (var z in zs)
            {
                if (z < 0 || z >= ctx.density.sizeZ)
                {
                    DebugLog.Warn(ctx, $"Skipping XY slice: z={z} out of range [0..{ctx.density.sizeZ - 1}]");
                    continue;
                }

                ctx.density.GetSliceXY(z, xy);
                var file = $"{prefix}density_xy_z{z:D3}.png";
                var path = Path.Combine(outDir, file);
                DebugPng.ExportGrayscaleFloatSlice(path, xy, ctx.density.sizeX, ctx.density.sizeY, vmin, vmax);
                exported.Add(file);
                ctx.densitySliceFiles.Add(file);
            }

            // YZ slices at selected X.
            var xs = ctx.settings.debugSliceXs ?? Array.Empty<int>();
            var yz = new float[ctx.density.sizeY * ctx.density.sizeZ];
            foreach (var x in xs)
            {
                if (x < 0 || x >= ctx.density.sizeX)
                {
                    DebugLog.Warn(ctx, $"Skipping YZ slice: x={x} out of range [0..{ctx.density.sizeX - 1}]");
                    continue;
                }

                ctx.density.GetSliceYZ(x, yz);
                var file = $"{prefix}density_yz_x{x:D3}.png";
                var path = Path.Combine(outDir, file);
                DebugPng.ExportGrayscaleFloatSlice(path, yz, ctx.density.sizeY, ctx.density.sizeZ, vmin, vmax);
                exported.Add(file);
                ctx.densitySliceFiles.Add(file);
            }

            DebugLog.Log(ctx, $"Exported {exported.Count} density slice PNG(s) using display range [{vmin:0.###}..{vmax:0.###}]");
            return exported;
        }

        private static void EnsureDensityStatsInternal(WorldContext ctx)
        {
            if (ctx.hasDensityStats) return;
            if (ctx.density == null) return;

            // Compute minimal stats + percentiles for display scaling.
            var s = DebugStats.Summarize(ctx.density.data);

            var sorted = new float[ctx.density.data.Length];
            Array.Copy(ctx.density.data, sorted, ctx.density.data.Length);
            Array.Sort(sorted);

            float P(float p)
            {
                if (sorted.Length == 0) return float.NaN;
                if (p <= 0) return sorted[0];
                if (p >= 1) return sorted[sorted.Length - 1];

                var idx = p * (sorted.Length - 1);
                var lo = (int)Math.Floor(idx);
                var hi = (int)Math.Ceiling(idx);
                if (lo == hi) return sorted[lo];
                var t = (float)(idx - lo);
                return sorted[lo] * (1f - t) + sorted[hi] * t;
            }

            ctx.densityStats = new DensityFieldStats
            {
                count = s.count,
                min = s.min,
                max = s.max,
                mean = s.mean,
                std = s.std,
                p01 = P(0.01f),
                p10 = P(0.10f),
                p50 = P(0.50f),
                p90 = P(0.90f),
                p99 = P(0.99f),
            };

            ctx.densityStats.displayMin = ctx.densityStats.p01;
            ctx.densityStats.displayMax = ctx.densityStats.p99;
            if (ctx.densityStats.displayMax <= ctx.densityStats.displayMin)
            {
                ctx.densityStats.displayMin = ctx.densityStats.min;
                ctx.densityStats.displayMax = ctx.densityStats.max;
            }

            ctx.hasDensityStats = true;
        }
    }
}


