using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Block;
using static Constants;

public class ChunkNoWater
{
    private ulong[] binaryBlocks = new ulong[3 * CHUNK_SIZE * CHUNK_SIZE];
    private byte[] idBlocks = new byte[CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE];

    public GameObject gameObject { get; }
    public Mesh mesh { get; }
    public MeshFilter meshFilter { get; }
    public MeshRenderer meshRenderer { get; }
    public MeshCollider meshCollider { get; }

    public ChunkNoWater(GameObject gameObject, Mesh mesh, MeshFilter meshFilter, MeshRenderer meshRenderer, MeshCollider meshCollider)
    {
        this.gameObject = gameObject;
        this.mesh = mesh;
        this.meshFilter = meshFilter;
        this.meshRenderer = meshRenderer;
        this.meshCollider = meshCollider;
    }

    public void SetBlock(int x, int y, int z, byte id)
    {
        idBlocks[(CHUNK_SIZE * CHUNK_SIZE * z) + (y * CHUNK_SIZE + x)] = id;
        binaryBlocks[(CHUNK_SIZE * CHUNK_SIZE * 0) + (z * CHUNK_SIZE + x)] |= 1ul << y;
        binaryBlocks[(CHUNK_SIZE * CHUNK_SIZE * 1) + (y * CHUNK_SIZE + z)] |= 1ul << x;
        binaryBlocks[(CHUNK_SIZE * CHUNK_SIZE * 2) + (y * CHUNK_SIZE + x)] |= 1ul << z;
    }

    public void SetBlocks(ulong[] binaryBlocks, byte[] idBlocks)
    {
        this.binaryBlocks = binaryBlocks;
        this.idBlocks = idBlocks;
    }

    public byte GetBlock(int x, int y, int z)
    {
        return idBlocks[(CHUNK_SIZE * CHUNK_SIZE * z) + (y * CHUNK_SIZE + x)];
    }

    public void UpdateMesh()
    {
        NativeList<Vector3> vertices = new NativeList<Vector3>(Allocator.Persistent);
        NativeList<int> triangles = new NativeList<int>(Allocator.Persistent);
        NativeList<Vector2> uvs = new NativeList<Vector2>(Allocator.Persistent);
        NativeList<Segment> segment = new NativeList<Segment>(Allocator.Persistent);

        NativeArray<ulong> bBlocks = new NativeArray<ulong>(binaryBlocks, Allocator.Persistent);
        NativeArray<byte> iBlocks = new NativeArray<byte>(idBlocks, Allocator.Persistent);

        MeshJob job = new MeshJob
        {
            BinaryBlocks = bBlocks,
            IdBlocks = iBlocks,
            BlockProperties = blockProperties,
            Vertices = vertices,
            Triangles = triangles,
            Uvs = uvs,
            Segment = segment,  
        };

        job.Schedule().Complete();

        bBlocks.Dispose();
        iBlocks.Dispose();

        // Material
        Material[] materials = new Material[segment.Length];

        for (int i = 0; i < segment.Length; i++)
            materials[i] = Materials[segment[i].materialId];

        mesh.subMeshCount = segment.Length;
        meshRenderer.materials = materials;

        // Vertices
        mesh.vertices = vertices.AsArray().ToArray();
        vertices.Dispose();

        // UV
        mesh.uv = uvs.AsArray().ToArray();
        uvs.Dispose();

        // Triangles
        for (int i = 0; i < segment.Length; i++)
            mesh.SetTriangles(new NativeSlice<int>(triangles.AsArray(), segment[i].start, segment[i].length).ToArray(), i);

        triangles.Dispose();
        segment.Dispose();

        // Mesh
        meshCollider.sharedMesh = mesh;
        mesh.RecalculateNormals();
    }

    //[BurstCompile]
    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    private struct MeshJob : IJob
    {
        [ReadOnly]
        public NativeArray<ulong> BinaryBlocks;

        [ReadOnly]
        public NativeArray<byte> IdBlocks;

        [ReadOnly]
        public UnsafeHashMap<byte, BlockProperties> BlockProperties;

        [WriteOnly]
        public NativeList<Vector3> Vertices;

        [WriteOnly]
        public NativeList<int> Triangles;

        [WriteOnly]
        public NativeList<Vector2> Uvs;

        [WriteOnly]
        public NativeList<Segment> Segment;

        public byte GetBlockID(int x, int y, int z)
        {
            return IdBlocks[(CHUNK_SIZE * CHUNK_SIZE * z) + (y * CHUNK_SIZE + x)];
        }

        public void Execute()
        {
            var data = new UnsafeHashMap<BlockData, NativeArray<ulong>>(32, Allocator.Temp);

            for (int axis = 0; axis < 3; axis++)
            {
                for (int z = 0; z < CHUNK_SIZE; z++)
                {
                    for (int x = 0; x < CHUNK_SIZE; x++)
                    {
                        ulong bits = BinaryBlocks[(CHUNK_SIZE * CHUNK_SIZE * axis) + (z * CHUNK_SIZE + x)];

                        if (bits == 0)
                            continue;

                        int axis2 = axis * 2;

                        // Right
                        ulong right = bits & ~(bits << 1);

                        while (right != 0)
                        {
                            int y = math.tzcnt(right);
                            right &= right - 1;

                            byte id = axis2 switch
                            {
                                0 or 1 => GetBlockID(x, y, z),
                                2 or 3 => GetBlockID(y, z, x),
                                _ => GetBlockID(x, z, y)
                            };

                            BlockData blockData = new BlockData(id, axis2, y);

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

                            byte id = axis2 switch
                            {
                                0 or 1 => GetBlockID(x, y, z),
                                2 or 3 => GetBlockID(y, z, x),
                                _ => GetBlockID(x, z, y)
                            };

                            BlockData blockData = new BlockData(id, axis2 + 1, y);

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

            var triangles = new UnsafeHashMap<byte, NativeList<int>>(16, Allocator.Temp);
            int verticesLength = 0;


            foreach (var item in data)
            {
                BlockData blockData = item.Key;
                NativeArray<ulong> xz = item.Value;

                byte id = blockData.id;
                int axis = blockData.axis;
                int y = blockData.y;

                byte materialId;

                if (axis == 0)
                    materialId = BlockProperties[id].bottom;
                else if (axis == 1)
                    materialId = BlockProperties[id].top;
                else
                    materialId = BlockProperties[id].side;

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

                        Vertices.AddRange(verticesPosition);
                        verticesPosition.Dispose();

                        Uvs.AddRange(uvsPosition);
                        uvsPosition.Dispose();

                        if (!triangles.ContainsKey(materialId))
                            triangles.Add(materialId, new NativeList<int>(Allocator.Temp));

                        NativeArray<int> trianglesPosition = new NativeArray<int>(6, Allocator.Temp);

                        trianglesPosition[0] = verticesLength;
                        trianglesPosition[1] = verticesLength + 1;
                        trianglesPosition[2] = verticesLength + 2;

                        trianglesPosition[3] = verticesLength;
                        trianglesPosition[4] = verticesLength + 2;
                        trianglesPosition[5] = verticesLength + 3;

                        triangles[materialId].AddRange(trianglesPosition);
                        trianglesPosition.Dispose();

                        verticesLength += 4;
                    }
                }

                xz.Dispose();
            }

            data.Dispose();

            int prev = 0;

            foreach (var item in triangles)
            {
                NativeList<int> list = item.Value;
                byte materialId = item.Key;

                Triangles.AddRange(list.AsArray());
                Segment.Add(new Segment(materialId, prev, list.Length));

                prev += list.Length;
            }
        }
    }

    public struct BlockData : IEquatable<BlockData>
    {
        public readonly byte id;
        public readonly int axis;
        public readonly int y;

        public BlockData(byte id, int axis, int y)
        {
            this.id = id;
            this.axis = axis;
            this.y = y;
        }

        public bool Equals(BlockData other)
        {
            return id == other.id && axis == other.axis && y == other.y;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + id.GetHashCode();
                hash = hash * 23 + axis.GetHashCode();
                hash = hash * 23 + y.GetHashCode();
                return hash;
            }
        }
    }

    public struct Segment
    {
        public readonly byte materialId;
        public readonly int start;
        public readonly int length;

        public Segment(byte materialId, int start, int length)
        {
            this.materialId = materialId;
            this.start = start;
            this.length = length;
        }
    }
}