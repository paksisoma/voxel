using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Constants;

public class Cloud
{
    private ulong[] blocks = new ulong[3 * CHUNK_SIZE * CHUNK_SIZE];

    public GameObject gameObject { get; }
    public Mesh mesh { get; }
    public MeshFilter meshFilter { get; }
    public MeshRenderer meshRenderer { get; }
    public int2 position { get; set; }

    public Cloud(GameObject gameObject, Mesh mesh, MeshFilter meshFilter, MeshRenderer meshRenderer, int2 position)
    {
        this.gameObject = gameObject;
        this.mesh = mesh;
        this.meshFilter = meshFilter;
        this.meshRenderer = meshRenderer;
        this.position = position;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddBlock(int x, int y, int z)
    {
        blocks[(CHUNK_SIZE * CHUNK_SIZE * 0) + (z * CHUNK_SIZE + x)] |= 1ul << y;
        blocks[(CHUNK_SIZE * CHUNK_SIZE * 1) + (y * CHUNK_SIZE + z)] |= 1ul << x;
        blocks[(CHUNK_SIZE * CHUNK_SIZE * 2) + (y * CHUNK_SIZE + x)] |= 1ul << z;
    }

    public void UpdateMap()
    {
        blocks = new ulong[3 * CHUNK_SIZE * CHUNK_SIZE];
        NativeArray<float> map = new NativeArray<float>(CHUNK_SIZE * CHUNK_SIZE, Allocator.Persistent);

        NoiseJob job = new NoiseJob
        {
            Position = position,
            Map = map
        };

        job.Schedule(CHUNK_SIZE * CHUNK_SIZE, 64).Complete();

        for (int i = 0; i < map.Length; i++)
        {
            int x = i / CHUNK_SIZE;
            int z = i % CHUNK_SIZE;

            if (map[i] > 0.5f)
            {
                int round = Mathf.RoundToInt(map[i] * 10);

                for (int y = 0; y < round; y++)
                    AddBlock(x, y + 10, z);
            }
        }

        map.Dispose();
    }

    public void UpdateMesh()
    {
        NativeList<Vector3> vertices = new NativeList<Vector3>(Allocator.Persistent);
        NativeList<int> triangles = new NativeList<int>(Allocator.Persistent);
        NativeList<Vector2> uvs = new NativeList<Vector2>(Allocator.Persistent);

        NativeArray<ulong> nativeBlocks = new NativeArray<ulong>(blocks, Allocator.Persistent);

        MeshJob job = new MeshJob
        {
            Blocks = nativeBlocks,
            Vertices = vertices,
            Triangles = triangles,
            Uvs = uvs,
        };

        job.Schedule().Complete();

        nativeBlocks.Dispose();

        mesh.Clear();

        // Vertices
        mesh.vertices = vertices.AsArray().ToArray();
        vertices.Dispose();

        // UV
        mesh.uv = uvs.AsArray().ToArray();
        uvs.Dispose();

        // Triangles
        mesh.triangles = triangles.AsArray().ToArray();
        triangles.Dispose();

        // Mesh
        mesh.RecalculateNormals();
    }

    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    private struct MeshJob : IJob
    {
        [ReadOnly]
        public NativeArray<ulong> Blocks;

        [WriteOnly]
        public NativeList<Vector3> Vertices;

        [WriteOnly]
        public NativeList<int> Triangles;

        [WriteOnly]
        public NativeList<Vector2> Uvs;

        public void Execute()
        {
            var data = new UnsafeHashMap<BlockData, NativeArray<ulong>>(32, Allocator.Temp);

            for (int axis = 0; axis < 3; axis++)
            {
                for (int z = 0; z < CHUNK_SIZE; z++)
                {
                    for (int x = 0; x < CHUNK_SIZE; x++)
                    {
                        ulong bits = Blocks[(CHUNK_SIZE * CHUNK_SIZE * axis) + (z * CHUNK_SIZE + x)];

                        int axis2 = axis * 2;

                        // Right
                        ulong right = bits & ~(bits << 1);

                        while (right != 0)
                        {
                            int y = math.tzcnt(right);
                            right &= right - 1;

                            BlockData blockData = new BlockData(axis2, y);

                            if (!data.TryGetValue(blockData, out var array))
                            {
                                array = new NativeArray<ulong>(CHUNK_SIZE, Allocator.Temp);
                                data.TryAdd(blockData, array);
                            }

                            array[x] |= 1ul << z;
                        }

                        // Left
                        ulong left = bits & ~(bits >> 1);

                        while (left != 0)
                        {
                            int y = math.tzcnt(left);
                            left &= left - 1;

                            BlockData blockData = new BlockData(axis2 + 1, y);

                            if (!data.TryGetValue(blockData, out var array))
                            {
                                array = new NativeArray<ulong>(CHUNK_SIZE, Allocator.Temp);
                                data.TryAdd(blockData, array);
                            }

                            array[x] |= 1ul << z;
                        }
                    }
                }
            }

            int verticesLength = 0;

            foreach (var item in data)
            {
                BlockData blockData = item.Key;
                NativeArray<ulong> xz = item.Value;

                int axis = blockData.axis;
                int y = blockData.y;

                if (y <= 0 || y > CHUNK_SIZE - 2)
                    continue;

                for (int i = 1; i < xz.Length - 1; i++)
                {
                    xz[i] = (xz[i] & ~1ul) & ~(1ul << CHUNK_SIZE - 1); // Remove padding

                    while (xz[i] != 0)
                    {
                        int trailingZeros = math.tzcnt(xz[i]);
                        int trailingOnes = math.tzcnt(~xz[i] >> trailingZeros);
                        ulong mask = ((1ul << trailingOnes) - 1ul) << trailingZeros;

                        int height = 1;

                        for (int j = i + 1; j < xz.Length - 1 && (xz[j] & mask) == mask; j++)
                        {
                            xz[j] = (xz[j] & ~1ul) & ~(1ul << CHUNK_SIZE - 1); // Remove padding

                            xz[j] ^= mask;
                            height++;
                        }

                        xz[i] ^= mask;

                        NativeArray<Vector3> verticesPosition = new NativeArray<Vector3>(4, Allocator.Temp);
                        NativeArray<Vector2> uvsPosition = new NativeArray<Vector2>(4, Allocator.Temp);

                        switch (axis)
                        {
                            case 0:
                                verticesPosition[0] = new Vector3(i, y, trailingZeros);
                                verticesPosition[1] = new Vector3(i + height, y, trailingZeros);
                                verticesPosition[2] = new Vector3(i + height, y, trailingZeros + trailingOnes);
                                verticesPosition[3] = new Vector3(i, y, trailingZeros + trailingOnes);

                                uvsPosition[0] = new Vector2(height, 0);
                                uvsPosition[1] = new Vector2(0, 0);
                                uvsPosition[2] = new Vector2(0, trailingOnes);
                                uvsPosition[3] = new Vector2(height, trailingOnes);
                                break;
                            case 1:
                                verticesPosition[0] = new Vector3(i + height, y + 1, trailingZeros);
                                verticesPosition[1] = new Vector3(i, y + 1, trailingZeros);
                                verticesPosition[2] = new Vector3(i, y + 1, trailingZeros + trailingOnes);
                                verticesPosition[3] = new Vector3(i + height, y + 1, trailingZeros + trailingOnes);

                                uvsPosition[0] = new Vector2(height, 0);
                                uvsPosition[1] = new Vector2(0, 0);
                                uvsPosition[2] = new Vector2(0, trailingOnes);
                                uvsPosition[3] = new Vector2(height, trailingOnes);
                                break;
                            case 2:
                                verticesPosition[0] = new Vector3(y, trailingZeros, i);
                                verticesPosition[1] = new Vector3(y, trailingZeros, i + height);
                                verticesPosition[2] = new Vector3(y, trailingZeros + trailingOnes, i + height);
                                verticesPosition[3] = new Vector3(y, trailingZeros + trailingOnes, i);

                                uvsPosition[0] = new Vector2(0, 0);
                                uvsPosition[1] = new Vector2(height, 0);
                                uvsPosition[2] = new Vector2(height, trailingOnes);
                                uvsPosition[3] = new Vector2(0, trailingOnes);
                                break;
                            case 3:
                                verticesPosition[0] = new Vector3(y + 1, trailingZeros, i + height);
                                verticesPosition[1] = new Vector3(y + 1, trailingZeros, i);
                                verticesPosition[2] = new Vector3(y + 1, trailingZeros + trailingOnes, i);
                                verticesPosition[3] = new Vector3(y + 1, trailingZeros + trailingOnes, i + height);

                                uvsPosition[0] = new Vector2(0, 0);
                                uvsPosition[1] = new Vector2(height, 0);
                                uvsPosition[2] = new Vector2(height, trailingOnes);
                                uvsPosition[3] = new Vector2(0, trailingOnes);
                                break;
                            case 4:
                                verticesPosition[0] = new Vector3(i + height, trailingZeros, y);
                                verticesPosition[1] = new Vector3(i, trailingZeros, y);
                                verticesPosition[2] = new Vector3(i, trailingZeros + trailingOnes, y);
                                verticesPosition[3] = new Vector3(i + height, trailingZeros + trailingOnes, y);

                                uvsPosition[0] = new Vector2(height, 0);
                                uvsPosition[1] = new Vector2(0, 0);
                                uvsPosition[2] = new Vector2(0, trailingOnes);
                                uvsPosition[3] = new Vector2(height, trailingOnes);
                                break;
                            default:
                                verticesPosition[0] = new Vector3(i, trailingZeros, y + 1);
                                verticesPosition[1] = new Vector3(i + height, trailingZeros, y + 1);
                                verticesPosition[2] = new Vector3(i + height, trailingZeros + trailingOnes, y + 1);
                                verticesPosition[3] = new Vector3(i, trailingZeros + trailingOnes, y + 1);

                                uvsPosition[0] = new Vector2(height, 0);
                                uvsPosition[1] = new Vector2(0, 0);
                                uvsPosition[2] = new Vector2(0, trailingOnes);
                                uvsPosition[3] = new Vector2(height, trailingOnes);
                                break;
                        }

                        // Vertices
                        Vertices.AddRange(verticesPosition);
                        verticesPosition.Dispose();

                        // Uvs
                        Uvs.AddRange(uvsPosition);
                        uvsPosition.Dispose();

                        // Triangles
                        NativeArray<int> trianglesPosition = new NativeArray<int>(6, Allocator.Temp);

                        trianglesPosition[0] = verticesLength;
                        trianglesPosition[1] = verticesLength + 1;
                        trianglesPosition[2] = verticesLength + 2;

                        trianglesPosition[3] = verticesLength;
                        trianglesPosition[4] = verticesLength + 2;
                        trianglesPosition[5] = verticesLength + 3;

                        Triangles.AddRange(trianglesPosition);
                        trianglesPosition.Dispose();

                        verticesLength += 4;
                    }
                }

                xz.Dispose();
            }

            data.Dispose();
        }
    }

    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    private struct NoiseJob : IJobParallelFor
    {
        [ReadOnly]
        public int2 Position;

        [WriteOnly]
        public NativeArray<float> Map;

        public void Execute(int i)
        {
            float2 position = new float2(i / CHUNK_SIZE + Position.x, i % CHUNK_SIZE + Position.y);

            float a = 1f * Noise(position, 500) +
            0.5f * Noise(position, 50);

            a /= 1f + 0.5f;
            a = math.pow(a, 4f);

            Map[i] = a;
        }

        public float Noise(float2 position, float frequency)
        {
            position /= frequency;
            return (noise.snoise(position) + 1) / 2;
        }
    }

    private struct BlockData : IEquatable<BlockData>
    {
        public readonly int axis;
        public readonly int y;

        public BlockData(int axis, int y)
        {
            this.axis = axis;
            this.y = y;
        }

        public bool Equals(BlockData other)
        {
            return axis == other.axis && y == other.y;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + axis.GetHashCode();
                hash = hash * 23 + y.GetHashCode();
                return hash;
            }
        }
    }
}