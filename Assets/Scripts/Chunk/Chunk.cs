using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using static Constants;

public class Chunk
{
    // Blocks position
    private BlockType[,,] typeBlocks = new BlockType[CHUNK_SIZE, CHUNK_SIZE, CHUNK_SIZE];
    private ulong[,,] binaryBlocks = new ulong[3, CHUNK_SIZE, CHUNK_SIZE];

    // Chunk position
    public readonly Vector3Int relativePosition;

    // Game objects
    public Dictionary<BlockType, GameObject> gameObjects = new Dictionary<BlockType, GameObject>();

    // Mesh data
    public MeshData[] meshData { get; private set; }

    public Chunk(Vector3Int position)
    {
        relativePosition = position;
    }

    public void SetBlock(int x, int y, int z, BlockType blockType)
    {
        typeBlocks[x, y, z] = blockType;
        binaryBlocks[0, z, x] |= 1ul << y;
        binaryBlocks[1, y, z] |= 1ul << x;
        binaryBlocks[2, y, x] |= 1ul << z;
    }

    public BlockType GetBlockType(int x, int y, int z)
    {
        return typeBlocks[x, y, z];
    }

    public void CalculateMeshData()
    {
        Dictionary<BlockType, Dictionary<int, Dictionary<int, ulong[]>>> data = new Dictionary<BlockType, Dictionary<int, Dictionary<int, ulong[]>>>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void AddData(int x, int z, int axis, ulong bits)
        {
            while (bits != 0)
            {
                int y = math.tzcnt(bits);
                bits &= bits - 1;

                BlockType blockType = axis switch
                {
                    0 or 1 => GetBlockType(x, y, z),
                    2 or 3 => GetBlockType(y, z, x),
                    _ => GetBlockType(x, z, y)
                };

                if (!data.TryGetValue(blockType, out var a))
                    data[blockType] = a = new Dictionary<int, Dictionary<int, ulong[]>>();

                if (!data[blockType].TryGetValue(axis, out var b))
                    data[blockType][axis] = b = new Dictionary<int, ulong[]>();

                if (!data[blockType][axis].TryGetValue(y, out var c))
                    data[blockType][axis][y] = c = new ulong[CHUNK_SIZE];

                data[blockType][axis][y][x] |= 1ul << z;
            }
        }

        // Face culling
        for (int axis = 0; axis < 3; axis++)
        {
            for (int z = 0; z < CHUNK_SIZE; z++)
            {
                for (int x = 0; x < CHUNK_SIZE; x++)
                {
                    ulong bits = binaryBlocks[axis, z, x];

                    ulong right = bits & ~(bits << 1); // Right face culling
                    ulong left = bits & ~(bits >> 1); // Left face culling

                    int doubleAxis = axis * 2;

                    AddData(x, z, doubleAxis, right);
                    AddData(x, z, doubleAxis + 1, left);
                }
            }
        }

        // Mesh data
        meshData = new MeshData[data.Count()];
        int meshDataIndex = 0;

        // Calculate vertices and triangles
        foreach ((BlockType blockType, Dictionary<int, Dictionary<int, ulong[]>> dictionary) in data)
        {
            // Calculate array length
            int verticesLength = 0;
            int trianglesLength = 0;

            int trianglesIndex = 0;
            int verticesIndex = 0;

            foreach ((int axis, Dictionary<int, ulong[]> blocks) in dictionary)
            {
                foreach ((int y, ulong[] bits) in blocks.Where(b => b.Key > 0 && b.Key <= CHUNK_SIZE - 2)) // Remove padding
                {
                    ulong[] xz = (ulong[])bits.Clone();

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

                            trianglesLength += 6;
                            verticesLength += 4;
                        }
                    }
                }
            }

            Vector3[] vertices = new Vector3[verticesLength];
            int[] triangles = new int[trianglesLength];

            // Add data to array
            foreach ((int axis, Dictionary<int, ulong[]> blocks) in dictionary)
            {
                foreach ((int y, ulong[] xz /*bits*/) in blocks.Where(b => b.Key > 0 && b.Key <= CHUNK_SIZE - 2)) // Remove padding
                {
                    // Only need if we don't want to overwrite the values
                    // ulong[] xz = (ulong[])bits.Clone();

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

                            // Triangles
                            triangles[trianglesIndex] = verticesIndex;
                            triangles[trianglesIndex + 1] = verticesIndex + 1;
                            triangles[trianglesIndex + 2] = verticesIndex + 2;

                            triangles[trianglesIndex + 3] = verticesIndex;
                            triangles[trianglesIndex + 4] = verticesIndex + 2;
                            triangles[trianglesIndex + 5] = verticesIndex + 3;

                            // Vertices
                            if (axis == 0) // Bottom
                            {
                                vertices[verticesIndex] = new Vector3(i, y, trailingZeros) + relativePosition;
                                vertices[verticesIndex + 1] = new Vector3(i + height, y, trailingZeros) + relativePosition;
                                vertices[verticesIndex + 2] = new Vector3(i + height, y, trailingZeros + trailingOnes) + relativePosition;
                                vertices[verticesIndex + 3] = new Vector3(i, y, trailingZeros + trailingOnes) + relativePosition;
                            }
                            else if (axis == 1) // Top
                            {
                                vertices[verticesIndex] = new Vector3(i + height, y + 1, trailingZeros) + relativePosition;
                                vertices[verticesIndex + 1] = new Vector3(i, y + 1, trailingZeros) + relativePosition;
                                vertices[verticesIndex + 2] = new Vector3(i, y + 1, trailingZeros + trailingOnes) + relativePosition;
                                vertices[verticesIndex + 3] = new Vector3(i + height, y + 1, trailingZeros + trailingOnes) + relativePosition;
                            }
                            else if (axis == 2) // Left
                            {
                                vertices[verticesIndex] = new Vector3(y, trailingZeros, i) + relativePosition;
                                vertices[verticesIndex + 1] = new Vector3(y, trailingZeros, i + height) + relativePosition;
                                vertices[verticesIndex + 2] = new Vector3(y, trailingZeros + trailingOnes, i + height) + relativePosition;
                                vertices[verticesIndex + 3] = new Vector3(y, trailingZeros + trailingOnes, i) + relativePosition;
                            }
                            else if (axis == 3) // Right
                            {
                                vertices[verticesIndex] = new Vector3(y + 1, trailingZeros, i + height) + relativePosition;
                                vertices[verticesIndex + 1] = new Vector3(y + 1, trailingZeros, i) + relativePosition;
                                vertices[verticesIndex + 2] = new Vector3(y + 1, trailingZeros + trailingOnes, i) + relativePosition;
                                vertices[verticesIndex + 3] = new Vector3(y + 1, trailingZeros + trailingOnes, i + height) + relativePosition;
                            }
                            else if (axis == 4) // Back
                            {
                                vertices[verticesIndex] = new Vector3(i + height, trailingZeros, y) + relativePosition;
                                vertices[verticesIndex + 1] = new Vector3(i, trailingZeros, y) + relativePosition;
                                vertices[verticesIndex + 2] = new Vector3(i, trailingZeros + trailingOnes, y) + relativePosition;
                                vertices[verticesIndex + 3] = new Vector3(i + height, trailingZeros + trailingOnes, y) + relativePosition;
                            }
                            else if (axis == 5) // Front
                            {
                                vertices[verticesIndex] = new Vector3(i, trailingZeros, y + 1) + relativePosition;
                                vertices[verticesIndex + 1] = new Vector3(i + height, trailingZeros, y + 1) + relativePosition;
                                vertices[verticesIndex + 2] = new Vector3(i + height, trailingZeros + trailingOnes, y + 1) + relativePosition;
                                vertices[verticesIndex + 3] = new Vector3(i, trailingZeros + trailingOnes, y + 1) + relativePosition;
                            }

                            trianglesIndex += 6;
                            verticesIndex += 4;
                        }
                    }
                }
            }

            meshData[meshDataIndex++] = new MeshData(blockType, vertices, triangles);
        }
    }
}

public struct MeshData
{
    public BlockType blockType;
    public Vector3[] vertices;
    public int[] triangles;

    public MeshData(BlockType blockType, Vector3[] vertices, int[] triangles)
    {
        this.blockType = blockType;
        this.vertices = vertices;
        this.triangles = triangles;
    }
}