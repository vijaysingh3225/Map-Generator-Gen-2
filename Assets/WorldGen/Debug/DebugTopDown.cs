using System;
using System.IO;
using WorldGen.Core;
using UnityEngine;

namespace WorldGen.Debug
{
    public static class DebugTopDown
    {
        /// <summary>
        /// For each (x,z), find the highest y that is solid (density > 0). Outputs height in world units.
        /// </summary>
        public static float[] ComputeSurfaceHeightWorld(DensityField3D density)
        {
            if (density == null) throw new ArgumentNullException(nameof(density));

            var out2D = new float[density.sizeX * density.sizeZ];
            var vs = density.voxelSize;

            for (int z = 0; z < density.sizeZ; z++)
            {
                for (int x = 0; x < density.sizeX; x++)
                {
                    float height = 0f;
                    for (int y = density.sizeY - 1; y >= 0; y--)
                    {
                        if (density.Get(x, y, z) > 0f)
                        {
                            height = y * vs;
                            break;
                        }
                    }
                    out2D[x + density.sizeX * z] = height;
                }
            }

            return out2D;
        }

        /// <summary>
        /// For each (x,z), count solid voxels along y and normalize to 0..1 by sizeY.
        /// </summary>
        public static float[] ComputeSolidOccupancy01(DensityField3D density)
        {
            if (density == null) throw new ArgumentNullException(nameof(density));

            var out2D = new float[density.sizeX * density.sizeZ];
            var inv = 1f / Mathf.Max(1, density.sizeY);

            for (int z = 0; z < density.sizeZ; z++)
            {
                for (int x = 0; x < density.sizeX; x++)
                {
                    int count = 0;
                    for (int y = 0; y < density.sizeY; y++)
                    {
                        if (density.Get(x, y, z) > 0f) count++;
                    }
                    out2D[x + density.sizeX * z] = count * inv;
                }
            }

            return out2D;
        }

        public static void ExportSurfaceAndOccupancy(WorldContext ctx, string prefix)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (ctx.settings == null) throw new ArgumentNullException(nameof(ctx.settings));
            if (ctx.density == null) throw new InvalidOperationException("ctx.density is null");
            if (!ctx.settings.exportTopDownMaps) return;

            prefix ??= string.Empty;
            Directory.CreateDirectory(ctx.outputPath);

            var maxYWorld = (ctx.density.sizeY - 1) * ctx.density.voxelSize;
            maxYWorld = Mathf.Max(0.0001f, maxYWorld);

            // Surface height (world units -> grayscale)
            {
                var heightWorld = ComputeSurfaceHeightWorld(ctx.density);
                var file = $"{prefix}surface_height.png";
                var path = Path.Combine(ctx.outputPath, file);
                DebugPng.ExportGrayscaleFloatSlice(path, heightWorld, ctx.density.sizeX, ctx.density.sizeZ, 0f, maxYWorld);
                ctx.densitySliceFiles.Add(file);
            }

            // Solid occupancy (0..1)
            {
                var occ01 = ComputeSolidOccupancy01(ctx.density);
                var file = $"{prefix}solid_occupancy.png";
                var path = Path.Combine(ctx.outputPath, file);
                DebugPng.ExportGrayscaleFloatSlice(path, occ01, ctx.density.sizeX, ctx.density.sizeZ, 0f, 1f);
                ctx.densitySliceFiles.Add(file);
            }
        }
    }
}


