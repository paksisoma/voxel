using UnityEngine;
using static Constants;

public class Generation : MonoBehaviour
{
    private Chunk[,] loadedChunks;

    private void Awake()
    {
        loadedChunks = new Chunk[RENDER_DISTANCE_LENGTH, RENDER_DISTANCE_LENGTH];
    }

    void Update()
    {
        Vector3Int playerChunk = Player.Instance.chunkPosition;

        for (int x = -RENDER_DISTANCE; x <= RENDER_DISTANCE; x++)
        {
            for (int z = -RENDER_DISTANCE; z <= RENDER_DISTANCE; z++)
            {
                Vector3Int relativePosition = new Vector3Int((x + playerChunk.x) * CHUNK_SIZE_NO_PADDING, 0, (z + playerChunk.z) * CHUNK_SIZE_NO_PADDING);
                Vector3Int chunkPosition = new Vector3Int(relativePosition.x / CHUNK_SIZE_NO_PADDING, relativePosition.y / CHUNK_SIZE_NO_PADDING, relativePosition.z / CHUNK_SIZE_NO_PADDING);

                // Does this chunk already exist
                //if (loadedChunks.Cast<Chunk>().Any(chunk => chunk != null && chunk.chunkPosition == chunkPosition)) continue;
                if (ChunkExists(chunkPosition)) continue;

                int a, b;

                // Find a null or far chunk
                for (int i = 0; i < RENDER_DISTANCE_LENGTH; i++)
                {
                    for (int j = 0; j < RENDER_DISTANCE_LENGTH; j++)
                    {
                        if (loadedChunks[i, j] == null)
                        {
                            a = i;
                            b = j;
                            goto FoundNull;
                        }

                        if (loadedChunks[i, j].chunkPosition.x > playerChunk.x + RENDER_DISTANCE ||
                            loadedChunks[i, j].chunkPosition.x < playerChunk.x - RENDER_DISTANCE ||
                            loadedChunks[i, j].chunkPosition.z > playerChunk.z + RENDER_DISTANCE ||
                            loadedChunks[i, j].chunkPosition.z < playerChunk.z - RENDER_DISTANCE)
                        {
                            a = i;
                            b = j;
                            goto FoundFar;
                        }
                    }
                }

                // If there is no null or far chunk
                continue;

                // If there is no null but there is far
                FoundFar:

                // Destroy old chunk
                Destroy(loadedChunks[a, b]);

                // If there is null
                FoundNull:

                // Generate new chunk
                loadedChunks[a, b] = GenerateChunk(relativePosition, chunkPosition);
            }
        }
    }

    public bool ChunkExists(Vector3Int chunkPosition)
    {
        for (int i = 0; i < RENDER_DISTANCE_LENGTH; i++)
            for (int j = 0; j < RENDER_DISTANCE_LENGTH; j++)
                if (loadedChunks[i, j] != null && loadedChunks[i, j].chunkPosition == chunkPosition)
                    return true;

        return false;
    }

    private Chunk GenerateChunk(Vector3Int relativePosition, Vector3Int chunkPosition)
    {
        Chunk chunk = gameObject.AddComponent<Chunk>();
        chunk.Initialize(relativePosition, chunkPosition);

        for (byte x = 0; x < CHUNK_SIZE; x++)
        {
            for (byte z = 0; z < CHUNK_SIZE; z++)
            {
                byte height = Noise(relativePosition.x + x, relativePosition.z + z);

                for (byte y = 0; y < Mathf.Min(height, CHUNK_SIZE); y++)
                {
                    chunk.SetBlock(x, y, z, BlockType.Stone);
                }
            }
        }

        chunk.Render();

        return chunk;
    }

    byte Noise(int x, int y)
    {
        const byte surfaceBegin = 5;

        float height = 5f * GetNoiseValue(x, y, 30f);
        height = Mathf.Pow(height, 2);

        return (byte)Mathf.Round(height + surfaceBegin);
    }

    float GetNoiseValue(float x, float y, float frequency)
    {
        float a = x / frequency;
        float b = y / frequency;

        float height = Mathf.PerlinNoise(a, b);

        height = Mathf.Max(height, 0);
        height = Mathf.Min(height, 1);

        return height;
    }
}