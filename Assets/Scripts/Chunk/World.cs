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
    public static Dictionary<Vector2Int, Chunk[]> chunks;
    private List<Vector2Int> chunkQueue;

    private bool render = false;

    [Header("Chunk")]
    public GameObject chunkParent;

    [Header("Tree")]
    public GameObject treeObject;
    public GameObject treeParent;

    private void Awake()
    {
        chunks = new Dictionary<Vector2Int, Chunk[]>();
        chunkQueue = new List<Vector2Int>();
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
                // Destroy
                for (int i = 0; i < verticalChunks.Length; i++)
                {
                    Destroy(verticalChunks[i].mesh);

                    foreach (GameObject prefab in verticalChunks[i].prefabs)
                        Destroy(prefab);
                }

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

    public static void SetBlock(int x, int y, int z, int id)
    {
        // Chunk position
        Vector3Int chunkPosition = new Vector3Int(x, y, z);

        if (chunkPosition.x < 0)
            chunkPosition.x -= CHUNK_SIZE_NO_PADDING - 1;

        if (chunkPosition.z < 0)
            chunkPosition.z -= CHUNK_SIZE_NO_PADDING - 1;

        chunkPosition /= CHUNK_SIZE_NO_PADDING;

        // Block position
        Vector3Int blockPosition = (new Vector3Int(x, y, z) - (chunkPosition * CHUNK_SIZE_NO_PADDING)) + new Vector3Int(1, 1, 1);

        // Chunk
        Chunk chunk = GetChunk(chunkPosition);
        chunk.SetBlock(blockPosition.x, blockPosition.y, blockPosition.z, id);
        chunk.UpdateMesh();

        // Neighbour
        void UpdateNeighbourBlock(Vector3Int chunkPosition, Vector3Int blockPosition, int id, int axis)
        {
            Vector3Int neighbourChunkPosition = chunkPosition;
            Vector3Int neighbourBlockPosition = blockPosition;

            if (blockPosition[axis] == 1) // Left
            {
                neighbourChunkPosition[axis]--;
                neighbourBlockPosition[axis] = CHUNK_SIZE_NO_PADDING + 1;
            }
            else if (blockPosition[axis] == 62) // Right
            {
                neighbourChunkPosition[axis]++;
                neighbourBlockPosition[axis] = 0;
            }
            else
            {
                return;
            }

            Chunk neighbourChunk = GetChunk(neighbourChunkPosition);
            neighbourChunk.SetBlock(neighbourBlockPosition.x, neighbourBlockPosition.y, neighbourBlockPosition.z, id);
            neighbourChunk.UpdateMesh();
        }

        UpdateNeighbourBlock(chunkPosition, blockPosition, id, 0); // X
        UpdateNeighbourBlock(chunkPosition, blockPosition, id, 1); // Y
        UpdateNeighbourBlock(chunkPosition, blockPosition, id, 2); // Z
    }

    public static int GetBlock(int x, int y, int z)
    {
        // Chunk position
        Vector3Int chunkPosition = new Vector3Int(x, y, z);

        if (chunkPosition.x < 0)
            chunkPosition.x -= CHUNK_SIZE_NO_PADDING - 1;

        if (chunkPosition.z < 0)
            chunkPosition.z -= CHUNK_SIZE_NO_PADDING - 1;

        chunkPosition /= CHUNK_SIZE_NO_PADDING;

        // Block position
        Vector3Int blockPosition = (new Vector3Int(x, y, z) - (chunkPosition * CHUNK_SIZE_NO_PADDING)) + new Vector3Int(1, 1, 1);

        // Chunk
        Chunk chunk = GetChunk(chunkPosition);
        return chunk.GetBlock(blockPosition.x, blockPosition.y, blockPosition.z);
    }

    private static Chunk GetChunk(Vector3Int position)
    {
        Vector2Int chunkKey = new Vector2Int(position.x, position.z);

        if (chunks.ContainsKey(chunkKey))
            return chunks[chunkKey][position.y];

        return null;
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

    /*void OnDestroy()
    {
        // Destroy chunks
        foreach (Transform child in chunkParent.transform)
            DestroyImmediate(child.gameObject);

        // Destroy trees
        foreach (Transform child in treeParent.transform)
            DestroyImmediate(child.gameObject);
    }*/

    void GenerateChunk(Vector2Int chunkPosition)
    {
        Vector2Int position = new Vector2Int(chunkPosition.x * CHUNK_SIZE_NO_PADDING, chunkPosition.y * CHUNK_SIZE_NO_PADDING);
        UnityEngine.Random.InitState(chunkPosition.x.GetHashCode() * chunkPosition.y.GetHashCode());

        NativeArray<int> heightMap = new NativeArray<int>(CHUNK_SIZE * CHUNK_SIZE, Allocator.Persistent); // Height map
        NativeArray<float> blueNoise = new NativeArray<float>(CHUNK_SIZE * CHUNK_SIZE, Allocator.Persistent); // Blue noise
        NativeArray<bool> snowMap = new NativeArray<bool>(CHUNK_SIZE * CHUNK_SIZE, Allocator.Persistent); // Snow map

        NoiseJob noiseJob = new NoiseJob
        {
            HeightMap = heightMap,
            BlueNoise = blueNoise,
            SnowMap = snowMap,
            RelativePosition = new int2(position.x, position.y),
        };

        noiseJob.Schedule(CHUNK_SIZE * CHUNK_SIZE, 64).Complete();

        // Tree map
        NativeArray<bool> treeMap = new NativeArray<bool>(CHUNK_SIZE * CHUNK_SIZE, Allocator.Persistent);
        TreeJob treeJob = new TreeJob { BlueNoise = blueNoise, TreeMap = treeMap };
        treeJob.Schedule(CHUNK_SIZE * CHUNK_SIZE, 64).Complete();
        blueNoise.Dispose();

        // Init chunks
        int chunkHeight = Mathf.Max(heightMap.Max() / CHUNK_SIZE_NO_PADDING + 1, MIN_CHUNK_HEIGHT);
        Chunk[] vChunks = new Chunk[chunkHeight];

        for (int i = 0; i < chunkHeight; i++)
        {
            Vector3 relativePosition = new Vector3(position.x, i * CHUNK_SIZE_NO_PADDING, position.y) - new Vector3(1.5f, 1.5f, 1.5f);

            GameObject gameObject = new GameObject(chunkPosition.x + "/" + i + "/" + chunkPosition.y);
            gameObject.transform.position = relativePosition;
            gameObject.transform.SetParent(chunkParent.transform);
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
                    // Snow biome
                    if (snowMap[j])
                    {
                        for (int y = 0; y < height - 1; y++)
                            chunk.AddSolid(x, y, z, 2); // Stone

                        // Under water
                        if (heightMap[j] < WATER_HEIGHT)
                            chunk.AddSolid(x, height - 1, z, 6); // Dirt
                        else
                            chunk.AddSolid(x, height - 1, z, 5); // Snow
                    }
                    else // Normal biome
                    {
                        for (int y = 0; y < height - 1; y++)
                            chunk.AddSolid(x, y, z, 2); // Stone

                        if (heightMap[j] < WATER_HEIGHT + 1)
                        {
                            chunk.AddSolid(x, height - 1, z, 4); // Sand
                        }
                        else
                        {
                            if (heightMap[j] > MOUNTAIN_HEIGHT_START)
                            {
                                bool stone = Mathf.Abs(MOUNTAIN_HEIGHT_START - heightMap[j]) >= UnityEngine.Random.Range(0, 20);

                                if (stone)
                                    chunk.AddSolid(x, height - 1, z, 2); // Stone
                                else
                                    chunk.AddSolid(x, height - 1, z, 1); // Grass
                            }
                            else
                            {
                                chunk.AddSolid(x, height - 1, z, 1); // Grass
                            }
                        }
                    }
                }

                if (i == 0)
                {
                    // Tree
                    if (treeMap[j] && heightMap[j] < MOUNTAIN_HEIGHT_START && heightMap[j] > WATER_HEIGHT)
                    {
                        GameObject tree = Instantiate(treeObject, relativePosition + new Vector3(x, heightMap[j], z), Quaternion.identity);
                        tree.transform.SetParent(treeParent.transform);
                        tree.isStatic = true;
                        StaticBatchingUtility.Combine(tree);
                        chunk.prefabs.Add(tree);
                    }

                    // Water
                    for (int y = height; y < WATER_HEIGHT; y++)
                        chunk.AddWater(x, y, z);
                }

            }

            chunk.UpdateMesh();
            vChunks[i] = chunk;
        }

        heightMap.Dispose();
        treeMap.Dispose();
        snowMap.Dispose();

        chunks.Add(chunkPosition, vChunks);
    }

    [BurstCompile]
    //[BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    private struct NoiseJob : IJobParallelFor
    {
        [WriteOnly]
        public NativeArray<int> HeightMap;

        [WriteOnly]
        public NativeArray<float> BlueNoise;

        [WriteOnly]
        public NativeArray<bool> SnowMap;

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

            // Blue noise
            float c = 0;

            if (Noise(position10000, 1000) > 0.5f)
                c = Noise(position10000, 1);

            BlueNoise[i] = c;

            // Snow map
            if (position.y > Noise(position, 10) * 50 + SNOW_BIOME_START)
                SnowMap[i] = true;
        }

        public float Noise(float2 position, float frequency)
        {
            position /= frequency;
            return (noise.snoise(position) + 1) / 2;
        }
    }

    //[BurstCompile]
    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    private struct TreeJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<float> BlueNoise;

        [WriteOnly]
        public NativeArray<bool> TreeMap;

        public void Execute(int i)
        {
            if (BlueNoise[i] == 0)
                return;

            int xc = i % CHUNK_SIZE;
            int yc = i / CHUNK_SIZE;

            int radius = 10;

            float max = 0;

            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int xn = dx + xc;
                    int yn = dy + yc;

                    int index = yn * CHUNK_SIZE + xn;

                    if (index >= 0 && index < BlueNoise.Length)
                    {
                        float e = BlueNoise[yn * CHUNK_SIZE + xn];

                        if (e > max)
                            max = e;
                    }
                }
            }

            if (BlueNoise[yc * CHUNK_SIZE + xc] == max)
                TreeMap[yc * CHUNK_SIZE + xc] = true;
        }
    }
}
