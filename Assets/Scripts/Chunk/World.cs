using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static Constants;

public class World : MonoBehaviour
{
    private Dictionary<Vector2Int, Dictionary<int, Chunk>> chunks;

    private void Awake()
    {
        chunks = new Dictionary<Vector2Int, Dictionary<int, Chunk>>();
    }

    private void Update()
    {
        Vector3Int playerChunk = Player.Instance.chunkPosition;

        // Generate near chunk if not exists
        for (int x = -RENDER_DISTANCE; x <= RENDER_DISTANCE; x++)
        {
            for (int z = -RENDER_DISTANCE; z <= RENDER_DISTANCE; z++)
            {
                Vector2Int chunkPosition = new Vector2Int(playerChunk.x + x, playerChunk.z + z);

                if (!chunks.ContainsKey(chunkPosition))
                {
                    chunks.Add(chunkPosition, new Dictionary<int, Chunk>());
                    GenerateVerticalChunk(chunkPosition);
                }
            }
        }

        // Remove far chunks
        Vector2Int[] chunkKeys = chunks.Keys.ToArray();

        for (int i = 0; i < chunkKeys.Length; i++)
        {
            if (chunkKeys[i].x > playerChunk.x + RENDER_DISTANCE || chunkKeys[i].x < playerChunk.x - RENDER_DISTANCE || chunkKeys[i].y > playerChunk.z + RENDER_DISTANCE || chunkKeys[i].y < playerChunk.z - RENDER_DISTANCE)
            {
                foreach (var chunk in chunks[chunkKeys[i]])
                {
                    if (chunk.Value.gameObject)
                    {
                        Destroy(chunk.Value.gameObject);
                        chunk.Value.gameObject = null;
                    }
                }

                chunks.Remove(chunkKeys[i]);
            }
        }
    }

    async void GenerateVerticalChunk(Vector2Int chunkPosition)
    {
        Dictionary<int, Chunk> verticalChunks = new Dictionary<int, Chunk>();

        await Task.Run(() =>
        {
            Vector3Int relativePosition = new Vector3Int(chunkPosition.x * CHUNK_SIZE_NO_PADDING, 0, chunkPosition.y * CHUNK_SIZE_NO_PADDING);

            for (byte x = 0; x < CHUNK_SIZE; x++)
            {
                for (byte z = 0; z < CHUNK_SIZE; z++)
                {
                    int height = Noise(relativePosition.x + x, relativePosition.z + z);
                    byte chunkY = 0;

                    relativePosition.y = 0;

                    while (height > 0)
                    {
                        byte y = 0;
                        int maxBlocks = Mathf.Min(height, CHUNK_SIZE);

                        if (!verticalChunks.ContainsKey(chunkY))
                            verticalChunks.Add(chunkY, new Chunk(relativePosition));

                        Chunk chunk = verticalChunks[chunkY];

                        while (y < maxBlocks)
                            chunk.SetBlock(x, y++, z, BlockType.Stone);

                        height -= maxBlocks;
                        relativePosition.y += CHUNK_SIZE_NO_PADDING;
                        chunkY++;

                        if (maxBlocks == CHUNK_SIZE)
                            height += 2;
                    }
                }
            }

            // Calculate mesh data
            foreach (Chunk chunk in verticalChunks.Values)
                chunk.CalculateMeshData();
        });

        // Create object on the main thread
        if (chunks.ContainsKey(chunkPosition))
        {
            foreach (Chunk chunk in chunks[chunkPosition].Values)
                if (chunk.gameObject)
                    return;

            chunks[chunkPosition].Clear();

            foreach (var chunk in verticalChunks)
            {
                chunks[chunkPosition].Add(chunk.Key, chunk.Value);
                chunk.Value.gameObject = CreateObject(chunk.Value.meshes);
            }
        }
    }

    private GameObject CreateObject(ChunkMesh[] meshes)
    {
        GameObject parentObject = new GameObject("Chunk");

        for (int i = 0; i < meshes.Length; i++)
        {
            ChunkMesh chunkMesh = meshes[i];

            GameObject childObject = new GameObject(chunkMesh.blockType.ToString());
            childObject.transform.parent = parentObject.transform;

            MeshFilter meshFilter = childObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = childObject.AddComponent<MeshRenderer>();
            MeshCollider meshCollider = childObject.AddComponent<MeshCollider>();

            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.vertices = chunkMesh.vertices;
            mesh.triangles = chunkMesh.triangles;

            mesh.RecalculateNormals();

            meshRenderer.material = BlockData.BlockProperties[chunkMesh.blockType].material;
            meshFilter.mesh = mesh;
            meshCollider.sharedMesh = mesh;
        }

        return parentObject;
    }

    int Noise(int x, int y)
    {
        const int surfaceBegin = 5;
        float height = 5f * GetNoiseValue(x, y, 30f);
        height = Mathf.Pow(height, 2);
        return Mathf.RoundToInt(height + surfaceBegin);
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