using UnityEngine;

namespace WorldGen.Core
{
    [CreateAssetMenu(fileName = "WorldGenSettings", menuName = "WorldGen/WorldGen Settings", order = 10)]
    public sealed class WorldGenSettings : ScriptableObject
    {
        [Header("Determinism")]
        public int seed = 12345;

        [Header("World")]
        public Vector3 worldSize = new Vector3(256, 128, 256);
        public float voxelSize = 2f;

        [Header("Density Grid")]
        public Vector3Int gridSize = new Vector3Int(128, 64, 128);

        [Header("Density Debug Slices")]
        public int[] debugSliceYs = { 0, 16, 32, 48, 63 };
        public int[] debugSliceXs = { 0, 64, 127 };
        public int[] debugSliceZs = { 0, 64, 127 };
        public bool exportDensitySlices = true;

        [Header("Debug Output")]
        [Tooltip("Created under the project root (next to Assets) when running in the editor.")]
        public string outputRoot = "WorldGenOutput";
        public RunIdMode runIdMode = RunIdMode.Timestamp;
        public bool logToConsole = true;
        public bool exportTestPng = true;

        public string GetSettingsSummary()
        {
            return $"seed={seed}, worldSize={worldSize}, voxelSize={voxelSize}, gridSize={gridSize}, outputRoot='{outputRoot}', runIdMode={runIdMode}";
        }
    }
}


