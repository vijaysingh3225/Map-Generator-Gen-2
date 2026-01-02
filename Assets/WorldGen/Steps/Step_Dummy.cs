using UnityEngine;
using WorldGen.Core;
using WorldGen.Debug;

namespace WorldGen.Steps
{
    [CreateAssetMenu(fileName = "Step_Dummy", menuName = "WorldGen/Steps/Dummy", order = 10)]
    public sealed class Step_Dummy : GenerationStepAsset
    {
        public override string StepName => "Dummy";

        public override void Generate(WorldGenSettings settings, WorldContext ctx)
        {
            DebugLog.Log(ctx, "Dummy step ran.");
            ctx.blackboard["dummy"] = "ok";
        }
    }
}


