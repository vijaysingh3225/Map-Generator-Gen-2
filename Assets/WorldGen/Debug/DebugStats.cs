using System;
using System.Collections.Generic;

namespace WorldGen.Debug
{
    public static class DebugStats
    {
        public struct Summary
        {
            public int count;
            public float min;
            public float max;
            public double mean;
            public double std;
        }

        public static Summary Summarize(float[] values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (values.Length == 0)
            {
                return new Summary { count = 0, min = float.NaN, max = float.NaN, mean = double.NaN, std = double.NaN };
            }

            var min = values[0];
            var max = values[0];
            double sum = 0;
            for (int i = 0; i < values.Length; i++)
            {
                var v = values[i];
                if (v < min) min = v;
                if (v > max) max = v;
                sum += v;
            }

            var mean = sum / values.Length;
            double varSum = 0;
            for (int i = 0; i < values.Length; i++)
            {
                var d = values[i] - mean;
                varSum += d * d;
            }

            var variance = varSum / values.Length;
            var std = Math.Sqrt(variance);

            return new Summary { count = values.Length, min = min, max = max, mean = mean, std = std };
        }

        /// <summary>
        /// Percentile using linear interpolation between sorted samples.
        /// p in [0,1].
        /// </summary>
        public static float Percentile(float[] values, float p)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (values.Length == 0) return float.NaN;
            if (p <= 0) return Min(values);
            if (p >= 1) return Max(values);

            // Copy to avoid mutating caller.
            var copy = new float[values.Length];
            Array.Copy(values, copy, values.Length);
            Array.Sort(copy);

            var idx = p * (copy.Length - 1);
            var lo = (int)Math.Floor(idx);
            var hi = (int)Math.Ceiling(idx);
            if (lo == hi) return copy[lo];

            var t = (float)(idx - lo);
            return copy[lo] * (1f - t) + copy[hi] * t;
        }

        public static Dictionary<string, float> Percentiles(float[] values, params float[] ps)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (ps == null) throw new ArgumentNullException(nameof(ps));

            var result = new Dictionary<string, float>();
            foreach (var p in ps)
            {
                var key = $"p{(p * 100f):0.#}";
                result[key] = Percentile(values, p);
            }
            return result;
        }

        private static float Min(float[] values)
        {
            var m = values[0];
            for (int i = 1; i < values.Length; i++) if (values[i] < m) m = values[i];
            return m;
        }

        private static float Max(float[] values)
        {
            var m = values[0];
            for (int i = 1; i < values.Length; i++) if (values[i] > m) m = values[i];
            return m;
        }
    }
}


