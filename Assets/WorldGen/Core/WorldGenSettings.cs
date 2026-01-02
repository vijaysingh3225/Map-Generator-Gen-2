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
        public bool exportEndOfRunDensitySlices = true;

        [Header("Carving (SDF)")]
        public bool exportBeforeAfterSlices = true;
        public float carveStrength = 1.0f;
        public int tunnelCount = 1;
        public float tunnelRadius = 10f;
        public int cavePocketCount = 3;
        public Vector2 cavePocketRadiusRange = new Vector2(6, 18);
        public float archRadius = 18f;
        public float archThickness = 12f;
        public float archHeight = 22f;
        public int carveSeedOffset = 1337;

        [Header("Milestone 3 - Major Mass Composition")]
        public int majorMassCountMin = 2;
        public int majorMassCountMax = 4;
        public Vector2 majorMassRadiusRange = new Vector2(45f, 80f); // world units
        public Vector2 majorMassHeightRange = new Vector2(40f, 90f); // world units
        public float majorMassEdgeFalloff = 20f;
        public int massPlacementMaxTries = 64;
        public float massMinSeparation = 90f; // world units (XZ plane)

        [Header("Terraces / Shelves")]
        public int terraceBands = 6;
        public float terraceStrength = 10f;
        public float terraceNoiseFreq = 0.01f;
        public float terraceNoiseAmp = 6f;

        [Header("Overhangs")]
        public int overhangCount = 3;
        public Vector2 overhangRadiusRange = new Vector2(25f, 55f);
        public Vector2 overhangHeightRange = new Vector2(35f, 75f);
        public float overhangCarveThickness = 18f;

        [Header("Floating Islands")]
        public int floatingIslandCount = 10;
        public Vector2 floatingIslandRadiusRange = new Vector2(10f, 28f);
        public Vector2 floatingIslandHeightRange = new Vector2(70f, 120f);
        public float floatingIslandDensityBoost = 25f;

        public int composeSeedOffset = 9001;

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


