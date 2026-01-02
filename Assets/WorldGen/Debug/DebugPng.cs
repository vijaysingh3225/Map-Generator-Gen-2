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
    }
}


