namespace WorldGen.Core
{
    public interface IGenerationStep
    {
        string StepName { get; }
        void Generate(WorldGenSettings settings, WorldContext ctx);
    }
}


