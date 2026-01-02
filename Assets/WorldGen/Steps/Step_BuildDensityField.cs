using System;
using UnityEngine;
using WorldGen.Core;
using WorldGen.Debug;

namespace WorldGen.Steps
{
    [CreateAssetMenu(fileName = "Step_BuildDensityField", menuName = "WorldGen/Steps/Build Density Field", order = 20)]
    public sealed class Step_BuildDensityField : GenerationStepAsset
    {
        public override string StepName => "BuildDensityField";

        public override void Generate(WorldGenSettings settings, WorldContext ctx)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            var gs = settings.gridSize;
            if (gs.x <= 0 || gs.y <= 0 || gs.z <= 0)
            {
                throw new InvalidOperationException($"Invalid gridSize: {gs}");
            }

            ctx.density = new DensityField3D(gs.x, gs.y, gs.z, settings.voxelSize);

            // Deterministic offsets derived from seeded RNG.
            var ox = (float)ctx.rng.NextDouble() * 10000f;
            var oz = (float)ctx.rng.NextDouble() * 10000f;
            var ox2 = (float)ctx.rng.NextDouble() * 10000f;
            var oz2 = (float)ctx.rng.NextDouble() * 10000f;

            var yMax = gs.y * settings.voxelSize;
            var baseHeight = yMax * 0.45f;

            const float freq = 0.035f;
            const float freq2 = 0.08f;
            var amp = yMax * 0.18f;     // primary noise amplitude
            var ridgeAmp = yMax * 0.10f; // ridge term amplitude

            var slopeX = yMax * 0.07f; // gentle slope across X
            var slopeZ = yMax * 0.04f; // gentle slope across Z

            ctx.density.Fill((x, y, z) =>
            {
                var wx = x * settings.voxelSize;
                var wy = y * settings.voxelSize;
                var wz = z * settings.voxelSize;

                // 2D noise in XZ (deterministic for given seed + settings).
                var n = Mathf.PerlinNoise(wx * freq + ox, wz * freq + oz) - 0.5f; // [-0.5..0.5]
                var n2 = Mathf.PerlinNoise(wx * freq2 + ox2, wz * freq2 + oz2);   // [0..1]
                var ridge = Mathf.Abs(n2 * 2f - 1f);                               // [0..1] ridge-like

                var hx = (x / (float)Mathf.Max(1, gs.x - 1) - 0.5f) * slopeX;
                var hz = (z / (float)Mathf.Max(1, gs.z - 1) - 0.5f) * slopeZ;

                var h = baseHeight + hx + hz + n * amp + ridge * ridgeAmp;

                // Positive = solid, negative = air.
                return h - wy;
            });

            // Stats (min/max/mean/std + percentiles) for later reporting & slice scaling.
            ctx.densityStats = ComputeStats(ctx.density.data);
            ctx.hasDensityStats = true;

            DebugLog.Log(ctx,
                $"Density stats: min={ctx.densityStats.min:0.###}, max={ctx.densityStats.max:0.###}, " +
                $"mean={ctx.densityStats.mean:0.###}, std={ctx.densityStats.std:0.###}, " +
                $"p01={ctx.densityStats.p01:0.###}, p10={ctx.densityStats.p10:0.###}, p50={ctx.densityStats.p50:0.###}, " +
                $"p90={ctx.densityStats.p90:0.###}, p99={ctx.densityStats.p99:0.###} (display [{ctx.densityStats.displayMin:0.###}..{ctx.densityStats.displayMax:0.###}])");
        }

        private static DensityFieldStats ComputeStats(float[] values)
        {
            var s = DebugStats.Summarize(values);

            // Sort once for percentiles.
            var sorted = new float[values.Length];
            Array.Copy(values, sorted, values.Length);
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

            var stats = new DensityFieldStats
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

            stats.displayMin = stats.p01;
            stats.displayMax = stats.p99;
            if (stats.displayMax <= stats.displayMin)
            {
                // Fallback to full range if degenerate.
                stats.displayMin = stats.min;
                stats.displayMax = stats.max;
            }

            return stats;
        }
    }
}


