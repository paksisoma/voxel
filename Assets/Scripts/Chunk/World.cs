using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
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
                    CreateVerticalChunk(chunkPosition).Forget();
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

    private async UniTask CreateVerticalChunk(Vector2Int chunkPosition)
    {
        Vector3Int relativePosition = new Vector3Int(chunkPosition.x * CHUNK_SIZE_NO_PADDING, 0, chunkPosition.y * CHUNK_SIZE_NO_PADDING);

        // Height map
        int[,] heightMap = new int[CHUNK_SIZE, CHUNK_SIZE];
        int heightMapMax = 0;

        Parallel.For(0, CHUNK_SIZE, x =>
        {
            for (int z = 0; z < CHUNK_SIZE; z++)
            {
                int height = Noise(relativePosition.x + x, relativePosition.z + z);
                heightMap[x, z] = height;

                int initialMax;
                do
                {
                    initialMax = heightMapMax;
                    if (height <= initialMax) break;
                }
                while (Interlocked.CompareExchange(ref heightMapMax, height, initialMax) != initialMax);
            }
        });

        await UniTask.Yield();

        // Generate chunks
        int chunkHeight = (heightMapMax / CHUNK_SIZE_NO_PADDING) + 1;

        for (int i = 0; i < chunkHeight; i++)
        {
            int relativeHeight = CHUNK_SIZE_NO_PADDING * i;

            Chunk chunk = new Chunk(new Vector3Int(relativePosition.x, relativeHeight, relativePosition.z));

            await UniTask.RunOnThreadPool(() =>
            {
                for (int x = 0; x < CHUNK_SIZE; x++)
                {
                    for (int z = 0; z < CHUNK_SIZE; z++)
                    {
                        int height = Mathf.Min(heightMap[x, z] - relativeHeight, CHUNK_SIZE);

                        for (int y = 0; y < height; y++)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Stone);
                        }
                    }
                }

                chunk.CalculateMeshData();
            });

            // Add chunk to chunks dictionary and create meshes
            if (chunks.ContainsKey(chunkPosition) && !chunks[chunkPosition].ContainsKey(i - 1))
            {
                chunks[chunkPosition].Add(i - 1, chunk);

                foreach (MeshData meshData in chunk.meshData)
                    if (meshData.vertices.Length > 0)
                        chunk.gameObjects[meshData.blockType] = CreateObject(meshData);
            }

            await UniTask.Yield();
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

    private int Noise(int x, int y)
    {
        x += 10000;
        y += 10000;

        float a = GetNoiseValue(x, y, 200f, 7f);
        a += Mathf.PerlinNoise(x, y) * 0.25f * GetNoiseValue(x, y, 50f, 7f);

        a = Mathf.Pow(a, 2.5f);

        return Mathf.RoundToInt(a) + 10;
    }

    private float GetNoiseValue(float x, float y, float frequency, float strength)
    {
        float a = x / frequency;
        float b = y / frequency;

        float height = Mathf.PerlinNoise(a, b);

        height = Mathf.Max(height, 0);
        height = Mathf.Min(height, 1);

        return height * strength;
    }
}