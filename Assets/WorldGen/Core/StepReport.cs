using System.Collections.Generic;

namespace WorldGen.Core
{
    [System.Serializable]
    public sealed class StepReport
    {
        public string stepName;
        public double ms;
        public string notes;

        // Not Unity-serializable; intended for report.txt only (we'll make it structured later if needed).
        public Dictionary<string, int> counters;

        public StepReport(string stepName, double ms, string notes = null)
        {
            this.stepName = stepName;
            this.ms = ms;
            this.notes = notes;
            counters = null;
        }
    }
}


