using System;
using System.IO;
using System.Text;
using UnityEngine;
using WorldGen.Core;

namespace WorldGen.Debug
{
    public static class DebugPaths
    {
        public static string GetProjectRoot()
        {
            return Directory.GetParent(Application.dataPath).FullName;
        }

        public static string ResolveOutputRootAbsolute(WorldGenSettings settings)
        {
            var rootName = settings != null && !string.IsNullOrWhiteSpace(settings.outputRoot)
                ? settings.outputRoot.Trim()
                : "WorldGenOutput";

            return Path.Combine(GetProjectRoot(), MakeSafePathSegment(rootName));
        }

        public static string GetRunId(WorldGenSettings settings)
        {
            if (settings == null) return $"seed0";

            var seed = settings.seed;
            return settings.runIdMode == RunIdMode.SeedOnly
                ? $"seed{seed}"
                : $"{DateTime.Now:yyyyMMdd_HHmmss}_seed{seed}";
        }

        public static string ResolveRunOutputPath(WorldGenSettings settings)
        {
            var rootAbs = ResolveOutputRootAbsolute(settings);
            var runId = GetRunId(settings);
            return Path.Combine(rootAbs, runId);
        }

        public static void EnsureDirectory(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir)) return;
            Directory.CreateDirectory(dir);
        }

        public static string MakeSafeFilename(string name, string fallback = "file")
        {
            if (string.IsNullOrWhiteSpace(name)) name = fallback;

            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            foreach (var c in name)
            {
                sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            }

            var cleaned = sb.ToString().Trim();
            return string.IsNullOrWhiteSpace(cleaned) ? fallback : cleaned;
        }

        public static string MakeSafePathSegment(string segment, string fallback = "folder")
        {
            // Reuse filename sanitization rules for a single path segment.
            return MakeSafeFilename(segment, fallback);
        }
    }
}


