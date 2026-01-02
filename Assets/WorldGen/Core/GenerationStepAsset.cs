using UnityEngine;

namespace WorldGen.Core
{
    public abstract class GenerationStepAsset : ScriptableObject
    {
        public abstract string StepName { get; }
        public abstract void Generate(WorldGenSettings settings, WorldContext ctx);
    }
}


