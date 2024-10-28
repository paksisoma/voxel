using System.Collections.Generic;
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
                    CreateVerticalChunk(chunkPosition);
                }
            }
        }

        // Remove far chunks
        var keysToRemove = new List<Vector2Int>();

        foreach (var chunkEntry in chunks)
        {
            var chunkKey = chunkEntry.Key;

            if (chunkKey.x > playerChunk.x + RENDER_DISTANCE || chunkKey.x < playerChunk.x - RENDER_DISTANCE || chunkKey.y > playerChunk.z + RENDER_DISTANCE || chunkKey.y < playerChunk.z - RENDER_DISTANCE)
            {
                foreach (var gameObjectPair in chunkEntry.Value.Values)
                {
                    foreach (BlockType blockType in gameObjectPair.gameObjects.Keys)
                        Destroy(gameObjectPair.gameObjects[blockType]);

                    gameObjectPair.gameObjects.Clear();
                }

                keysToRemove.Add(chunkKey);
            }
        }

        foreach (var key in keysToRemove)
            chunks.Remove(key);
    }

    async void CreateVerticalChunk(Vector2Int chunkPosition)
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

        if (chunks.ContainsKey(chunkPosition))
        {
            foreach ((int y, Chunk chunk) in verticalChunks)
            {
                if (chunks[chunkPosition].ContainsKey(y))
                    return;

                foreach (MeshData meshData in chunk.meshData)
                    chunk.gameObjects[meshData.blockType] = CreateObject(meshData);

                chunks[chunkPosition].Add(y, chunk);
            }
        }
    }

    private GameObject CreateObject(MeshData meshes)
    {
        GameObject gameObject = new GameObject(meshes.blockType.ToString());

        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.vertices = meshes.vertices;
        mesh.triangles = meshes.triangles;

        mesh.RecalculateNormals();

        meshRenderer.material = BlockData.BlockProperties[meshes.blockType].material;
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;

        return gameObject;
    }

    int Noise(int x, int y)
    {
        x += 10000;
        y += 10000;

        float a = GetNoiseValue(x, y, 200f, 7f);
        a += Mathf.PerlinNoise(x, y) * 0.25f * GetNoiseValue(x, y, 50f, 7f);

        a = Mathf.Pow(a, 2.5f);

        return Mathf.RoundToInt(a) + 10;
    }

    float GetNoiseValue(float x, float y, float frequency, float strength)
    {
        float a = x / frequency;
        float b = y / frequency;

        float height = Mathf.PerlinNoise(a, b);

        height = Mathf.Max(height, 0);
        height = Mathf.Min(height, 1);

        return height * strength;
    }
}