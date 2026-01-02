using UnityEngine;

namespace WorldGen.Core
{
    public static class Sdf
    {
        public static float Sphere(Vector3 p, Vector3 center, float radius)
        {
            return (p - center).magnitude - radius;
        }

        // Signed distance to axis-aligned box.
        public static float Box(Vector3 p, Vector3 center, Vector3 halfExtents)
        {
            var q = Abs(p - center) - halfExtents;
            var outside = new Vector3(
                Mathf.Max(q.x, 0f),
                Mathf.Max(q.y, 0f),
                Mathf.Max(q.z, 0f)
            ).magnitude;

            var inside = Mathf.Min(Mathf.Max(q.x, Mathf.Max(q.y, q.z)), 0f);
            return outside + inside;
        }

        // Signed distance to capsule around segment AB.
        public static float Capsule(Vector3 p, Vector3 a, Vector3 b, float radius)
        {
            var pa = p - a;
            var ba = b - a;
            var denom = Vector3.Dot(ba, ba);
            var h = denom <= 1e-8f ? 0f : Mathf.Clamp01(Vector3.Dot(pa, ba) / denom);
            return (pa - ba * h).magnitude - radius;
        }

        // Optional smooth union: k > 0; smaller k = sharper transition.
        public static float SmoothMin(float a, float b, float k)
        {
            var h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
            return Mathf.Lerp(b, a, h) - k * h * (1f - h);
        }

        private static Vector3 Abs(Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }
    }
}


