using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Constants;

public class World : MonoBehaviour
{
    public Dictionary<Vector2Int, Chunk[]> chunks = new Dictionary<Vector2Int, Chunk[]>();
    public List<Vector2Int> chunkQueue = new List<Vector2Int>();

    private GameObject chunksParent;
    private bool render = false;

    private void Awake()
    {
        chunksParent = GameObject.Find("Chunks");
    }

    void Update()
    {
        if (render)
            return;

        Vector3Int playerChunk = Player.Instance.chunkPosition;

        // Generate near chunk if not exists
        for (int x = -RENDER_DISTANCE; x <= RENDER_DISTANCE; x++)
        {
            for (int z = -RENDER_DISTANCE; z <= RENDER_DISTANCE; z++)
            {
                Vector2Int chunkPosition = new Vector2Int(playerChunk.x + x, playerChunk.z + z);

                if (!chunks.ContainsKey(chunkPosition) && !chunkQueue.Contains(chunkPosition))
                    chunkQueue.Add(chunkPosition);
            }
        }

        // Remove far chunks
        List<Vector2Int> keysToRemove = new List<Vector2Int>();

        foreach ((Vector2Int chunkPosition, Chunk[] verticalChunks) in chunks)
        {
            if (chunkPosition.x > playerChunk.x + RENDER_DISTANCE || chunkPosition.x < playerChunk.x - RENDER_DISTANCE || chunkPosition.y > playerChunk.z + RENDER_DISTANCE || chunkPosition.y < playerChunk.z - RENDER_DISTANCE)
            {
                // Destroy meshes
                for (int i = 0; i < verticalChunks.Length; i++)
                    Destroy(verticalChunks[i].mesh);

                keysToRemove.Add(chunkPosition);
            }
        }

        foreach (var key in keysToRemove)
        {
            chunks.Remove(key);
            chunkQueue.Remove(key);
        }

        chunkQueue = chunkQueue.OrderBy(chunk => Vector2Int.Distance(chunk, new Vector2Int(playerChunk.x, playerChunk.z))).ToList();
        _ = RenderChunks();
    }

    async UniTask RenderChunks()
    {
        render = true;

        foreach (var chunkPosition in chunkQueue)
        {
            if (!chunks.ContainsKey(chunkPosition))
            {
                GenerateChunk(chunkPosition);
                await UniTask.Yield();
            }
        }

        render = false;
    }

    void OnApplicationQuit()
    {
        foreach (Transform child in chunksParent.transform)
            DestroyImmediate(child.gameObject);
    }

    void GenerateChunk(Vector2Int chunkPosition)
    {
        Vector2Int position = new Vector2Int(chunkPosition.x * CHUNK_SIZE_NO_PADDING, chunkPosition.y * CHUNK_SIZE_NO_PADDING);
        UnityEngine.Random.InitState(chunkPosition.x.GetHashCode() * chunkPosition.y.GetHashCode());

        // Height map
        NativeArray<int> heightMap = new NativeArray<int>(CHUNK_SIZE * CHUNK_SIZE, Allocator.Persistent);

        NoiseJob job = new NoiseJob
        {
            HeightMap = heightMap,
            RelativePosition = new int2(position.x, position.y),
        };

        job.Schedule(CHUNK_SIZE * CHUNK_SIZE, 64).Complete();

        // Init chunks
        int chunkHeight = Mathf.Max(heightMap.Max() / CHUNK_SIZE_NO_PADDING + 1, MIN_CHUNK_HEIGHT);
        Chunk[] vChunks = new Chunk[chunkHeight];

        for (int i = 0; i < chunkHeight; i++)
        {
            GameObject gameObject = new GameObject();
            gameObject.transform.position = new Vector3(position.x, i * CHUNK_SIZE_NO_PADDING, position.y);
            gameObject.transform.SetParent(chunksParent.transform);
            gameObject.isStatic = true;

            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();

            Mesh mesh = new Mesh();

            meshFilter.mesh = mesh;
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            Chunk chunk = new Chunk(gameObject, mesh, meshFilter, meshRenderer, meshCollider);

            // Set blocks
            for (int j = 0; j < heightMap.Length; j++)
            {
                int x = j / CHUNK_SIZE;
                int z = j % CHUNK_SIZE;

                int height = Mathf.Min(heightMap[j] - (i * CHUNK_SIZE_NO_PADDING), CHUNK_SIZE);

                if (height > 0)
                {
                    for (int y = 0; y < height - 1; y++)
                        chunk.SetBlock(x, y, z, 2); // Stone

                    if (heightMap[j] < WATER_HEIGHT + 1)
                    {
                        chunk.SetBlock(x, height - 1, z, 4); // Sand
                    }
                    else
                    {
                        if (heightMap[j] > 115)
                        {
                            bool stone = Mathf.Abs(115 - heightMap[j]) >= UnityEngine.Random.Range(0, 20);

                            if (stone)
                                chunk.SetBlock(x, height - 1, z, 2); // Stone
                            else
                                chunk.SetBlock(x, height - 1, z, 1); // Grass
                        }
                        else
                        {
                            chunk.SetBlock(x, height - 1, z, 1); // Grass
                        }
                    }
                }

                if (i == 0)
                    for (int y = height; y < WATER_HEIGHT; y++)
                        chunk.SetWater(x, y, z); // Water
            }

            chunk.UpdateMesh();
            vChunks[i] = chunk;
        }

        heightMap.Dispose();

        chunks.Add(chunkPosition, vChunks);
    }

    //[BurstCompile]
    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    private struct NoiseJob : IJobParallelFor
    {
        [WriteOnly]
        public NativeArray<int> HeightMap;

        [ReadOnly]
        public int2 RelativePosition;

        public void Execute(int i)
        {
            float2 position = new float2(i / CHUNK_SIZE + RelativePosition.x, i % CHUNK_SIZE + RelativePosition.y);

            // Mountains
            float noise1000 = Noise(position, 1000);

            float a = 1f * Noise(position, 5000) +
                            1f * Noise(position, 2000) +
                            1f * noise1000 * Noise(position, 500) +
                            0.5f * noise1000 * Noise(position, 100) +
                            0.25f * noise1000 * Noise(position, 50) +
                            0.1f * noise1000 * Noise(position, 30);

            a /= 1f + 1f + 1f + 0.5f + 0.25f + 0.1f;
            a = math.pow(a, 4f);

            // Hills
            float2 position10000 = position + 10000;

            float b = 1f * Noise(position10000, 1000) +
                    0.5f * Noise(position10000, 500) +
                    0.25f * Noise(position10000, 250) +
                    0.1f * Noise(position10000, 250) * Noise(position10000, 50);

            b /= 1f + 0.5f + 0.25f + 0.1f;
            b *= 0.5f;

            // Height map
            a += b;

            HeightMap[i] = Mathf.RoundToInt(a * (HIGHEST_BLOCK - 2)) + 2;
        }

        public float Noise(float2 position, float frequency)
        {
            position /= frequency;
            return (noise.snoise(position) + 1) / 2;
        }
    }
}
