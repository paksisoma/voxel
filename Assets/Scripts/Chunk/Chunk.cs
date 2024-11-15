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
    private ulong[,,] binaryNormalBlocks = new ulong[3, CHUNK_SIZE, CHUNK_SIZE];
    private ulong[,,] binaryWaterBlocks = new ulong[3, CHUNK_SIZE, CHUNK_SIZE];

    // Chunk position
    public readonly Vector3Int relativePosition;

    // Game objects
    public Dictionary<BlockType, GameObject> gameObjects = new Dictionary<BlockType, GameObject>();
    public List<GameObject> prefabs = new List<GameObject>();

    // Mesh data
    public MeshData[] meshData { get; private set; }

    public Chunk(Vector3Int position)
    {
        relativePosition = position;
    }

    public void SetBlock(int x, int y, int z, BlockType blockType)
    {
        typeBlocks[x, y, z] = blockType;
        binaryNormalBlocks[0, z, x] |= 1ul << y;
        binaryNormalBlocks[1, y, z] |= 1ul << x;
        binaryNormalBlocks[2, y, x] |= 1ul << z;
    }

    public void SetWater(int x, int y, int z)
    {
        typeBlocks[x, y, z] = BlockType.Water;
        binaryWaterBlocks[0, z, x] |= 1ul << y;
        binaryWaterBlocks[1, y, z] |= 1ul << x;
        binaryWaterBlocks[2, y, x] |= 1ul << z;
    }

    public BlockType GetBlock(int x, int y, int z)
    {
        return typeBlocks[x, y, z];
    }

    public void CalculateMeshData()
    {
        Dictionary<BlockType, Dictionary<int, Dictionary<int, ulong[]>>> data = new Dictionary<BlockType, Dictionary<int, Dictionary<int, ulong[]>>>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void AddNormalData(int x, int z, int axis, ulong bits)
        {
            while (bits != 0)
            {
                int y = math.tzcnt(bits);
                bits &= bits - 1;

                BlockType blockType = axis switch
                {
                    0 or 1 => GetBlock(x, y, z),
                    2 or 3 => GetBlock(y, z, x),
                    _ => GetBlock(x, z, y)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void AddWaterData(int x, int z, int axis, ulong bits)
        {
            while (bits != 0)
            {
                int y = math.tzcnt(bits);
                bits &= bits - 1;

                if (!data.TryGetValue(BlockType.Water, out var a))
                    data[BlockType.Water] = a = new Dictionary<int, Dictionary<int, ulong[]>>();

                if (!data[BlockType.Water].TryGetValue(axis, out var b))
                    data[BlockType.Water][axis] = b = new Dictionary<int, ulong[]>();

                if (!data[BlockType.Water][axis].TryGetValue(y, out var c))
                    data[BlockType.Water][axis][y] = c = new ulong[CHUNK_SIZE];

                data[BlockType.Water][axis][y][x] |= 1ul << z;
            }
        }

        // Face culling
        for (int axis = 0; axis < 3; axis++)
        {
            for (int z = 0; z < CHUNK_SIZE; z++)
            {
                for (int x = 0; x < CHUNK_SIZE; x++)
                {
                    int doubleAxis = axis * 2;

                    // Simple block
                    ulong normalBits = binaryNormalBlocks[axis, z, x];

                    ulong normalRight = normalBits & ~(normalBits << 1); // Right face culling
                    AddNormalData(x, z, doubleAxis, normalRight);

                    ulong normalLeft = normalBits & ~(normalBits >> 1); // Left face culling
                    AddNormalData(x, z, doubleAxis + 1, normalLeft);

                    // Water block
                    ulong waterBits = binaryWaterBlocks[axis, z, x];

                    if (waterBits == 0)
                        continue;

                    ulong waterRight = waterBits & ~(waterBits << 1); // Right face culling
                    waterRight ^= normalLeft << 1;
                    AddWaterData(x, z, doubleAxis, waterRight);

                    ulong waterLeft = waterBits & ~(waterBits >> 1); // Left face culling

                    // If not top
                    if (axis != 0)
                        waterLeft ^= normalRight >> 1;

                    AddWaterData(x, z, doubleAxis + 1, waterLeft);
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
            // Using list would be better but much slower (i think)
            int verticesLength = 0;
            int topTrianglesLength = 0;
            int sideTrianglesLength = 0;
            int bottomTrianglesLength = 0;

            int verticesIndex = 0;
            int topTrianglesIndex = 0;
            int sideTrianglesIndex = 0;
            int bottomTrianglesIndex = 0;

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

                            if (axis == 0) // Bottom
                            {
                                bottomTrianglesLength += 6;
                            }
                            else if (axis == 1) // Top
                            {
                                topTrianglesLength += 6;
                            }
                            else // Side
                            {
                                sideTrianglesLength += 6;
                            }

                            verticesLength += 4;
                        }
                    }
                }
            }

            Vector3[] vertices = new Vector3[verticesLength];
            int[] topTriangles = new int[topTrianglesLength];
            int[] sideTriangles = new int[sideTrianglesLength];
            int[] bottomTriangles = new int[bottomTrianglesLength];
            Vector2[] uvs = new Vector2[verticesLength];

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

                            // Vertices and triangles
                            Vector3 verticesPosition = relativePosition;

                            if (axis == 0) // Bottom
                            {
                                // Vertices
                                vertices[verticesIndex] = new Vector3(i, y, trailingZeros) + verticesPosition;
                                vertices[verticesIndex + 1] = new Vector3(i + height, y, trailingZeros) + verticesPosition;
                                vertices[verticesIndex + 2] = new Vector3(i + height, y, trailingZeros + trailingOnes) + verticesPosition;
                                vertices[verticesIndex + 3] = new Vector3(i, y, trailingZeros + trailingOnes) + verticesPosition;

                                // Triangles
                                bottomTriangles[bottomTrianglesIndex++] = verticesIndex;
                                bottomTriangles[bottomTrianglesIndex++] = verticesIndex + 1;
                                bottomTriangles[bottomTrianglesIndex++] = verticesIndex + 2;

                                bottomTriangles[bottomTrianglesIndex++] = verticesIndex;
                                bottomTriangles[bottomTrianglesIndex++] = verticesIndex + 2;
                                bottomTriangles[bottomTrianglesIndex++] = verticesIndex + 3;

                                // UV
                                uvs[verticesIndex] = new Vector2(height, 0);
                                uvs[verticesIndex + 1] = new Vector2(0, 0);
                                uvs[verticesIndex + 2] = new Vector2(0, trailingOnes);
                                uvs[verticesIndex + 3] = new Vector2(height, trailingOnes);
                            }
                            else if (axis == 1) // Top
                            {
                                // Vertices
                                if (blockType == BlockType.Water) // Water block
                                    verticesPosition.y -= 0.3f;

                                vertices[verticesIndex] = new Vector3(i + height, y + 1, trailingZeros) + verticesPosition;
                                vertices[verticesIndex + 1] = new Vector3(i, y + 1, trailingZeros) + verticesPosition;
                                vertices[verticesIndex + 2] = new Vector3(i, y + 1, trailingZeros + trailingOnes) + verticesPosition;
                                vertices[verticesIndex + 3] = new Vector3(i + height, y + 1, trailingZeros + trailingOnes) + verticesPosition;

                                // Triangles
                                topTriangles[topTrianglesIndex++] = verticesIndex;
                                topTriangles[topTrianglesIndex++] = verticesIndex + 1;
                                topTriangles[topTrianglesIndex++] = verticesIndex + 2;

                                topTriangles[topTrianglesIndex++] = verticesIndex;
                                topTriangles[topTrianglesIndex++] = verticesIndex + 2;
                                topTriangles[topTrianglesIndex++] = verticesIndex + 3;

                                // UV
                                uvs[verticesIndex] = new Vector2(height, 0);
                                uvs[verticesIndex + 1] = new Vector2(0, 0);
                                uvs[verticesIndex + 2] = new Vector2(0, trailingOnes);
                                uvs[verticesIndex + 3] = new Vector2(height, trailingOnes);
                            }
                            else if (axis == 2) // Left
                            {
                                // Vertices
                                vertices[verticesIndex] = new Vector3(y, trailingZeros, i) + verticesPosition;
                                vertices[verticesIndex + 1] = new Vector3(y, trailingZeros, i + height) + verticesPosition;
                                vertices[verticesIndex + 2] = new Vector3(y, trailingZeros + trailingOnes, i + height) + verticesPosition;
                                vertices[verticesIndex + 3] = new Vector3(y, trailingZeros + trailingOnes, i) + verticesPosition;

                                // Triangles
                                sideTriangles[sideTrianglesIndex++] = verticesIndex;
                                sideTriangles[sideTrianglesIndex++] = verticesIndex + 1;
                                sideTriangles[sideTrianglesIndex++] = verticesIndex + 2;

                                sideTriangles[sideTrianglesIndex++] = verticesIndex;
                                sideTriangles[sideTrianglesIndex++] = verticesIndex + 2;
                                sideTriangles[sideTrianglesIndex++] = verticesIndex + 3;

                                // UV
                                uvs[verticesIndex] = new Vector2(0, 0);
                                uvs[verticesIndex + 1] = new Vector2(height, 0);
                                uvs[verticesIndex + 2] = new Vector2(height, trailingOnes);
                                uvs[verticesIndex + 3] = new Vector2(0, trailingOnes);
                            }
                            else if (axis == 3) // Right
                            {
                                // Vertices
                                vertices[verticesIndex] = new Vector3(y + 1, trailingZeros, i + height) + verticesPosition;
                                vertices[verticesIndex + 1] = new Vector3(y + 1, trailingZeros, i) + verticesPosition;
                                vertices[verticesIndex + 2] = new Vector3(y + 1, trailingZeros + trailingOnes, i) + verticesPosition;
                                vertices[verticesIndex + 3] = new Vector3(y + 1, trailingZeros + trailingOnes, i + height) + verticesPosition;

                                // Triangles
                                sideTriangles[sideTrianglesIndex++] = verticesIndex;
                                sideTriangles[sideTrianglesIndex++] = verticesIndex + 1;
                                sideTriangles[sideTrianglesIndex++] = verticesIndex + 2;

                                sideTriangles[sideTrianglesIndex++] = verticesIndex;
                                sideTriangles[sideTrianglesIndex++] = verticesIndex + 2;
                                sideTriangles[sideTrianglesIndex++] = verticesIndex + 3;

                                // UV
                                uvs[verticesIndex] = new Vector2(0, 0);
                                uvs[verticesIndex + 1] = new Vector2(height, 0);
                                uvs[verticesIndex + 2] = new Vector2(height, trailingOnes);
                                uvs[verticesIndex + 3] = new Vector2(0, trailingOnes);
                            }
                            else if (axis == 4) // Back
                            {
                                // Vertices
                                vertices[verticesIndex] = new Vector3(i + height, trailingZeros, y) + verticesPosition;
                                vertices[verticesIndex + 1] = new Vector3(i, trailingZeros, y) + verticesPosition;
                                vertices[verticesIndex + 2] = new Vector3(i, trailingZeros + trailingOnes, y) + verticesPosition;
                                vertices[verticesIndex + 3] = new Vector3(i + height, trailingZeros + trailingOnes, y) + verticesPosition;

                                // Triangles
                                sideTriangles[sideTrianglesIndex++] = verticesIndex;
                                sideTriangles[sideTrianglesIndex++] = verticesIndex + 1;
                                sideTriangles[sideTrianglesIndex++] = verticesIndex + 2;

                                sideTriangles[sideTrianglesIndex++] = verticesIndex;
                                sideTriangles[sideTrianglesIndex++] = verticesIndex + 2;
                                sideTriangles[sideTrianglesIndex++] = verticesIndex + 3;

                                // UV
                                uvs[verticesIndex] = new Vector2(height, 0);
                                uvs[verticesIndex + 1] = new Vector2(0, 0);
                                uvs[verticesIndex + 2] = new Vector2(0, trailingOnes);
                                uvs[verticesIndex + 3] = new Vector2(height, trailingOnes);
                            }
                            else if (axis == 5) // Front
                            {
                                // Vertices
                                vertices[verticesIndex] = new Vector3(i, trailingZeros, y + 1) + verticesPosition;
                                vertices[verticesIndex + 1] = new Vector3(i + height, trailingZeros, y + 1) + verticesPosition;
                                vertices[verticesIndex + 2] = new Vector3(i + height, trailingZeros + trailingOnes, y + 1) + verticesPosition;
                                vertices[verticesIndex + 3] = new Vector3(i, trailingZeros + trailingOnes, y + 1) + verticesPosition;

                                // Triangles
                                sideTriangles[sideTrianglesIndex++] = verticesIndex;
                                sideTriangles[sideTrianglesIndex++] = verticesIndex + 1;
                                sideTriangles[sideTrianglesIndex++] = verticesIndex + 2;

                                sideTriangles[sideTrianglesIndex++] = verticesIndex;
                                sideTriangles[sideTrianglesIndex++] = verticesIndex + 2;
                                sideTriangles[sideTrianglesIndex++] = verticesIndex + 3;

                                // UV
                                uvs[verticesIndex] = new Vector2(height, 0);
                                uvs[verticesIndex + 1] = new Vector2(0, 0);
                                uvs[verticesIndex + 2] = new Vector2(0, trailingOnes);
                                uvs[verticesIndex + 3] = new Vector2(height, trailingOnes);
                            }

                            verticesIndex += 4;
                        }
                    }
                }
            }

            meshData[meshDataIndex++] = new MeshData(blockType, vertices, topTriangles, sideTriangles, bottomTriangles, uvs);
        }
    }
}

public struct MeshData
{
    public BlockType blockType;
    public Vector3[] vertices;
    // Separating the triangles array only if the block has different materials would be better
    public int[] topTriangles;
    public int[] sideTriangles;
    public int[] bottomTriangles;
    public Vector2[] uvs;

    public MeshData(BlockType blockType, Vector3[] vertices, int[] topTriangles, int[] sideTriangles, int[] bottomTriangles, Vector2[] uvs)
    {
        this.blockType = blockType;
        this.vertices = vertices;
        this.topTriangles = topTriangles;
        this.sideTriangles = sideTriangles;
        this.bottomTriangles = bottomTriangles;
        this.uvs = uvs;
    }
}