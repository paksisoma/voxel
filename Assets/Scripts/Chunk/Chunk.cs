using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Mathematics;
using UnityEngine;
using static Constants;

public class Chunk : MonoBehaviour
{
    private BlockType[,,] typeBlocks = new BlockType[CHUNK_SIZE, CHUNK_SIZE, CHUNK_SIZE];
    private ulong[,,] binaryBlocks = new ulong[3, CHUNK_SIZE, CHUNK_SIZE];

    private Dictionary<BlockType, ChunkRenderer> renderers = new Dictionary<BlockType, ChunkRenderer>();

    public Vector3Int relativePosition { get; private set; }
    public Vector3Int chunkPosition { get; private set; }

    public void Initialize(Vector3Int relativePosition, Vector3Int chunkPosition)
    {
        this.relativePosition = relativePosition;
        this.chunkPosition = chunkPosition;
    }

    void OnDestroy()
    {
        foreach (var render in renderers)
            Destroy(render.Value);
    }

    public void SetBlock(byte x, byte y, byte z, BlockType blockType)
    {
        typeBlocks[x, y, z] = blockType;
        binaryBlocks[0, z, x] |= 1ul << y;
        binaryBlocks[1, y, z] |= 1ul << x;
        binaryBlocks[2, y, x] |= 1ul << z;
    }

    public BlockType GetBlockType(byte x, byte y, byte z)
    {
        return typeBlocks[x, y, z];
    }

    public BlockType GetBlockType(Vector3Byte position)
    {
        return typeBlocks[position.x, position.y, position.z];
    }

    public void Render()
    {
        // STOPWATCH START

        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();

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

                        if (!renderers.TryGetValue(blockType, out ChunkRenderer renderer))
                        {
                            renderers[blockType] = renderer = gameObject.AddComponent<ChunkRenderer>();
                            renderers[blockType].Initialize(blockType, relativePosition.x, relativePosition.y, relativePosition.z);
                        }

                        renderers[blockType].AddData(axis, x, y, z);
                    }
                }
            }
        }

        // CREATE MESHES

        foreach (ChunkRenderer renderer in renderers.Values)
            renderer.Render();

        // STOPWATCH END

        stopWatch.Stop();
        TimeSpan ts = stopWatch.Elapsed;
        UnityEngine.Debug.Log("Chunk: " + ts.Milliseconds + "ms");
    }
}