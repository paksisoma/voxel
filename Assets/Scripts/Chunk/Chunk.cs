using System.Collections.Generic;
using System.Linq;
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

    // Mesh
    private ChunkMesh[] _meshes;
    private GameObject _gameObject;

    public ChunkMesh[] meshes
    {
        get
        {
            lock (lockObject)
            {
                return _meshes;
            }
        }
    }

    public GameObject gameObject
    {
        get
        {
            lock (lockObject)
            {
                return _gameObject;
            }
        }
        set
        {
            lock (lockObject)
            {
                _gameObject = value;
            }
        }
    }

    // Lock
    private object lockObject = new object();

    public Chunk(Vector3Int position)
    {
        relativePosition = position;
    }

    public void SetBlock(byte x, byte y, byte z, BlockType blockType)
    {
        lock (lockObject)
        {
            typeBlocks[x, y, z] = blockType;
            binaryBlocks[0, z, x] |= 1ul << y;
            binaryBlocks[1, y, z] |= 1ul << x;
            binaryBlocks[2, y, x] |= 1ul << z;
        }
    }

    public BlockType GetBlockType(byte x, byte y, byte z)
    {
        lock (lockObject)
        {
            return typeBlocks[x, y, z];
        }
    }

    public BlockType GetBlockType(Vector3Byte position)
    {
        lock (lockObject)
        {
            return typeBlocks[position.x, position.y, position.z];
        }
    }

    public void CalculateMeshData()
    {
        lock (lockObject)
        {
            Dictionary<BlockType, Dictionary<byte, Dictionary<byte, ulong[]>>> data = new Dictionary<BlockType, Dictionary<byte, Dictionary<byte, ulong[]>>>();
            
            // FACE CULLING

            ulong[,,] culling = new ulong[6, CHUNK_SIZE, CHUNK_SIZE];

            for (byte axis = 0; axis < 3; axis++)
            {
                for (byte z = 0; z < CHUNK_SIZE; z++)
                {
                    for (byte x = 0; x < CHUNK_SIZE; x++)
                    {
                        ulong col = binaryBlocks[axis, z, x];
                        culling[2 * axis + 0, z, x] = col & ~(col << 1);
                        culling[2 * axis + 1, z, x] = col & ~(col >> 1);
                    }
                }
            }

            // SEPARATE DIFFERENT BLOCK TYPES

            for (byte axis = 0; axis < 6; axis++)
            {
                for (byte z = 0; z < CHUNK_SIZE; z++)
                {
                    for (byte x = 0; x < CHUNK_SIZE; x++)
                    {
                        ulong col = culling[axis, z, x];

                        while (col != 0)
                        {
                            byte y = (byte)math.tzcnt(col);
                            col &= col - 1;

                            Vector3Byte position = axis switch
                            {
                                0 or 1 => new Vector3Byte(x, y, z),
                                2 or 3 => new Vector3Byte(y, z, x),
                                _ => new Vector3Byte(x, z, y),
                            };

                            BlockType blockType = GetBlockType(position);

                            // ADD DATA

                            if (!data.TryGetValue(blockType, out var a))
                                data[blockType] = a = new Dictionary<byte, Dictionary<byte, ulong[]>>();

                            if (!data[blockType].TryGetValue(axis, out var b))
                                data[blockType][axis] = b = new Dictionary<byte, ulong[]>();

                            if (!data[blockType][axis].TryGetValue(y, out var c))
                                data[blockType][axis][y] = c = new ulong[CHUNK_SIZE];

                            data[blockType][axis][y][x] |= 1ul << z;
                        }
                    }
                }
            }

            // CALCULATE VERTICES, TRIANGLES

            _meshes = new ChunkMesh[data.Count];
            int meshIndex = 0;

            foreach (var c in data)
            {
                BlockType blockType = c.Key;

                uint trianglesLength = 0;
                uint verticesLength = 0;

                int verticesIndex = 0;
                int trianglesIndex = 0;

                // COUNT ARRAY LENGTH

                foreach (var a in c.Value)
                {
                    byte axis = a.Key;
                    Dictionary<byte, ulong[]> blocks = a.Value;

                    foreach (var b in blocks.Where(b => b.Key > 0 && b.Key <= CHUNK_SIZE - 2)) // PADDDING REMOVE
                    {
                        byte y = b.Key;
                        ulong[] xz = (ulong[])b.Value.Clone();

                        for (byte i = 1; i < xz.Length - 1; i++)
                        {
                            // PADDDING REMOVE
                            xz[i] = (xz[i] & ~1ul) & ~(1ul << CHUNK_SIZE - 1);

                            while (xz[i] != 0)
                            {
                                int trailingZeros = math.tzcnt(xz[i]);
                                int trailingOnes = math.tzcnt(~xz[i] >> trailingZeros);
                                ulong mask = ((1ul << trailingOnes) - 1ul) << trailingZeros;

                                byte height = 1;

                                for (int j = i + 1; j < xz.Length - 1 && (xz[j] & mask) == mask; j++)
                                {
                                    // PADDDING REMOVE
                                    xz[j] = (xz[j] & ~1ul) & ~(1ul << CHUNK_SIZE - 1);

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

                // MESH

                _meshes[meshIndex] = new ChunkMesh(blockType, new Vector3[verticesLength], new int[trianglesLength]);

                // ADD VERTICES AND TRIANGLES TO ARRAY USING BINARY GREEDY MESHING

                foreach (var a in c.Value)
                {
                    byte axis = a.Key;
                    Dictionary<byte, ulong[]> blocks = a.Value;

                    foreach (var b in blocks.Where(b => b.Key > 0 && b.Key <= CHUNK_SIZE - 2)) // PADDDING REMOVE
                    {
                        byte y = b.Key;
                        ulong[] xz = (ulong[])b.Value.Clone();

                        for (byte i = 1; i < xz.Length - 1; i++)
                        {
                            // PADDDING REMOVE
                            xz[i] = (xz[i] & ~1ul) & ~(1ul << CHUNK_SIZE - 1);

                            while (xz[i] != 0)
                            {
                                int trailingZeros = math.tzcnt(xz[i]);
                                int trailingOnes = math.tzcnt(~xz[i] >> trailingZeros);
                                ulong mask = ((1ul << trailingOnes) - 1ul) << trailingZeros;

                                byte height = 1;

                                for (int j = i + 1; j < xz.Length - 1 && (xz[j] & mask) == mask; j++)
                                {
                                    // PADDDING REMOVE
                                    xz[j] = (xz[j] & ~1ul) & ~(1ul << CHUNK_SIZE - 1);

                                    xz[j] ^= mask;
                                    height++;
                                }

                                xz[i] ^= mask;

                                // TRIANGLES

                                _meshes[meshIndex].triangles[trianglesIndex] = verticesIndex;
                                _meshes[meshIndex].triangles[trianglesIndex + 1] = verticesIndex + 1;
                                _meshes[meshIndex].triangles[trianglesIndex + 2] = verticesIndex + 2;

                                _meshes[meshIndex].triangles[trianglesIndex + 3] = verticesIndex;
                                _meshes[meshIndex].triangles[trianglesIndex + 4] = verticesIndex + 2;
                                _meshes[meshIndex].triangles[trianglesIndex + 5] = verticesIndex + 3;

                                // VERTICES

                                if (axis == 0) // Bottom
                                {
                                    _meshes[meshIndex].vertices[verticesIndex] = new Vector3(i, y, trailingZeros) + new Vector3(relativePosition.x, relativePosition.y, relativePosition.z);
                                    _meshes[meshIndex].vertices[verticesIndex + 1] = new Vector3(i + height, y, trailingZeros) + new Vector3(relativePosition.x, relativePosition.y, relativePosition.z);
                                    _meshes[meshIndex].vertices[verticesIndex + 2] = new Vector3(i + height, y, trailingZeros + trailingOnes) + new Vector3(relativePosition.x, relativePosition.y, relativePosition.z);
                                    _meshes[meshIndex].vertices[verticesIndex + 3] = new Vector3(i, y, trailingZeros + trailingOnes) + new Vector3(relativePosition.x, relativePosition.y, relativePosition.z);
                                }
                                else if (axis == 1) // Top
                                {
                                    _meshes[meshIndex].vertices[verticesIndex] = new Vector3(i + height, y + 1, trailingZeros) + new Vector3(relativePosition.x, relativePosition.y, relativePosition.z);
                                    _meshes[meshIndex].vertices[verticesIndex + 1] = new Vector3(i, y + 1, trailingZeros) + new Vector3(relativePosition.x, relativePosition.y, relativePosition.z);
                                    _meshes[meshIndex].vertices[verticesIndex + 2] = new Vector3(i, y + 1, trailingZeros + trailingOnes) + new Vector3(relativePosition.x, relativePosition.y, relativePosition.z);
                                    _meshes[meshIndex].vertices[verticesIndex + 3] = new Vector3(i + height, y + 1, trailingZeros + trailingOnes) + new Vector3(relativePosition.x, relativePosition.y, relativePosition.z);
                                }
                                else if (axis == 2) // Left
                                {
                                    _meshes[meshIndex].vertices[verticesIndex] = new Vector3(y, trailingZeros, i) + new Vector3(relativePosition.x, relativePosition.y, relativePosition.z);
                                    _meshes[meshIndex].vertices[verticesIndex + 1] = new Vector3(y, trailingZeros, i + height) + new Vector3(relativePosition.x, relativePosition.y, relativePosition.z);
                                    _meshes[meshIndex].vertices[verticesIndex + 2] = new Vector3(y, trailingZeros + trailingOnes, i + height) + new Vector3(relativePosition.x, relativePosition.y, relativePosition.z);
                                    _meshes[meshIndex].vertices[verticesIndex + 3] = new Vector3(y, trailingZeros + trailingOnes, i) + new Vector3(relativePosition.x, relativePosition.y, relativePosition.z);
                                }
                                else if (axis == 3) // Right
                                {
                                    _meshes[meshIndex].vertices[verticesIndex] = new Vector3(y + 1, trailingZeros, i + height) + new Vector3(relativePosition.x, relativePosition.y, relativePosition.z);
                                    _meshes[meshIndex].vertices[verticesIndex + 1] = new Vector3(y + 1, trailingZeros, i) + new Vector3(relativePosition.x, relativePosition.y, relativePosition.z);
                                    _meshes[meshIndex].vertices[verticesIndex + 2] = new Vector3(y + 1, trailingZeros + trailingOnes, i) + new Vector3(relativePosition.x, relativePosition.y, relativePosition.z);
                                    _meshes[meshIndex].vertices[verticesIndex + 3] = new Vector3(y + 1, trailingZeros + trailingOnes, i + height) + new Vector3(relativePosition.x, relativePosition.y, relativePosition.z);
                                }
                                else if (axis == 4) // Back
                                {
                                    _meshes[meshIndex].vertices[verticesIndex] = new Vector3(i + height, trailingZeros, y) + new Vector3(relativePosition.x, relativePosition.y, relativePosition.z);
                                    _meshes[meshIndex].vertices[verticesIndex + 1] = new Vector3(i, trailingZeros, y) + new Vector3(relativePosition.x, relativePosition.y, relativePosition.z);
                                    _meshes[meshIndex].vertices[verticesIndex + 2] = new Vector3(i, trailingZeros + trailingOnes, y) + new Vector3(relativePosition.x, relativePosition.y, relativePosition.z);
                                    _meshes[meshIndex].vertices[verticesIndex + 3] = new Vector3(i + height, trailingZeros + trailingOnes, y) + new Vector3(relativePosition.x, relativePosition.y, relativePosition.z);
                                }
                                else if (axis == 5) // Front
                                {
                                    _meshes[meshIndex].vertices[verticesIndex] = new Vector3(i, trailingZeros, y + 1) + new Vector3(relativePosition.x, relativePosition.y, relativePosition.z);
                                    _meshes[meshIndex].vertices[verticesIndex + 1] = new Vector3(i + height, trailingZeros, y + 1) + new Vector3(relativePosition.x, relativePosition.y, relativePosition.z);
                                    _meshes[meshIndex].vertices[verticesIndex + 2] = new Vector3(i + height, trailingZeros + trailingOnes, y + 1) + new Vector3(relativePosition.x, relativePosition.y, relativePosition.z);
                                    _meshes[meshIndex].vertices[verticesIndex + 3] = new Vector3(i, trailingZeros + trailingOnes, y + 1) + new Vector3(relativePosition.x, relativePosition.y, relativePosition.z);
                                }

                                trianglesIndex += 6;
                                verticesIndex += 4;
                            }
                        }
                    }
                }

                meshIndex++;
            }
        }
    }
}

public struct ChunkMesh
{
    public BlockType blockType;
    public Vector3[] vertices;
    public int[] triangles;
    
    public ChunkMesh(BlockType blockType, Vector3[] vertices, int[] triangles)
    {
        this.blockType = blockType;
        this.vertices = vertices;
        this.triangles = triangles;
    }
}