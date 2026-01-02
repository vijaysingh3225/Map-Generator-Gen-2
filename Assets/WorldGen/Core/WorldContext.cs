using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace WorldGen.Core
{
    public sealed class WorldContext
    {
        public int seed;
        public System.Random rng;
        public WorldGenSettings settings;
        public string outputPath;

        public DensityField3D density;
        public bool hasDensityStats;
        public DensityFieldStats densityStats;
        public List<string> densitySliceFiles = new List<string>();

        public Dictionary<string, object> blackboard = new Dictionary<string, object>();
        public List<StepReport> stepReports = new List<StepReport>();
        public StringBuilder runLog = new StringBuilder(1024);

        public GameObject runtimeRoot;

        public WorldContext(WorldGenSettings settings, int seed, string outputPath)
        {
            this.settings = settings;
            this.seed = seed;
            this.outputPath = outputPath;
            rng = new System.Random(seed);
        }
    }
}


