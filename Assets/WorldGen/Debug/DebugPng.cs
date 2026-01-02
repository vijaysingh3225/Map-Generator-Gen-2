using System.IO;
using UnityEngine;

namespace WorldGen.Debug
{
    public static class DebugPng
    {
        public static void WritePng(Texture2D texture, string absolutePath)
        {
            if (texture == null) throw new System.ArgumentNullException(nameof(texture));
            if (string.IsNullOrWhiteSpace(absolutePath)) throw new System.ArgumentException("Path is null/empty", nameof(absolutePath));

            var bytes = texture.EncodeToPNG();
            var dir = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllBytes(absolutePath, bytes);
        }

        public static void ExportGrayscaleFloatSlice(string absolutePath, float[] values, int width, int height, float vmin, float vmax)
        {
            if (string.IsNullOrWhiteSpace(absolutePath)) throw new System.ArgumentException("Path is null/empty", nameof(absolutePath));
            if (values == null) throw new System.ArgumentNullException(nameof(values));
            if (values.Length != width * height) throw new System.ArgumentException("values must be width*height", nameof(values));
            if (width <= 0) throw new System.ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new System.ArgumentOutOfRangeException(nameof(height));

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            try
            {
                var denom = vmax - vmin;
                var useMid = Mathf.Abs(denom) < 1e-9f;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var v = values[x + width * y];
                        var t = useMid ? 0.5f : Mathf.InverseLerp(vmin, vmax, v);
                        t = Mathf.Clamp01(t);
                        tex.SetPixel(x, y, new Color(t, t, t, 1f));
                    }
                }

                tex.Apply(false, false);
                WritePng(tex, absolutePath);
            }
            finally
            {
                if (Application.isPlaying) Object.Destroy(tex);
                else Object.DestroyImmediate(tex);
            }
        }
    }
}


