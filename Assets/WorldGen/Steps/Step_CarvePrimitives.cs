using System;
using System.Collections.Generic;
using UnityEngine;
using WorldGen.Core;
using WorldGen.Debug;

namespace WorldGen.Steps
{
    [CreateAssetMenu(fileName = "Step_CarvePrimitives", menuName = "WorldGen/Steps/Carve Primitives", order = 30)]
    public sealed class Step_CarvePrimitives : GenerationStepAsset
    {
        public override string StepName => "CarvePrimitives";

        private struct CapsuleVoid
        {
            public Vector3 a;
            public Vector3 b;
            public float r;
        }

        private struct SphereVoid
        {
            public Vector3 c;
            public float r;
        }

        private struct ArchVoid
        {
            public Vector3 a;
            public Vector3 b;
            public float r;
            public float maxY;
        }

        public override void Generate(WorldGenSettings settings, WorldContext ctx)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            if (ctx.density == null)
            {
                DebugLog.Warn(ctx, "CarvePrimitives: ctx.density is null (did you run BuildDensityField first?). Skipping.");
                return;
            }

            DebugSlices.EnsureDensityStats(ctx);
            var beforeStats = ctx.densityStats;
            DebugLog.Log(ctx,
                $"CarvePrimitives BEFORE stats: min={beforeStats.min:0.###}, max={beforeStats.max:0.###}, mean={beforeStats.mean:0.###}, std={beforeStats.std:0.###}, " +
                $"p01={beforeStats.p01:0.###}, p50={beforeStats.p50:0.###}, p99={beforeStats.p99:0.###}");

            if (settings.exportBeforeAfterSlices)
            {
                DebugSlices.ExportDensitySlices(ctx, "before_");
            }

            var carveStrength = settings.carveStrength <= 0 ? 1f : settings.carveStrength;
            var rng = new System.Random(unchecked(ctx.seed + settings.carveSeedOffset));

            var vs = ctx.density.voxelSize;
            var maxX = (ctx.density.sizeX - 1) * vs;
            var maxY = (ctx.density.sizeY - 1) * vs;
            var maxZ = (ctx.density.sizeZ - 1) * vs;

            // Build primitives deterministically.
            var tunnels = BuildTunnels(settings, rng, maxX, maxY, maxZ);
            var pockets = BuildPockets(settings, rng, maxX, maxY, maxZ);
            var arch = BuildArch(settings, rng, maxX, maxY, maxZ);

            // Single pass carve.
            const float eps = 1e-6f;
            int affected = 0;
            int solidToAir = 0;
            int insideAny = 0;

            for (int z = 0; z < ctx.density.sizeZ; z++)
            {
                var pz = z * vs;
                for (int y = 0; y < ctx.density.sizeY; y++)
                {
                    var py = y * vs;
                    for (int x = 0; x < ctx.density.sizeX; x++)
                    {
                        var px = x * vs;
                        var p = new Vector3(px, py, pz);

                        var d0 = ctx.density.Get(x, y, z);
                        var d = d0;

                        // Tunnels
                        for (int i = 0; i < tunnels.Length; i++)
                        {
                            var t = tunnels[i];
                            var sdf = Sdf.Capsule(p, t.a, t.b, t.r) * carveStrength;
                            if (sdf < 0f) insideAny++;
                            d = Mathf.Min(d, sdf);
                        }

                        // Spherical pockets
                        for (int i = 0; i < pockets.Length; i++)
                        {
                            var s = pockets[i];
                            var sdf = Sdf.Sphere(p, s.c, s.r) * carveStrength;
                            if (sdf < 0f) insideAny++;
                            d = Mathf.Min(d, sdf);
                        }

                        // Arch carve (capsule but only below maxY)
                        if (p.y <= arch.maxY)
                        {
                            var sdf = Sdf.Capsule(p, arch.a, arch.b, arch.r) * carveStrength;
                            if (sdf < 0f) insideAny++;
                            d = Mathf.Min(d, sdf);
                        }

                        if (Mathf.Abs(d - d0) > eps) affected++;
                        if (d0 > 0f && d <= 0f) solidToAir++;

                        ctx.density.Set(x, y, z, d);
                    }
                }
            }

            // After stats.
            ctx.densityStats = ComputeStats(ctx.density.data);
            ctx.hasDensityStats = true;

            var afterStats = ctx.densityStats;
            DebugLog.Log(ctx,
                $"CarvePrimitives AFTER stats: min={afterStats.min:0.###}, max={afterStats.max:0.###}, mean={afterStats.mean:0.###}, std={afterStats.std:0.###}, " +
                $"p01={afterStats.p01:0.###}, p50={afterStats.p50:0.###}, p99={afterStats.p99:0.###}");

            if (settings.exportBeforeAfterSlices)
            {
                DebugSlices.ExportDensitySlices(ctx, "after_");
            }

            ctx.pendingStepCounters = new Dictionary<string, int>
            {
                { "tunnels", tunnels.Length },
                { "cavePockets", pockets.Length },
                { "affectedVoxels", affected },
                { "solidToAir", solidToAir },
            };

            ctx.pendingStepNotes =
                $"carveStrength={carveStrength:0.###}, tunnelRadius={settings.tunnelRadius:0.###}, archRadius={settings.archRadius:0.###}, " +
                $"solidToAir={solidToAir}, affected={affected}";

            DebugLog.Log(ctx, $"CarvePrimitives: affectedVoxels={affected}, solidToAir={solidToAir}");
        }

        private static CapsuleVoid[] BuildTunnels(WorldGenSettings settings, System.Random rng, float maxX, float maxY, float maxZ)
        {
            var count = Mathf.Max(0, settings.tunnelCount);
            var tunnels = new CapsuleVoid[count];
            for (int i = 0; i < count; i++)
            {
                // Alternate orientation so slices show obvious changes.
                var yA = RandRange(rng, maxY * 0.45f, maxY * 0.65f);
                var yB = yA + RandRange(rng, -maxY * 0.05f, maxY * 0.05f);

                if (i % 2 == 0)
                {
                    var z0 = RandRange(rng, maxZ * 0.15f, maxZ * 0.85f);
                    var z1 = RandRange(rng, maxZ * 0.15f, maxZ * 0.85f);
                    tunnels[i] = new CapsuleVoid
                    {
                        a = new Vector3(0f, yA, z0),
                        b = new Vector3(maxX, yB, z1),
                        r = Mathf.Max(0.01f, settings.tunnelRadius),
                    };
                }
                else
                {
                    var x0 = RandRange(rng, maxX * 0.15f, maxX * 0.85f);
                    var x1 = RandRange(rng, maxX * 0.15f, maxX * 0.85f);
                    tunnels[i] = new CapsuleVoid
                    {
                        a = new Vector3(x0, yA, 0f),
                        b = new Vector3(x1, yB, maxZ),
                        r = Mathf.Max(0.01f, settings.tunnelRadius),
                    };
                }
            }
            return tunnels;
        }

        private static SphereVoid[] BuildPockets(WorldGenSettings settings, System.Random rng, float maxX, float maxY, float maxZ)
        {
            var count = Mathf.Max(0, settings.cavePocketCount);
            var pockets = new SphereVoid[count];

            var rMin = Mathf.Max(0.01f, Mathf.Min(settings.cavePocketRadiusRange.x, settings.cavePocketRadiusRange.y));
            var rMax = Mathf.Max(rMin, Mathf.Max(settings.cavePocketRadiusRange.x, settings.cavePocketRadiusRange.y));

            for (int i = 0; i < count; i++)
            {
                var r = RandRange(rng, rMin, rMax);
                var c = new Vector3(
                    RandRange(rng, r, Mathf.Max(r, maxX - r)),
                    RandRange(rng, maxY * 0.25f, maxY * 0.60f),
                    RandRange(rng, r, Mathf.Max(r, maxZ - r))
                );

                pockets[i] = new SphereVoid { c = c, r = r };
            }
            return pockets;
        }

        private static ArchVoid BuildArch(WorldGenSettings settings, System.Random rng, float maxX, float maxY, float maxZ)
        {
            // Center near mid map; span along X with fixed Z.
            var centerZ = RandRange(rng, maxZ * 0.35f, maxZ * 0.65f);
            var centerX = RandRange(rng, maxX * 0.35f, maxX * 0.65f);

            var y = Mathf.Clamp(settings.archHeight, 0f, maxY);
            var r = Mathf.Max(0.01f, settings.archRadius);

            var halfSpan = r * 1.6f; // slightly wider than radius for a big opening
            var a = new Vector3(Mathf.Clamp(centerX - halfSpan, 0f, maxX), y, centerZ);
            var b = new Vector3(Mathf.Clamp(centerX + halfSpan, 0f, maxX), y, centerZ);

            var maxCarveY = Mathf.Clamp(y + Mathf.Max(0f, settings.archThickness), 0f, maxY);

            return new ArchVoid
            {
                a = a,
                b = b,
                r = r,
                maxY = maxCarveY,
            };
        }

        private static float RandRange(System.Random rng, float min, float max)
        {
            if (max < min) (min, max) = (max, min);
            var t = (float)rng.NextDouble();
            return min + (max - min) * t;
        }

        private static DensityFieldStats ComputeStats(float[] values)
        {
            var s = DebugStats.Summarize(values);

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
                stats.displayMin = stats.min;
                stats.displayMax = stats.max;
            }

            return stats;
        }
    }
}


