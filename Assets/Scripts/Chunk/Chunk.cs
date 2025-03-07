using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Blocks;
using static Constants;

public class Chunk
{
    private ulong[] solidBlocks = new ulong[3 * CHUNK_SIZE * CHUNK_SIZE];
    private ulong[] waterBlocks = new ulong[3 * CHUNK_SIZE * CHUNK_SIZE];
    private int[] idBlocks = new int[CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE];

    public GameObject gameObject { get; }
    public Mesh mesh { get; }
    public MeshFilter meshFilter { get; }
    public MeshRenderer meshRenderer { get; }
    public MeshCollider meshCollider { get; }

    public List<GameObject> prefabs = new List<GameObject>();

    public Chunk(GameObject gameObject, Mesh mesh, MeshFilter meshFilter, MeshRenderer meshRenderer, MeshCollider meshCollider)
    {
        this.gameObject = gameObject;
        this.mesh = mesh;
        this.meshFilter = meshFilter;
        this.meshRenderer = meshRenderer;
        this.meshCollider = meshCollider;
    }

    public void AddSolid(int x, int y, int z, int id)
    {
        idBlocks[(CHUNK_SIZE * CHUNK_SIZE * z) + (y * CHUNK_SIZE + x)] = id;
        solidBlocks[(CHUNK_SIZE * CHUNK_SIZE * 0) + (z * CHUNK_SIZE + x)] |= 1ul << y;
        solidBlocks[(CHUNK_SIZE * CHUNK_SIZE * 1) + (y * CHUNK_SIZE + z)] |= 1ul << x;
        solidBlocks[(CHUNK_SIZE * CHUNK_SIZE * 2) + (y * CHUNK_SIZE + x)] |= 1ul << z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddSolid(Vector3Int position, int id)
    {
        AddSolid(position.x, position.y, position.z, id);
    }

    public void RemoveSolid(int x, int y, int z)
    {
        idBlocks[(CHUNK_SIZE * CHUNK_SIZE * z) + (y * CHUNK_SIZE + x)] = 0;
        solidBlocks[(CHUNK_SIZE * CHUNK_SIZE * 0) + (z * CHUNK_SIZE + x)] ^= 1ul << y;
        solidBlocks[(CHUNK_SIZE * CHUNK_SIZE * 1) + (y * CHUNK_SIZE + z)] ^= 1ul << x;
        solidBlocks[(CHUNK_SIZE * CHUNK_SIZE * 2) + (y * CHUNK_SIZE + x)] ^= 1ul << z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveSolid(Vector3Int position)
    {
        RemoveSolid(position.x, position.y, position.z);
    }

    public void AddWater(int x, int y, int z)
    {
        idBlocks[(CHUNK_SIZE * CHUNK_SIZE * z) + (y * CHUNK_SIZE + x)] = 3;
        waterBlocks[(CHUNK_SIZE * CHUNK_SIZE * 0) + (z * CHUNK_SIZE + x)] |= 1ul << y;
        waterBlocks[(CHUNK_SIZE * CHUNK_SIZE * 1) + (y * CHUNK_SIZE + z)] |= 1ul << x;
        waterBlocks[(CHUNK_SIZE * CHUNK_SIZE * 2) + (y * CHUNK_SIZE + x)] |= 1ul << z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddWater(Vector3Int position)
    {
        AddWater(position.x, position.y, position.z);
    }

    public void RemoveWater(int x, int y, int z)
    {
        idBlocks[(CHUNK_SIZE * CHUNK_SIZE * z) + (y * CHUNK_SIZE + x)] = 0;
        waterBlocks[(CHUNK_SIZE * CHUNK_SIZE * 0) + (z * CHUNK_SIZE + x)] ^= 1ul << y;
        waterBlocks[(CHUNK_SIZE * CHUNK_SIZE * 1) + (y * CHUNK_SIZE + z)] ^= 1ul << x;
        waterBlocks[(CHUNK_SIZE * CHUNK_SIZE * 2) + (y * CHUNK_SIZE + x)] ^= 1ul << z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveWater(Vector3Int position)
    {
        RemoveWater(position.x, position.y, position.z);
    }

    public void SetBlock(int x, int y, int z, int id)
    {
        if (id == 0)
        {
            int block = idBlocks[(CHUNK_SIZE * CHUNK_SIZE * z) + (y * CHUNK_SIZE + x)];

            if (block == 3)
                RemoveWater(x, y, z);
            else if (block > 0)
                RemoveSolid(x, y, z);
        }
        else if (id == 3)
        {
            AddWater(x, y, z);
        }
        else
        {
            AddSolid(x, y, z, id);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBlock(Vector3Int position, int id)
    {
        SetBlock(position.x, position.y, position.z, id);
    }

    public int GetBlock(int x, int y, int z)
    {
        return idBlocks[(CHUNK_SIZE * CHUNK_SIZE * z) + y * CHUNK_SIZE + x];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetBlock(Vector3Int position)
    {
        return GetBlock(position.x, position.y, position.z);
    }

    public int GetGroundPosition(int x, int y, int z)
    {
        ulong bits = solidBlocks[z * CHUNK_SIZE + x];
        ulong mask = ulong.MaxValue >> (64 - y);
        return 64 - math.lzcnt(bits & mask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetGroundPosition(Vector3Int position)
    {
        return GetGroundPosition(position.x, position.y, position.z);
    }

    public void UpdateMesh()
    {
        NativeList<Vector3> vertices = new NativeList<Vector3>(Allocator.Persistent);
        NativeList<int> triangles = new NativeList<int>(Allocator.Persistent);
        NativeList<Vector2> uvs = new NativeList<Vector2>(Allocator.Persistent);
        NativeList<Segment> segment = new NativeList<Segment>(Allocator.Persistent);

        NativeArray<ulong> sBlocks = new NativeArray<ulong>(solidBlocks, Allocator.Persistent);
        NativeArray<ulong> wBlocks = new NativeArray<ulong>(waterBlocks, Allocator.Persistent);
        NativeArray<int> iBlocks = new NativeArray<int>(idBlocks, Allocator.Persistent);

        MeshJob job = new MeshJob
        {
            SolidBlocks = sBlocks,
            WaterBlocks = wBlocks,
            IdBlocks = iBlocks,
            BlockProperties = Blocks.Instance.blockProperties,
            Vertices = vertices,
            Triangles = triangles,
            Uvs = uvs,
            Segment = segment,
        };

        job.Schedule().Complete();

        sBlocks.Dispose();
        wBlocks.Dispose();
        iBlocks.Dispose();

        mesh.Clear();

        // Material
        Material[] materials = new Material[segment.Length];

        for (int i = 0; i < segment.Length; i++)
            materials[i] = Blocks.Instance.materials[segment[i].materialId];

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
        public NativeArray<ulong> SolidBlocks;

        [ReadOnly]
        public NativeArray<ulong> WaterBlocks;

        [ReadOnly]
        public NativeArray<int> IdBlocks;

        [ReadOnly]
        public UnsafeHashMap<int, BlockProperties> BlockProperties;

        [WriteOnly]
        public NativeList<Vector3> Vertices;

        [WriteOnly]
        public NativeList<int> Triangles;

        [WriteOnly]
        public NativeList<Vector2> Uvs;

        [WriteOnly]
        public NativeList<Segment> Segment;

        public int GetBlockID(int x, int y, int z)
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
                        ulong solidBits = SolidBlocks[(CHUNK_SIZE * CHUNK_SIZE * axis) + (z * CHUNK_SIZE + x)];
                        ulong waterBits = WaterBlocks[(CHUNK_SIZE * CHUNK_SIZE * axis) + (z * CHUNK_SIZE + x)];

                        int axis2 = axis * 2;

                        // Solid right
                        ulong solidRight = solidBits & ~(solidBits << 1);

                        while (solidRight != 0)
                        {
                            int y = math.tzcnt(solidRight);
                            solidRight &= solidRight - 1;

                            int id = axis2 switch
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

                        // Solid left
                        ulong solidLeft = solidBits & ~(solidBits >> 1);

                        while (solidLeft != 0)
                        {
                            int y = math.tzcnt(solidLeft);
                            solidLeft &= solidLeft - 1;

                            int id = axis2 switch
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

                        // Water right
                        ulong waterRight = (waterBits & ~(waterBits << 1)) & ~((solidBits & ~(solidBits >> 1)) << 1);

                        while (waterRight != 0)
                        {
                            int y = math.tzcnt(waterRight);
                            waterRight &= waterRight - 1;

                            BlockData blockData = new BlockData(3, axis2, y);

                            if (!data.TryGetValue(blockData, out var array))
                            {
                                array = new NativeArray<ulong>(CHUNK_SIZE, Allocator.Temp);
                                data.TryAdd(blockData, array);
                            }

                            array[x] |= 1ul << z;
                        }

                        // Water left
                        ulong waterLeft = (waterBits & ~(waterBits >> 1)) & ~((solidBits & ~(solidBits << 1)) >> 1);

                        while (waterLeft != 0)
                        {
                            int y = math.tzcnt(waterLeft);
                            waterLeft &= waterLeft - 1;

                            BlockData blockData = new BlockData(3, axis2 + 1, y);

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

            var triangles = new UnsafeHashMap<int, NativeList<int>>(16, Allocator.Temp);
            int verticesLength = 0;

            foreach (var item in data)
            {
                BlockData blockData = item.Key;
                NativeArray<ulong> xz = item.Value;

                int id = blockData.id;
                int axis = blockData.axis;
                int y = blockData.y;

                int materialId;

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
                int materialId = item.Key;

                Triangles.AddRange(list.AsArray());
                Segment.Add(new Segment(materialId, prev, list.Length));

                prev += list.Length;
            }
        }
    }

    public struct BlockData : IEquatable<BlockData>
    {
        public readonly int id;
        public readonly int axis;
        public readonly int y;

        public BlockData(int id, int axis, int y)
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
        public readonly int materialId;
        public readonly int start;
        public readonly int length;

        public Segment(int materialId, int start, int length)
        {
            this.materialId = materialId;
            this.start = start;
            this.length = length;
        }
    }
}