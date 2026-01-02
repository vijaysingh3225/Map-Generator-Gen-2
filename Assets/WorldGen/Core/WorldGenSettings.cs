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

        [Header("Debug Output")]
        [Tooltip("Created under the project root (next to Assets) when running in the editor.")]
        public string outputRoot = "WorldGenOutput";
        public RunIdMode runIdMode = RunIdMode.Timestamp;
        public bool logToConsole = true;
        public bool exportTestPng = true;

        public string GetSettingsSummary()
        {
            return $"seed={seed}, worldSize={worldSize}, voxelSize={voxelSize}, outputRoot='{outputRoot}', runIdMode={runIdMode}";
        }
    }
}


