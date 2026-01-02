namespace WorldGen.Core
{
    [System.Serializable]
    public struct DensityFieldStats
    {
        public int count;
        public float min;
        public float max;
        public double mean;
        public double std;

        public float p01;
        public float p10;
        public float p50;
        public float p90;
        public float p99;

        // Recommended visualization window for slice exports (typically p01..p99).
        public float displayMin;
        public float displayMax;
    }
}


