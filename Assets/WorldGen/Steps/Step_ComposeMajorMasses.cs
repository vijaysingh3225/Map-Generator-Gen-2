using System;
using System.Collections.Generic;
using UnityEngine;
using WorldGen.Core;
using WorldGen.Debug;

namespace WorldGen.Steps
{
    [CreateAssetMenu(fileName = "Step_ComposeMajorMasses", menuName = "WorldGen/Steps/Compose Major Masses", order = 25)]
    public sealed class Step_ComposeMajorMasses : GenerationStepAsset
    {
        public override string StepName => "ComposeMajorMasses";

        private struct MajorMass
        {
            public Vector3 center;
            public float radius;
            public float heightInfluence;
            public float addStrength;
            public float edgeFalloff;
        }

        private struct Overhang
        {
            public Vector3 center;
            public float radius;
            public float cutoffY; // carve only when p.y < cutoffY
        }

        private struct FloatingIsland
        {
            public Vector3 center;
            public float radius;
            public float boost;
        }

        public override void Generate(WorldGenSettings settings, WorldContext ctx)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            if (ctx.density == null)
            {
                DebugLog.Warn(ctx, "ComposeMajorMasses: ctx.density is null (did you run BuildDensityField first?). Skipping.");
                return;
            }

            var rng = new System.Random(unchecked(ctx.seed + settings.composeSeedOffset));

            var vs = ctx.density.voxelSize;
            var maxX = (ctx.density.sizeX - 1) * vs;
            var maxY = (ctx.density.sizeY - 1) * vs;
            var maxZ = (ctx.density.sizeZ - 1) * vs;

            if (settings.exportBeforeAfterSlices)
            {
                DebugSlices.EnsureDensityStats(ctx);
                DebugSlices.ExportDensitySlices(ctx, "m3_before_");
            }

            // Deterministic offsets for terrace noise.
            var terraceOx = (float)rng.NextDouble() * 10000f;
            var terraceOz = (float)rng.NextDouble() * 10000f;

            // Place major masses with rejection sampling in XZ.
            var masses = PlaceMajorMasses(settings, rng, maxX, maxY, maxZ);

            // Overhang undercuts (void spheres only below a cutoff height).
            var overhangs = PlaceOverhangs(settings, rng, maxX, maxY, maxZ);

            // Floating islands (solid boosts).
            var islands = PlaceIslands(settings, rng, maxX, maxY, maxZ);

            LogPlacement(ctx, settings, masses, overhangs, islands);

            // One voxel pass to apply: masses add + terraces + overhang voids + islands add.
            const float eps = 1e-6f;
            int affected = 0;

            var terraceBands = Mathf.Max(1, settings.terraceBands);
            var bandHeight = (maxY <= 0f) ? 1f : (maxY / terraceBands);
            var terraceStrength = settings.terraceStrength;
            var terraceFreq = Mathf.Max(0f, settings.terraceNoiseFreq);
            var terraceAmp = settings.terraceNoiseAmp;

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

                        // Major masses (solid bias).
                        for (int i = 0; i < masses.Length; i++)
                        {
                            var m = masses[i];

                            var dx = p.x - m.center.x;
                            var dz = p.z - m.center.z;
                            var dh = Mathf.Sqrt(dx * dx + dz * dz);
                            var dv = Mathf.Abs(p.y - m.center.y);

                            var r = Mathf.Max(0.01f, m.radius);
                            var h = Mathf.Max(0.01f, m.heightInfluence);

                            var f = 1f - (dh * dh) / (r * r) - (dv * dv) / (h * h);
                            if (f > 0f)
                            {
                                // Soft edge falloff so blobs read as volumetric masses.
                                var edge = Mathf.Clamp01(f);
                                if (m.edgeFalloff > 0.01f)
                                {
                                    // Apply a gentle non-linear rolloff (not a real SDF, just shaping).
                                    edge = Mathf.SmoothStep(0f, 1f, edge);
                                }

                                d += edge * m.addStrength;
                            }
                        }

                        // Terraces / shelves: vertical band shaping + subtle (x,z) noise.
                        if (terraceStrength != 0f)
                        {
                            var t = (bandHeight <= 1e-6f) ? 0f : (py / bandHeight);
                            var frac = t - Mathf.Floor(t); // [0..1)
                            var terraceMask = 1f - Mathf.Abs(frac - 0.5f) * 2f; // peak at band center
                            terraceMask = Mathf.Clamp01(terraceMask);

                            var n = 0f;
                            if (terraceFreq > 0f && terraceAmp != 0f)
                            {
                                n = (Mathf.PerlinNoise(px * terraceFreq + terraceOx, pz * terraceFreq + terraceOz) - 0.5f) * terraceAmp;
                            }

                            d += terraceMask * terraceStrength + n;
                        }

                        // Overhang undercuts (void).
                        for (int i = 0; i < overhangs.Length; i++)
                        {
                            var o = overhangs[i];
                            if (p.y <= o.cutoffY)
                            {
                                var sdf = Sdf.Sphere(p, o.center, o.radius);
                                d = Mathf.Min(d, sdf);
                            }
                        }

                        // Floating islands (solid boost).
                        for (int i = 0; i < islands.Length; i++)
                        {
                            var isl = islands[i];
                            var sdf = Sdf.Sphere(p, isl.center, isl.radius);
                            if (sdf < 0f)
                            {
                                var depth = Mathf.Clamp01((-sdf) / Mathf.Max(0.01f, isl.radius)); // 0 at surface, 1 at center
                                d += isl.boost * depth;
                            }
                        }

                        if (Mathf.Abs(d - d0) > eps) affected++;
                        ctx.density.Set(x, y, z, d);
                    }
                }
            }

            // Recompute stats after composition and store.
            ctx.densityStats = ComputeStats(ctx.density.data);
            ctx.hasDensityStats = true;

            DebugLog.Log(ctx,
                $"ComposeMajorMasses AFTER stats: min={ctx.densityStats.min:0.###}, max={ctx.densityStats.max:0.###}, mean={ctx.densityStats.mean:0.###}, std={ctx.densityStats.std:0.###}, " +
                $"p01={ctx.densityStats.p01:0.###}, p50={ctx.densityStats.p50:0.###}, p99={ctx.densityStats.p99:0.###}");

            if (settings.exportBeforeAfterSlices)
            {
                DebugSlices.ExportDensitySlices(ctx, "m3_after_");
            }

            ctx.pendingStepCounters = new Dictionary<string, int>
            {
                { "majorMasses", masses.Length },
                { "overhangs", overhangs.Length },
                { "floatingIslands", islands.Length },
                { "affectedVoxels", affected },
            };

            ctx.pendingStepNotes = $"majorMasses={masses.Length}, overhangs={overhangs.Length}, floatingIslands={islands.Length}, affected={affected}";
        }

        private static MajorMass[] PlaceMajorMasses(WorldGenSettings s, System.Random rng, float maxX, float maxY, float maxZ)
        {
            var min = Mathf.Max(0, s.majorMassCountMin);
            var max = Mathf.Max(min, s.majorMassCountMax);
            var targetCount = rng.Next(min, max + 1);

            var tries = Mathf.Max(1, s.massPlacementMaxTries);
            var minSep = Mathf.Max(0f, s.massMinSeparation);
            var minSep2 = minSep * minSep;

            var list = new List<MajorMass>(targetCount);

            for (int attempt = 0; attempt < tries && list.Count < targetCount; attempt++)
            {
                var cx = RandRange(rng, 0f, maxX);
                var cz = RandRange(rng, 0f, maxZ);
                var cy = RandRange(rng, maxY * 0.35f, maxY * 0.65f);

                var ok = true;
                for (int i = 0; i < list.Count; i++)
                {
                    var c = list[i].center;
                    var dx = cx - c.x;
                    var dz = cz - c.z;
                    if (dx * dx + dz * dz < minSep2)
                    {
                        ok = false;
                        break;
                    }
                }

                if (!ok) continue;

                var radius = RandRange(rng, s.majorMassRadiusRange.x, s.majorMassRadiusRange.y);
                var h = RandRange(rng, s.majorMassHeightRange.x, s.majorMassHeightRange.y);

                // Make blobs "taller than wide" by biasing height upward a bit.
                h = Mathf.Max(h, radius * 0.85f);

                list.Add(new MajorMass
                {
                    center = new Vector3(cx, cy, cz),
                    radius = Mathf.Max(1f, radius),
                    heightInfluence = Mathf.Max(1f, h),
                    addStrength = 40f,
                    edgeFalloff = Mathf.Max(0f, s.majorMassEdgeFalloff),
                });
            }

            return list.ToArray();
        }

        private static Overhang[] PlaceOverhangs(WorldGenSettings s, System.Random rng, float maxX, float maxY, float maxZ)
        {
            var count = Mathf.Max(0, s.overhangCount);
            var list = new Overhang[count];

            for (int i = 0; i < count; i++)
            {
                var radius = RandRange(rng, s.overhangRadiusRange.x, s.overhangRadiusRange.y);
                radius = Mathf.Max(1f, radius);

                var cx = RandRange(rng, 0f, maxX);
                var cz = RandRange(rng, 0f, maxZ);
                var cutoffY = RandRange(rng, s.overhangHeightRange.x, s.overhangHeightRange.y);
                cutoffY = Mathf.Clamp(cutoffY, 0f, maxY);

                // Center the sphere slightly below the cutoff to bias an "undercut" feel.
                var cy = Mathf.Clamp(cutoffY - Mathf.Max(0f, s.overhangCarveThickness) * 0.5f, 0f, maxY);

                list[i] = new Overhang
                {
                    center = new Vector3(cx, cy, cz),
                    radius = radius,
                    cutoffY = cutoffY,
                };
            }

            return list;
        }

        private static FloatingIsland[] PlaceIslands(WorldGenSettings s, System.Random rng, float maxX, float maxY, float maxZ)
        {
            var count = Mathf.Max(0, s.floatingIslandCount);
            var list = new FloatingIsland[count];

            for (int i = 0; i < count; i++)
            {
                var radius = RandRange(rng, s.floatingIslandRadiusRange.x, s.floatingIslandRadiusRange.y);
                radius = Mathf.Max(1f, radius);

                var cx = RandRange(rng, 0f, maxX);
                var cz = RandRange(rng, 0f, maxZ);
                var cy = RandRange(rng, s.floatingIslandHeightRange.x, s.floatingIslandHeightRange.y);
                cy = Mathf.Clamp(cy, 0f, maxY);

                list[i] = new FloatingIsland
                {
                    center = new Vector3(cx, cy, cz),
                    radius = radius,
                    boost = s.floatingIslandDensityBoost,
                };
            }

            return list;
        }

        private static void LogPlacement(WorldContext ctx, WorldGenSettings s, MajorMass[] masses, Overhang[] overhangs, FloatingIsland[] islands)
        {
            DebugLog.Log(ctx, $"ComposeMajorMasses: masses={masses.Length} (target {s.majorMassCountMin}-{s.majorMassCountMax}), minSep={s.massMinSeparation:0.##}, tries={s.massPlacementMaxTries}");
            for (int i = 0; i < masses.Length; i++)
            {
                var m = masses[i];
                DebugLog.Log(ctx, $"  Mass[{i}]: center=({m.center.x:0.#},{m.center.y:0.#},{m.center.z:0.#}) r={m.radius:0.#} h={m.heightInfluence:0.#}");
            }

            DebugLog.Log(ctx, $"  Terraces: bands={Mathf.Max(1, s.terraceBands)}, strength={s.terraceStrength:0.##}, noiseFreq={s.terraceNoiseFreq:0.####}, noiseAmp={s.terraceNoiseAmp:0.##}");

            DebugLog.Log(ctx, $"  Overhangs: count={overhangs.Length}, rRange=({s.overhangRadiusRange.x:0.#},{s.overhangRadiusRange.y:0.#}), yRange=({s.overhangHeightRange.x:0.#},{s.overhangHeightRange.y:0.#}), thickness={s.overhangCarveThickness:0.#}");
            for (int i = 0; i < overhangs.Length; i++)
            {
                var o = overhangs[i];
                DebugLog.Log(ctx, $"    Overhang[{i}]: center=({o.center.x:0.#},{o.center.y:0.#},{o.center.z:0.#}) r={o.radius:0.#} cutoffY={o.cutoffY:0.#}");
            }

            DebugLog.Log(ctx, $"  FloatingIslands: count={islands.Length}, rRange=({s.floatingIslandRadiusRange.x:0.#},{s.floatingIslandRadiusRange.y:0.#}), yRange=({s.floatingIslandHeightRange.x:0.#},{s.floatingIslandHeightRange.y:0.#}), boost={s.floatingIslandDensityBoost:0.#}");
            for (int i = 0; i < islands.Length; i++)
            {
                var isl = islands[i];
                DebugLog.Log(ctx, $"    Island[{i}]: center=({isl.center.x:0.#},{isl.center.y:0.#},{isl.center.z:0.#}) r={isl.radius:0.#}");
            }
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


