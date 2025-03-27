using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Constants;
using static Utils;

public class World : MonoBehaviour
{
    public static World Instance { get; private set; }

    private Dictionary<Vector2Int, ChunkData> chunks;
    private List<Vector2Int> chunkQueue;

    private bool render = false;

    private int _renderDistance;
    public int renderDistance
    {
        get => _renderDistance;
        set
        {
            _renderDistance = value;

            if (_renderDistance > value)
                DestroyChunks();
        }
    }

    [Header("Chunk")]
    public GameObject chunkParent;

    [Header("Tree")]
    public GameObject treeObject;
    public GameObject treeParent;

    [Header("Rock")]
    public GameObject rockObject;
    public GameObject rockParent;

    [Header("Cloud")]
    public Material cloudMaterial;
    public GameObject cloudParent;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        chunks = new Dictionary<Vector2Int, ChunkData>();
        chunkQueue = new List<Vector2Int>();
        renderDistance = RENDER_DISTANCE;
    }

    private void Update()
    {
        if (render)
            return;

        Vector3Int playerChunk = Player.Instance.chunkPosition;

        // Generate near chunk if not exists
        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                Vector2Int chunkPosition = new Vector2Int(playerChunk.x + x, playerChunk.z + z);

                if (!chunks.ContainsKey(chunkPosition) && !chunkQueue.Contains(chunkPosition))
                    chunkQueue.Add(chunkPosition);
            }
        }

        // Remove far chunks
        List<Vector2Int> keysToRemove = new List<Vector2Int>();

        foreach ((Vector2Int chunkPosition, ChunkData verticalChunks) in chunks)
        {
            if (chunkPosition.x > playerChunk.x + renderDistance || chunkPosition.x < playerChunk.x - renderDistance || chunkPosition.y > playerChunk.z + renderDistance || chunkPosition.y < playerChunk.z - renderDistance)
            {
                // Destroy vertical chunks
                for (int i = 0; i < verticalChunks.chunks.Length; i++)
                    if (verticalChunks.chunks[i] != null)
                        Destroy(verticalChunks.chunks[i].mesh);

                // Destroy prefabs
                foreach (GameObject prefab in verticalChunks.prefabs.Values)
                    Destroy(prefab);

                // Destroy cloud
                Destroy(verticalChunks.cloud.mesh);

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

        foreach (Vector2Int chunkPosition in chunkQueue)
        {
            if (!chunks.ContainsKey(chunkPosition))
            {
                GenerateChunk(chunkPosition);
                await UniTask.Yield();
            }
        }

        render = false;
    }

    private void DestroyChunks()
    {
        // Destroy chunks
        foreach (Transform child in chunkParent.transform)
            Destroy(child.gameObject);

        // Destroy trees
        foreach (Transform child in treeParent.transform)
            Destroy(child.gameObject);

        // Destroy clouds
        foreach (Transform child in cloudParent.transform)
            Destroy(child.gameObject);
    }

    private void GenerateChunk(Vector2Int chunkPosition)
    {
        Vector2Int worldPosition = new Vector2Int(chunkPosition.x * CHUNK_SIZE_NO_PADDING, chunkPosition.y * CHUNK_SIZE_NO_PADDING);

        // Terrain map generation
        NativeArray<int> terrainMap = new NativeArray<int>(CHUNK_SIZE * CHUNK_SIZE, Allocator.Persistent);

        TerrainJob terrainJob = new TerrainJob
        {
            Map = terrainMap,
            Position = new int2(worldPosition.x, worldPosition.y),
        };

        var jobHandle1 = terrainJob.Schedule(CHUNK_SIZE * CHUNK_SIZE, 64);

        // Noise map generation for transition effects
        NativeArray<float> noiseMap = new NativeArray<float>(CHUNK_SIZE * CHUNK_SIZE, Allocator.Persistent);

        NoiseJob noiseJob = new NoiseJob
        {
            Map = noiseMap,
            MapWidth = CHUNK_SIZE,
            Position = new int2(worldPosition.x, worldPosition.y),
            Frequency = 5,
        };

        var jobHandle2 = noiseJob.Schedule(CHUNK_SIZE * CHUNK_SIZE, 64);

        // Noise map generation for tree
        int treeNoiseMapWidth = CHUNK_SIZE_NO_PADDING + (TREE_DENSITY * 2);
        NativeArray<float> treeNoiseMap = new NativeArray<float>(treeNoiseMapWidth * treeNoiseMapWidth, Allocator.Persistent);

        NoiseJob treeNoiseJob = new NoiseJob
        {
            Map = treeNoiseMap,
            MapWidth = treeNoiseMapWidth,
            Position = new int2(worldPosition.x, worldPosition.y),
            Frequency = 5,
        };

        var jobHandle3 = treeNoiseJob.Schedule(treeNoiseMapWidth * treeNoiseMapWidth, 64);

        // Forest map generation
        NativeArray<float> forestMap = new NativeArray<float>(CHUNK_SIZE_NO_PADDING * CHUNK_SIZE_NO_PADDING, Allocator.Persistent);

        NoiseJob forestJob = new NoiseJob
        {
            Map = forestMap,
            MapWidth = CHUNK_SIZE_NO_PADDING,
            Position = new int2(worldPosition.x + 10000, worldPosition.y + 10000),
            Frequency = 1000,
        };

        var jobHandle4 = forestJob.Schedule(CHUNK_SIZE_NO_PADDING * CHUNK_SIZE_NO_PADDING, 64);

        // Complete jobs
        JobHandle.CombineDependencies(jobHandle1, jobHandle2).Complete();
        JobHandle.CombineDependencies(jobHandle3, jobHandle4).Complete();

        // Tree map generation
        NativeArray<bool> treeMap = new NativeArray<bool>(CHUNK_SIZE_NO_PADDING * CHUNK_SIZE_NO_PADDING, Allocator.Persistent);

        PeakJob treeJob = new PeakJob
        {
            InputMap = treeNoiseMap,
            InputMapWidth = treeNoiseMapWidth,
            OutputMap = treeMap,
            OutputMapWidth = CHUNK_SIZE_NO_PADDING,
            Radius = TREE_DENSITY,
        };

        treeJob.Schedule(CHUNK_SIZE_NO_PADDING * CHUNK_SIZE_NO_PADDING, 64).Complete(); // Can't combine the job because treeNoiseMap is needed

        // Rock noise generation
        int rockNoiseMapWidth = CHUNK_SIZE_NO_PADDING + (TREE_DENSITY * 2);
        NativeArray<float> rockNoiseMap = new NativeArray<float>(rockNoiseMapWidth * rockNoiseMapWidth, Allocator.Persistent);

        NoiseJob rockNoiseJob = new NoiseJob
        {
            Map = rockNoiseMap,
            MapWidth = rockNoiseMapWidth,
            Position = new int2(worldPosition.x + 10000, worldPosition.y + 10000),
            Frequency = 5,
        };

        rockNoiseJob.Schedule(rockNoiseMapWidth * rockNoiseMapWidth, 64).Complete();

        // Rock map generation
        NativeArray<bool> rockMap = new NativeArray<bool>(CHUNK_SIZE_NO_PADDING * CHUNK_SIZE_NO_PADDING, Allocator.Persistent);

        PeakJob rockJob = new PeakJob
        {
            InputMap = rockNoiseMap,
            InputMapWidth = rockNoiseMapWidth,
            OutputMap = rockMap,
            OutputMapWidth = CHUNK_SIZE_NO_PADDING,
            Radius = TREE_DENSITY,
        };

        rockJob.Schedule(CHUNK_SIZE_NO_PADDING * CHUNK_SIZE_NO_PADDING, 64).Complete();

        // Vertical chunks
        Chunk[] verticalChunks = new Chunk[CHUNK_HEIGHT];

        int currentHeight = Mathf.Max(terrainMap.Max() / CHUNK_SIZE_NO_PADDING + 1, MIN_CHUNK_HEIGHT);

        for (int i = 0; i < currentHeight; i++)
        {
            Vector3Int position = new Vector3Int(worldPosition.x, i * CHUNK_SIZE_NO_PADDING, worldPosition.y);

            GameObject gameObject = new GameObject(chunkPosition.x + "/" + i + "/" + chunkPosition.y);
            gameObject.transform.position = position - new Vector3(1.5f, 1.5f, 1.5f); ;
            gameObject.transform.SetParent(chunkParent.transform);
            gameObject.isStatic = true;

            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();

            Mesh mesh = new Mesh();

            meshFilter.mesh = mesh;
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            // Terrain placement
            Chunk chunk = new Chunk(gameObject, mesh, meshFilter, meshRenderer, meshCollider);

            for (int j = 0; j < terrainMap.Length; j++)
            {
                int x = j / CHUNK_SIZE;
                int z = j % CHUNK_SIZE;
                int height = Mathf.Min(terrainMap[j] - (i * CHUNK_SIZE_NO_PADDING), CHUNK_SIZE);

                if (height <= 0)
                    continue;

                for (int y = 0; y < height - 1; y++) // Stone
                    chunk.AddSolid(x, y, z, 2);

                for (int y = terrainMap[j]; y < WATER_HEIGHT; y++) // Water
                    chunk.AddWater(x, y, z);

                // Biome
                bool snowBiome = false;

                int zPosition = position.z + z;

                if (zPosition > SNOW_BIOME_TRANSITION_START)
                {
                    if (zPosition > SNOW_BIOME_TRANSITION_END)
                    {
                        snowBiome = true;
                    }
                    else
                    {
                        float transitionValue = (SNOW_BIOME_TRANSITION_END - zPosition) / (float)(SNOW_BIOME_TRANSITION_END - SNOW_BIOME_TRANSITION_START);

                        if (noiseMap[j] > transitionValue)
                            snowBiome = true;
                        else
                            snowBiome = false;
                    }
                }

                if (snowBiome) // Snow biome
                {
                    if (terrainMap[j] < WATER_HEIGHT)
                        chunk.AddSolid(x, height - 1, z, 6); // Dirt
                    else
                        chunk.AddSolid(x, height - 1, z, 5); // Snow
                }
                else // Grass biome
                {
                    if (terrainMap[j] < WATER_HEIGHT + 1) // Sand
                    {
                        chunk.AddSolid(x, height - 1, z, 4);
                    }
                    else if (terrainMap[j] > MOUNTAIN_TRANSITION_START) // Mountain
                    {
                        if (terrainMap[j] > MOUNTAIN_TRANSITION_END) // Stone
                        {
                            chunk.AddSolid(x, height - 1, z, 2);
                        }
                        else // Grass, stone transition
                        {
                            float transitionValue = (MOUNTAIN_TRANSITION_END - terrainMap[j]) / (float)(MOUNTAIN_TRANSITION_END - MOUNTAIN_TRANSITION_START);

                            if (noiseMap[j] > transitionValue)
                                chunk.AddSolid(x, height - 1, z, 2);
                            else
                                chunk.AddSolid(x, height - 1, z, 1);
                        }
                    }
                    else // Grass
                    {
                        chunk.AddSolid(x, height - 1, z, 1);
                    }
                }
            }

            chunk.UpdateMesh();
            verticalChunks[i] = chunk;
        }

        // Prefabs placement
        Dictionary<Vector3Int, GameObject> prefabs = new Dictionary<Vector3Int, GameObject>();

        for (int i = 0; i < treeMap.Length; i++)
        {
            if (treeMap[i] && forestMap[i] > 0.5f) // Tree
            {
                int x = i / CHUNK_SIZE_NO_PADDING;
                int z = i % CHUNK_SIZE_NO_PADDING;

                int height = terrainMap[(x + 1) * CHUNK_SIZE + (z + 1)];

                int y = height % CHUNK_SIZE_NO_PADDING;

                // Place tree above water and under mountain
                if (height > WATER_HEIGHT && height < MOUNTAIN_TRANSITION_START)
                {
                    GameObject tree = Instantiate(treeObject, new Vector3(worldPosition.x + x, height - 1.5f, worldPosition.y + z), Quaternion.identity);
                    tree.transform.SetParent(treeParent.transform);
                    tree.isStatic = true;
                    StaticBatchingUtility.Combine(tree);
                    prefabs.Add(new Vector3Int(x, y, z), tree);
                }
            }
            else if (treeMap[i] == false && rockMap[i] && forestMap[i] > 0.5f) // Stone
            {
                int x = i / CHUNK_SIZE_NO_PADDING;
                int z = i % CHUNK_SIZE_NO_PADDING;

                int height = terrainMap[(x + 1) * CHUNK_SIZE + (z + 1)];

                int y = height % CHUNK_SIZE_NO_PADDING;

                // Place rock above water and under mountain
                if (height > WATER_HEIGHT && height < MOUNTAIN_TRANSITION_START)
                {
                    GameObject rock = Instantiate(rockObject, new Vector3(worldPosition.x + x, height - 1.5f, worldPosition.y + z), Quaternion.identity);
                    rock.transform.SetParent(rockParent.transform);
                    rock.isStatic = true;
                    rock.transform.Rotate(0f, (float)rockNoiseMap[i] * 180, 0f, Space.World);
                    StaticBatchingUtility.Combine(rock);

                    prefabs.Add(new Vector3Int(x + 1, y, z + 1), rock);
                }
            }
        }

        terrainMap.Dispose();
        noiseMap.Dispose();
        treeNoiseMap.Dispose();
        treeMap.Dispose();
        forestMap.Dispose();
        rockNoiseMap.Dispose();
        rockMap.Dispose();

        Cloud cloud = GenerateCloud(chunkPosition, new Vector3(worldPosition.x, CLOUD_HEIGHT, worldPosition.y) - new Vector3(1.5f, 1.5f, 1.5f));

        ChunkData chunkData = new ChunkData(verticalChunks, cloud, prefabs);
        chunks.Add(chunkPosition, chunkData);
    }

    private Cloud GenerateCloud(Vector2Int chunkPosition, Vector3 worldPosition)
    {
        int2 position = new int2(chunkPosition.x * CHUNK_SIZE_NO_PADDING, chunkPosition.y * CHUNK_SIZE_NO_PADDING);

        GameObject gameObject = new GameObject(chunkPosition.x + "/" + chunkPosition.y);
        gameObject.transform.position = worldPosition;
        gameObject.transform.SetParent(cloudParent.transform);
        gameObject.isStatic = true;

        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = cloudMaterial;

        Mesh mesh = new Mesh();

        meshFilter.mesh = mesh;
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        Cloud cloud = new Cloud(gameObject, mesh, meshFilter, meshRenderer, position);

        cloud.UpdateMap();
        cloud.UpdateMesh();

        return cloud;
    }

    public void SetBlock(Vector3Int worldPosition, byte id)
    {
        Vector3Int chunkPosition = WorldPositionToChunkPosition(worldPosition);
        Vector3Int relativePosition = WorldPositionToChunkRelativePosition(chunkPosition, worldPosition);

        Chunk chunk = GetChunk(chunkPosition);
        chunk.SetBlock(relativePosition, id);
        chunk.UpdateMesh();

        void UpdateNeighbourBlock(Vector3Int chunkPosition, Vector3Int relativePosition, byte id, int axis)
        {
            Vector3Int neighbourChunkPosition = chunkPosition;
            Vector3Int neighbourRelativePosition = relativePosition;

            if (relativePosition[axis] == 1) // Left
            {
                neighbourChunkPosition[axis]--;
                neighbourRelativePosition[axis] = CHUNK_SIZE_NO_PADDING + 1;
            }
            else if (relativePosition[axis] == 62) // Right
            {
                neighbourChunkPosition[axis]++;
                neighbourRelativePosition[axis] = 0;
            }
            else
            {
                return;
            }

            Chunk neighbourChunk = GetChunk(neighbourChunkPosition);
            neighbourChunk.SetBlock(neighbourRelativePosition, id);
            neighbourChunk.UpdateMesh();
        }

        UpdateNeighbourBlock(chunkPosition, relativePosition, id, 0); // X
        UpdateNeighbourBlock(chunkPosition, relativePosition, id, 1); // Y
        UpdateNeighbourBlock(chunkPosition, relativePosition, id, 2); // Z
    }

    public byte GetBlock(Vector3Int worldPosition)
    {
        Vector3Int chunkPosition = WorldPositionToChunkPosition(worldPosition);
        Vector3Int relativePosition = WorldPositionToChunkRelativePosition(chunkPosition, worldPosition);

        Chunk chunk = GetChunk(chunkPosition);

        return chunk.GetBlock(relativePosition);
    }

    public Chunk GetChunk(Vector3Int chunkPosition)
    {
        if (chunks.TryGetValue(new Vector2Int(chunkPosition.x, chunkPosition.z), out ChunkData chunk))
            return chunk.chunks[chunkPosition.y];
        else
            return null;
    }

    public bool IsValidChunk(Vector3Int chunkPosition)
    {
        if (chunks.TryGetValue(new Vector2Int(chunkPosition.x, chunkPosition.z), out ChunkData chunk))
            return chunk.chunks[chunkPosition.y] != null;
        else
            return false;
    }

    public int GetGroundPosition(Vector3Int worldPosition)
    {
        Vector3Int chunkPosition = WorldPositionToChunkPosition(worldPosition);
        Vector3Int relativePosition = WorldPositionToChunkRelativePosition(chunkPosition, worldPosition);

        Chunk chunk = GetChunk(chunkPosition);

        return chunk.GetGroundPosition(relativePosition) + (chunkPosition.y * CHUNK_SIZE_NO_PADDING);
    }

    public bool IsGround(Vector3Int position)
    {
        Vector3Int down = position + Vector3Int.down;

        return GetBlock(position) == 0 && GetBlock(down) != 0;
    }

    public GameObject GetPrefab(Vector3Int worldPosition)
    {
        Vector3Int chunkPosition = WorldPositionToChunkPosition(worldPosition);
        Vector3Int relativePosition = WorldPositionToChunkRelativePosition(chunkPosition, worldPosition);

        if (chunks.TryGetValue(new Vector2Int(chunkPosition.x, chunkPosition.z), out ChunkData chunk))
            if (chunk.prefabs.TryGetValue(relativePosition, out GameObject gameObject))
                return gameObject;

        return null;
    }

    public IEnumerable<Vector3Int> GetNeighbourBlocks(Vector3Int position)
    {
        // Cardinal
        Vector3Int[] cardinal = {
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(1, 0, 0)
        };
        bool[] hasCardinal = new bool[cardinal.Length];

        for (int y = -1; y <= 1; y++)
        {
            for (int i = 0; i < cardinal.Length; i++)
            {
                if (hasCardinal[i] == false)
                {
                    Vector3Int neighbour = position + new Vector3Int(cardinal[i].x, y, cardinal[i].z);
                    if (IsGround(neighbour))
                    {
                        hasCardinal[i] = true;
                        yield return neighbour;
                    }
                }
            }
        }

        // Diagonal
        var diagonals = new (int first, int second, Vector3Int offset)[]
        {
            (0, 2, new Vector3Int(-1, 0, 1)),
            (0, 3, new Vector3Int(1, 0, 1)),
            (1, 2, new Vector3Int(-1, 0, -1)),
            (1, 3, new Vector3Int(1, 0, -1))
        };

        for (int y = -1; y <= 1; y++)
        {
            foreach (var (first, second, offset) in diagonals)
            {
                if (hasCardinal[first] && hasCardinal[second])
                {
                    Vector3Int diagNeighbour = position + new Vector3Int(offset.x, y, offset.z);
                    if (IsGround(diagNeighbour))
                        yield return diagNeighbour;
                }
            }
        }
    }

    public List<Vector3Int> Pathfinding(Vector3Int startPosition, Vector3Int endPosition)
    {
        if (!IsGround(startPosition) || !IsValidChunk(WorldPositionToChunkPosition(startPosition)))
            throw new System.InvalidOperationException("Invalid start position.");

        if (!IsGround(endPosition) || !IsValidChunk(WorldPositionToChunkPosition(startPosition)))
            throw new System.InvalidOperationException("Invalid goal position.");

        List<Vector3Int> path = new List<Vector3Int>();

        Node startNode = new Node
        {
            Position = startPosition,
            Parent = null,
            G = 0,
            H = ManhattanDistance(startPosition, endPosition)
        };

        startNode.F = startNode.G + startNode.H;

        List<Node> openList = new List<Node> { startNode };
        HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();
        Dictionary<Vector3, Node> nodeDict = new Dictionary<Vector3, Node>() {
            { startNode.Position, startNode }
        };

        while (openList.Count > 0)
        {
            // Shortest F node
            Node current = openList.OrderBy(n => n.F).First();

            // Found end position
            if (current.Position == endPosition)
            {
                Node node = current;

                while (node.Parent != null)
                {
                    path.Add(node.Position);
                    node = node.Parent;
                }

                break;
            }

            // Swap current node from open to closed
            openList.Remove(current);
            closedSet.Add(current.Position);

            int g = current.G + 1;

            // Check neighbours
            foreach (Vector3Int position in GetNeighbourBlocks(current.Position))
            {
                // If already exist
                if (closedSet.Contains(position))
                    continue;

                if (nodeDict.TryGetValue(position, out Node node))
                {
                    if (node.G > g)
                    {
                        node.Parent = current;
                        node.G = g;
                        node.H = ManhattanDistance(position, endPosition);
                        node.F = node.G + node.H;
                    }
                }
                else
                {
                    node = new Node
                    {
                        Position = position,
                        Parent = current,
                        G = g,
                        H = ManhattanDistance(position, endPosition)
                    };

                    node.F = node.G + node.H;

                    openList.Add(node);
                    nodeDict.TryAdd(position, node);
                }
            }
        }

        path.Reverse();

        return path;
    }

    public struct ChunkData
    {
        public Chunk[] chunks;
        public Cloud cloud;
        public Dictionary<Vector3Int, GameObject> prefabs;

        public ChunkData(Chunk[] chunks, Cloud cloud, Dictionary<Vector3Int, GameObject> prefabs)
        {
            this.chunks = chunks;
            this.cloud = cloud;
            this.prefabs = prefabs;
        }
    }
}