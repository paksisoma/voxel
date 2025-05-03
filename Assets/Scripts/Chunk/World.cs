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

    public Unity.Mathematics.Random random;

    private Dictionary<Vector2Int, ChunkData> chunks;
    private List<Vector2Int> chunkQueue;

    private bool render = false;

    public int renderDistance;

    [Header("Chunk")]
    public GameObject chunkParent;

    [Header("Cloud")]
    public Material cloudMaterial;
    public GameObject cloudParent;

    public GameObject specialsParent;
    private GameObject treeObject;
    private GameObject rockObject;
    private GameObject stickObject;

    [Header("NPC")]
    public GameObject npcParent;
    public GameObject predator;
    public GameObject prey;

    private StorageWorld storageWorld;
    private StorageCharacter storageCharacter;
    private bool initPlayer = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        chunks = new Dictionary<Vector2Int, ChunkData>();
        chunkQueue = new List<Vector2Int>();

        treeObject = ((Special)Items.Instance.items[106]).gameObject;
        rockObject = ((Special)Items.Instance.items[103]).gameObject;
        stickObject = ((Special)Items.Instance.items[104]).gameObject;

        random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);

        LoadWorld();
        LoadCharacter();
    }

    private void Update()
    {
        renderDistance = Settings.Instance.renderDistance;

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

        foreach ((Vector2Int chunkPosition, _) in chunks)
        {
            if (chunkPosition.x > playerChunk.x + renderDistance || chunkPosition.x < playerChunk.x - renderDistance || chunkPosition.y > playerChunk.z + renderDistance || chunkPosition.y < playerChunk.z - renderDistance)
            {
                DestroyVerticalChunk(chunkPosition);
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

    private void OnApplicationQuit()
    {
        Save();
    }

    public void Save()
    {
        foreach ((Vector2Int chunkPosition, _) in chunks)
            DestroyVerticalChunk(chunkPosition);

        SaveWorld();
        SaveCharacter();
    }

    private void SaveWorld()
    {
        Storage.SetWorld(storageWorld);
    }

    private void LoadWorld()
    {
        storageWorld = Storage.GetWorld();
        Seed.seed = storageWorld.seed;
    }

    private void SaveCharacter()
    {
        storageCharacter.health = Player.Instance.health;
        storageCharacter.thirst = Player.Instance.thirst;
        storageCharacter.hunger = Player.Instance.hunger;
        storageCharacter.temperature = Player.Instance.temperature;
        storageCharacter.position = Player.Instance.transform.position;
        storageCharacter.rotation = Player.Instance.transform.eulerAngles.y;

        storageCharacter.yaw = ThirdPersonCamera.Instance.yaw;
        storageCharacter.pitch = ThirdPersonCamera.Instance.pitch;

        Storage.SetCharacter(storageCharacter);
    }

    private void LoadCharacter()
    {
        storageCharacter = Storage.GetCharacter();

        Player.Instance.health = storageCharacter.health;
        Player.Instance.thirst = storageCharacter.thirst;
        Player.Instance.hunger = storageCharacter.hunger;
        Player.Instance.temperature = storageCharacter.temperature;
        Player.Instance.transform.rotation = Quaternion.Euler(0, storageCharacter.rotation, 0);
        Player.Instance.WarpPlayer(storageCharacter.position);

        ThirdPersonCamera.Instance.yaw = storageCharacter.yaw;
        ThirdPersonCamera.Instance.pitch = storageCharacter.pitch;
    }

    // It doesn't remove the chunk from the dictionary, only destroy the chunks
    private void DestroyVerticalChunk(Vector2Int chunkPosition)
    {
        ChunkData verticalChunk = chunks[chunkPosition];

        // Destroy vertical chunks
        for (int i = 0; i < verticalChunk.chunks.Length; i++)
        {
            if (verticalChunk.chunks[i] != null)
            {
                Destroy(verticalChunk.chunks[i].mesh);

                // Save chunk changes
                List<StorageBlockData> blockChanges = verticalChunk.chunks[i].changes;

                if (blockChanges.Count > 0)
                    Storage.SaveBlocks(new Vector3Int(chunkPosition.x, i, chunkPosition.y), blockChanges);
            }
        }

        // Destroy specials
        foreach (GameObject special in verticalChunk.specials.Values)
            Destroy(special);

        // Save specials changes
        if (verticalChunk.specialChanges.Count > 0)
            Storage.SaveSpecials(chunkPosition, verticalChunk.specialChanges);

        // Destroy cloud
        Destroy(verticalChunk.cloud.mesh);
    }

    private async UniTask RenderChunks()
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

        // Load player after chunk generation
        if (!initPlayer)
        {
            if (storageWorld.visit == 0)
                Player.Instance.WarpPlayerUp(Vector2.zero);

            storageWorld.visit++;
            initPlayer = true;
        }

        render = false;
    }

    private void GenerateChunk(Vector2Int chunkPosition)
    {
        Vector2Int worldPosition = new Vector2Int(chunkPosition.x * CHUNK_SIZE_NO_PADDING, chunkPosition.y * CHUNK_SIZE_NO_PADDING);

        // Terrain map generation
        NativeArray<int> terrainMap = new NativeArray<int>(CHUNK_SIZE * CHUNK_SIZE, Allocator.Persistent);

        TerrainJob terrainJob = new TerrainJob
        {
            Map = terrainMap,
            Position1 = new int2(worldPosition.x, worldPosition.y) + Seed.offset1,
            Position2 = new int2(worldPosition.x, worldPosition.y) + Seed.offset2,
        };

        var jobHandle1 = terrainJob.Schedule(CHUNK_SIZE * CHUNK_SIZE, 64);

        // Noise map generation for transition effects
        NativeArray<float> noiseMap = new NativeArray<float>(CHUNK_SIZE * CHUNK_SIZE, Allocator.Persistent);

        NoiseJob noiseJob = new NoiseJob
        {
            Map = noiseMap,
            MapWidth = CHUNK_SIZE,
            Position = new int2(worldPosition.x, worldPosition.y) + Seed.offset3,
            Frequency = 5,
        };

        var jobHandle2 = noiseJob.Schedule(CHUNK_SIZE * CHUNK_SIZE, 64);

        // Noise map generation
        NativeArray<float> noiseMap2 = new NativeArray<float>(CHUNK_SIZE * CHUNK_SIZE, Allocator.Persistent);

        NoiseJob noiseJob2 = new NoiseJob
        {
            Map = noiseMap2,
            MapWidth = CHUNK_SIZE,
            Position = new int2(worldPosition.x, worldPosition.y) + Seed.offset4,
            Frequency = 5,
        };

        noiseJob2.Schedule(CHUNK_SIZE * CHUNK_SIZE, 64).Complete();

        // Noise map generation for tree
        int treeNoiseMapWidth = CHUNK_SIZE_NO_PADDING + (TREE_DENSITY * 2);
        NativeArray<float> treeNoiseMap = new NativeArray<float>(treeNoiseMapWidth * treeNoiseMapWidth, Allocator.Persistent);

        NoiseJob treeNoiseJob = new NoiseJob
        {
            Map = treeNoiseMap,
            MapWidth = treeNoiseMapWidth,
            Position = new int2(worldPosition.x, worldPosition.y) + Seed.offset5,
            Frequency = 5,
        };

        var jobHandle3 = treeNoiseJob.Schedule(treeNoiseMapWidth * treeNoiseMapWidth, 64);

        // Forest map generation
        NativeArray<float> forestMap = new NativeArray<float>(CHUNK_SIZE_NO_PADDING * CHUNK_SIZE_NO_PADDING, Allocator.Persistent);

        NoiseJob forestJob = new NoiseJob
        {
            Map = forestMap,
            MapWidth = CHUNK_SIZE_NO_PADDING,
            Position = new int2(worldPosition.x, worldPosition.y) + Seed.offset2,
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
            Position = new int2(worldPosition.x, worldPosition.y) + Seed.offset4,
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

        // Stick noise generation
        int stickNoiseMapWidth = CHUNK_SIZE_NO_PADDING + (TREE_DENSITY * 2);
        NativeArray<float> stickNoiseMap = new NativeArray<float>(rockNoiseMapWidth * rockNoiseMapWidth, Allocator.Persistent);

        NoiseJob stickNoiseJob = new NoiseJob
        {
            Map = stickNoiseMap,
            MapWidth = stickNoiseMapWidth,
            Position = new int2(worldPosition.x, worldPosition.y) + Seed.offset3,
            Frequency = 5,
        };

        stickNoiseJob.Schedule(stickNoiseMapWidth * stickNoiseMapWidth, 64).Complete();

        // Stick map generation
        NativeArray<bool> stickMap = new NativeArray<bool>(CHUNK_SIZE_NO_PADDING * CHUNK_SIZE_NO_PADDING, Allocator.Persistent);

        PeakJob stickJob = new PeakJob
        {
            InputMap = stickNoiseMap,
            InputMapWidth = stickNoiseMapWidth,
            OutputMap = stickMap,
            OutputMapWidth = CHUNK_SIZE_NO_PADDING,
            Radius = TREE_DENSITY,
        };

        stickJob.Schedule(CHUNK_SIZE_NO_PADDING * CHUNK_SIZE_NO_PADDING, 64).Complete();

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

                void AddMineral()
                {
                    int blockPositionZ = worldPosition.y + z;

                    if (blockPositionZ > DIAMOND_START_POSITION)
                        chunk.AddSolid(x, height - 1, z, 12);
                    else if (blockPositionZ > RUBY_START_POSITION)
                        chunk.AddSolid(x, height - 1, z, 10);
                    else if (blockPositionZ > GOLD_START_POSITION)
                        chunk.AddSolid(x, height - 1, z, 11);
                    else if (blockPositionZ > COPPER_START_POSITION)
                        chunk.AddSolid(x, height - 1, z, 9);
                    else if (blockPositionZ > IRON_START_POSITION)
                        chunk.AddSolid(x, height - 1, z, 8);
                    else
                        chunk.AddSolid(x, height - 1, z, 7);
                }

                if (snowBiome) // Snow biome
                {
                    if (terrainMap[j] < WATER_HEIGHT)
                        chunk.AddSolid(x, height - 1, z, 6); // Dirt
                    else
                    {
                        if (terrainMap[j] > MOUNTAIN_TRANSITION_START) // Mountain
                        {
                            if (noiseMap[j] > 0.05f)
                                chunk.AddSolid(x, height - 1, z, 5);
                            else
                                AddMineral();
                        }
                        else
                        {
                            chunk.AddSolid(x, height - 1, z, 5); // Snow
                        }
                    }
                }
                else // Grass biome
                {
                    if (terrainMap[j] < WATER_HEIGHT + 1) // Sand
                    {
                        chunk.AddSolid(x, height - 1, z, 4);
                    }
                    else if (terrainMap[j] > MOUNTAIN_TRANSITION_START) // Mountain
                    {
                        if (terrainMap[j] > MOUNTAIN_TRANSITION_END)
                        {
                            if (noiseMap[j] > 0.05f)
                                chunk.AddSolid(x, height - 1, z, 2);
                            else
                                AddMineral();
                        }
                        else // Grass, stone transition
                        {
                            float transitionValue = (MOUNTAIN_TRANSITION_END - terrainMap[j]) / (float)(MOUNTAIN_TRANSITION_END - MOUNTAIN_TRANSITION_START);

                            if (noiseMap[j] > transitionValue)
                            {
                                if (noiseMap[j] > 0.05f)
                                    chunk.AddSolid(x, height - 1, z, 2);
                                else
                                    AddMineral();
                            }
                            else
                            {
                                chunk.AddSolid(x, height - 1, z, 1);
                            }
                        }
                    }
                    else // Grass
                    {
                        chunk.AddSolid(x, height - 1, z, 1);
                    }
                }
            }

            //chunk.UpdateMesh();
            verticalChunks[i] = chunk;
        }

        // Specials placement
        Dictionary<Vector3Int, GameObject> specials = new Dictionary<Vector3Int, GameObject>();

        for (int i = 0; i < treeMap.Length; i++)
        {
            if (forestMap[i] > 0.5f)
            {
                if (treeMap[i]) // Tree
                {
                    int x = i / CHUNK_SIZE_NO_PADDING;
                    int z = i % CHUNK_SIZE_NO_PADDING;
                    int y = terrainMap[(x + 1) * CHUNK_SIZE + (z + 1)];

                    // Place tree above water and under mountain
                    if (y > WATER_HEIGHT && y < MOUNTAIN_TRANSITION_START)
                    {
                        Vector3Int objectPosition = new Vector3Int(worldPosition.x + x, y - 1, worldPosition.y + z);
                        GameObject tree = Instantiate(treeObject, objectPosition, Quaternion.identity);
                        tree.transform.SetParent(specialsParent.transform);
                        tree.isStatic = true;
                        StaticBatchingUtility.Combine(tree);
                        specials.Add(objectPosition, tree);
                    }
                }
                else if (rockMap[i]) // Stone
                {
                    int x = i / CHUNK_SIZE_NO_PADDING;
                    int z = i % CHUNK_SIZE_NO_PADDING;
                    int y = terrainMap[(x + 1) * CHUNK_SIZE + (z + 1)];

                    // Place rock above water and under mountain
                    if (y > WATER_HEIGHT && y < MOUNTAIN_TRANSITION_START)
                    {
                        Vector3Int objectPosition = new Vector3Int(worldPosition.x + x, y - 1, worldPosition.y + z);
                        GameObject rock = Instantiate(rockObject, objectPosition, Quaternion.identity);
                        rock.transform.SetParent(specialsParent.transform);
                        rock.isStatic = true;
                        rock.transform.Rotate(0f, (float)rockNoiseMap[i] * 180, 0f, Space.World);
                        StaticBatchingUtility.Combine(rock);
                        specials.Add(objectPosition, rock);
                    }
                }
                else if (stickMap[i])
                {
                    int x = i / CHUNK_SIZE_NO_PADDING;
                    int z = i % CHUNK_SIZE_NO_PADDING;
                    int y = terrainMap[(x + 1) * CHUNK_SIZE + (z + 1)];

                    // Place stick above water and under mountain
                    if (y > WATER_HEIGHT && y < MOUNTAIN_TRANSITION_START)
                    {
                        Vector3Int objectPosition = new Vector3Int(worldPosition.x + x, y - 1, worldPosition.y + z);
                        GameObject stick = Instantiate(stickObject, objectPosition, Quaternion.identity);
                        stick.transform.SetParent(specialsParent.transform);
                        stick.isStatic = true;
                        stick.transform.Rotate(0f, (float)stickNoiseMap[i] * 180, 0f, Space.World);
                        StaticBatchingUtility.Combine(stick);
                        specials.Add(objectPosition, stick);
                    }
                }
            }
        }

        // Load blocks changes
        for (int i = 0; i < verticalChunks.Length; i++)
        {
            Chunk chunk = verticalChunks[i];

            if (chunk != null)
            {
                Vector3Int currentChunkPosition = new Vector3Int(chunkPosition.x, i, chunkPosition.y);
                List<StorageBlockData> blocks = Storage.GetBlocks(currentChunkPosition);

                foreach (StorageBlockData block in blocks)
                {
                    chunk.SetBlock(block.position.x, block.position.y, block.position.z, block.type);
                    Vector3Int blockPosition = new Vector3Int(block.position.x, block.position.y, block.position.z);
                }

                chunk.UpdateMesh();
            }
        }

        // Load specials changes
        List<StorageSpecialData> specialsStorage = Storage.GetSpecials(chunkPosition);

        foreach (var special in specialsStorage)
        {
            if (special.type == 0 && specials.TryGetValue(special.position, out GameObject specialObject))
            {
                DestroyImmediate(specialObject);
                specials.Remove(special.position);
            }
            else if (Items.Instance.items.TryGetValue(special.type, out Item item))
            {
                Special specialItem = (Special)item;
                GameObject gameObject = Instantiate(specialItem.gameObject, special.position, Quaternion.identity, chunkParent.transform);
                specials.Add(special.position, gameObject);
            }
        }

        // Spawn predator
        bool spawnPredator = random.NextFloat(0f, 1f) <= PREDATOR_SPAWN_RATE;

        if (spawnPredator)
        {
            int x = random.NextInt(0, CHUNK_SIZE_NO_PADDING);
            int z = random.NextInt(0, CHUNK_SIZE_NO_PADDING);
            int y = terrainMap[(x + 1) * CHUNK_SIZE + (z + 1)];

            if (y > WATER_HEIGHT)
            {
                GameObject npc = Instantiate(predator, new Vector3(worldPosition.x + x, y - 1, worldPosition.y + z), Quaternion.identity);
                npc.transform.SetParent(npcParent.transform);
            }
        }

        // Spawn prey
        bool spawnPrey = random.NextFloat(0f, 1f) <= PREY_SPAWN_RATE;

        if (spawnPrey)
        {
            int x = random.NextInt(0, CHUNK_SIZE_NO_PADDING);
            int z = random.NextInt(0, CHUNK_SIZE_NO_PADDING);
            int y = terrainMap[(x + 1) * CHUNK_SIZE + (z + 1)];

            if (y > WATER_HEIGHT)
            {
                GameObject npc = Instantiate(prey, new Vector3(worldPosition.x + x, y - 1, worldPosition.y + z), Quaternion.identity);
                npc.transform.SetParent(npcParent.transform);
            }
        }

        terrainMap.Dispose();
        noiseMap.Dispose();
        noiseMap2.Dispose();
        treeNoiseMap.Dispose();
        treeMap.Dispose();
        forestMap.Dispose();
        rockNoiseMap.Dispose();
        rockMap.Dispose();
        stickNoiseMap.Dispose();
        stickMap.Dispose();

        Cloud cloud = GenerateCloud(chunkPosition, new Vector3(worldPosition.x, CLOUD_HEIGHT, worldPosition.y) - new Vector3(1.5f, 1.5f, 1.5f));

        ChunkData chunkData = new ChunkData(verticalChunks, cloud, specials);
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
        chunk.SetAndSaveBlock((byte)relativePosition.x, (byte)relativePosition.y, (byte)relativePosition.z, id);
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
            neighbourChunk.SetAndSaveBlock((byte)neighbourRelativePosition.x, (byte)neighbourRelativePosition.y, (byte)neighbourRelativePosition.z, id);
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

    public void AddSpecial(Vector3Int worldPosition, Special special)
    {
        Vector3Int chunkPosition = WorldPositionToChunkPosition(worldPosition);

        if (chunks.TryGetValue(new Vector2Int(chunkPosition.x, chunkPosition.z), out ChunkData chunkData))
        {
            Vector3 position = new Vector3(worldPosition.x, worldPosition.y, worldPosition.z);
            GameObject specialObject = Instantiate(special.gameObject, position, Quaternion.identity, chunkParent.transform);

            chunkData.specials.Add(worldPosition, specialObject);
            chunkData.specialChanges.Add(new StorageSpecialData(worldPosition, special.itemID));
        }
    }

    public void RemoveSpecial(Vector3Int worldPosition)
    {
        Vector3Int chunkPosition = WorldPositionToChunkPosition(worldPosition);

        if (chunks.TryGetValue(new Vector2Int(chunkPosition.x, chunkPosition.z), out ChunkData chunkData))
        {
            if (chunkData.specials.TryGetValue(worldPosition, out GameObject specialObject))
            {
                chunkData.specials.Remove(worldPosition);
                chunkData.specialChanges.Add(new StorageSpecialData(worldPosition, 0));

                Destroy(specialObject);
            }
        }
    }

    public GameObject GetSpecial(Vector3Int worldPosition)
    {
        Vector3Int chunkPosition = WorldPositionToChunkPosition(worldPosition);

        if (chunks.TryGetValue(new Vector2Int(chunkPosition.x, chunkPosition.z), out ChunkData chunk))
            if (chunk.specials.TryGetValue(worldPosition, out GameObject specialObject))
                return specialObject;

        return null;
    }

    public Chunk GetChunk(Vector3Int chunkPosition)
    {
        if (chunks.TryGetValue(new Vector2Int(chunkPosition.x, chunkPosition.z), out ChunkData chunk))
            if (chunkPosition.y >= 0 && chunkPosition.y < chunk.chunks.Length)
                return chunk.chunks[chunkPosition.y];

        return null;
    }

    public bool IsValidChunk(Vector3Int chunkPosition)
    {
        if (chunks.TryGetValue(new Vector2Int(chunkPosition.x, chunkPosition.z), out ChunkData chunk))
            if (chunkPosition.y >= 0 && chunkPosition.y < chunk.chunks.Length)
                return chunk.chunks[chunkPosition.y] != null;

        return false;
    }

    public bool TryGetGroundPosition(Vector3Int worldPosition, out Vector3Int groundPosition)
    {
        groundPosition = default;

        Vector3Int chunkPosition = WorldPositionToChunkPosition(worldPosition);
        Vector3Int relativePosition = WorldPositionToChunkRelativePosition(chunkPosition, worldPosition);

        Chunk chunk = GetChunk(chunkPosition);

        if (chunk != null)
        {
            int ground = chunk.GetGroundPosition(relativePosition) + (chunkPosition.y * CHUNK_SIZE_NO_PADDING);
            groundPosition = new Vector3Int(worldPosition.x, ground, worldPosition.z);
            return true;
        }

        return false;
    }

    public bool IsGround(Vector3Int position)
    {
        Vector3Int down = position + Vector3Int.down;

        return IsValidChunk(WorldPositionToChunkPosition(position)) && GetBlock(position) == 0 && GetBlock(down) != 0 && (GetSpecial(position) == null || GetSpecial(position).CompareTag("IgnorePathFinding"));
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

    public bool TryPathfinding(Vector3Int startPosition, Vector3Int endPosition, out List<Vector3Int> path)
    {
        path = new List<Vector3Int>();

        if (!IsValidChunk(WorldPositionToChunkPosition(startPosition)) || !IsGround(startPosition))
            return false;

        if (!IsValidChunk(WorldPositionToChunkPosition(startPosition)) || !IsGround(endPosition))
            return false;

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

        return true;
    }

    public struct ChunkData
    {
        public Chunk[] chunks;
        public Cloud cloud;

        public Dictionary<Vector3Int, GameObject> specials;
        public List<StorageSpecialData> specialChanges;

        public ChunkData(Chunk[] chunks, Cloud cloud, Dictionary<Vector3Int, GameObject> specials)
        {
            this.chunks = chunks;
            this.cloud = cloud;
            this.specials = specials;
            this.specialChanges = new List<StorageSpecialData>();
        }
    }
}