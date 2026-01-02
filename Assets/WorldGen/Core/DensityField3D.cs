using System;

namespace WorldGen.Core
{
    public sealed class DensityField3D
    {
        public readonly int sizeX;
        public readonly int sizeY;
        public readonly int sizeZ;
        public readonly float voxelSize;

        public readonly float[] data;

        public int Count => data.Length;

        public DensityField3D(int sizeX, int sizeY, int sizeZ, float voxelSize)
        {
            if (sizeX <= 0) throw new ArgumentOutOfRangeException(nameof(sizeX));
            if (sizeY <= 0) throw new ArgumentOutOfRangeException(nameof(sizeY));
            if (sizeZ <= 0) throw new ArgumentOutOfRangeException(nameof(sizeZ));
            if (voxelSize <= 0) throw new ArgumentOutOfRangeException(nameof(voxelSize));

            this.sizeX = sizeX;
            this.sizeY = sizeY;
            this.sizeZ = sizeZ;
            this.voxelSize = voxelSize;

            data = new float[sizeX * sizeY * sizeZ];
        }

        public int Index(int x, int y, int z)
        {
            return x + sizeX * (y + sizeY * z);
        }

        public float Get(int x, int y, int z)
        {
            return data[Index(x, y, z)];
        }

        public void Set(int x, int y, int z, float val)
        {
            data[Index(x, y, z)] = val;
        }

        public void Fill(Func<int, int, int, float> fn)
        {
            if (fn == null) throw new ArgumentNullException(nameof(fn));

            for (int z = 0; z < sizeZ; z++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int x = 0; x < sizeX; x++)
                    {
                        data[Index(x, y, z)] = fn(x, y, z);
                    }
                }
            }
        }

        // sizeX * sizeZ
        public void GetSliceXZ(int y, float[] out2D)
        {
            if (out2D == null) throw new ArgumentNullException(nameof(out2D));
            if (out2D.Length != sizeX * sizeZ) throw new ArgumentException("out2D must be sizeX*sizeZ", nameof(out2D));
            if (y < 0 || y >= sizeY) throw new ArgumentOutOfRangeException(nameof(y));

            for (int z = 0; z < sizeZ; z++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    out2D[x + sizeX * z] = data[Index(x, y, z)];
                }
            }
        }

        // sizeX * sizeY
        public void GetSliceXY(int z, float[] out2D)
        {
            if (out2D == null) throw new ArgumentNullException(nameof(out2D));
            if (out2D.Length != sizeX * sizeY) throw new ArgumentException("out2D must be sizeX*sizeY", nameof(out2D));
            if (z < 0 || z >= sizeZ) throw new ArgumentOutOfRangeException(nameof(z));

            for (int y = 0; y < sizeY; y++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    out2D[x + sizeX * y] = data[Index(x, y, z)];
                }
            }
        }

        // sizeY * sizeZ
        public void GetSliceYZ(int x, float[] out2D)
        {
            if (out2D == null) throw new ArgumentNullException(nameof(out2D));
            if (out2D.Length != sizeY * sizeZ) throw new ArgumentException("out2D must be sizeY*sizeZ", nameof(out2D));
            if (x < 0 || x >= sizeX) throw new ArgumentOutOfRangeException(nameof(x));

            for (int z = 0; z < sizeZ; z++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    out2D[y + sizeY * z] = data[Index(x, y, z)];
                }
            }
        }
    }
}


