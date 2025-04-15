using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static Constants;

public static class Storage
{
    public static Dictionary<Vector3Int, (int, int)> chunkSeek; // Chunk position, start, length

    private static string worldPath;
    private static string indexPath;
    private static string blocksPath;
    private static string characterPath;

    private const int blockDataBytes = 4;

    public static void LoadMap(string mapName)
    {
        string path = Application.dataPath + "/Save/" + mapName + "/";

        worldPath = path + "/world_00.bin";
        indexPath = path + "/world_01.bin";
        blocksPath = path + "/world_02.bin";
        characterPath = path + "/character.bin";

        // Load index table
        chunkSeek = new Dictionary<Vector3Int, (int, int)>();

        using (BinaryReader reader = new BinaryReader(File.Open(indexPath, FileMode.Open)))
        {
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                int x = reader.ReadInt32();
                int y = reader.ReadInt32();
                int z = reader.ReadInt32();
                int start = reader.ReadInt32();
                int length = reader.ReadInt32();

                chunkSeek.Add(new Vector3Int(x, y, z), (start, length));
            }
        }
    }

    public static void CreateMap(string mapName, uint seed)
    {
        string path = Application.dataPath;

        // Save folder
        path += "/Save/";
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        // Map folder
        path += "/" + mapName + "/";
        Directory.CreateDirectory(path);

        // World main file
        worldPath = path + "/world_00.bin";
        File.Create(worldPath).Close();

        // Index table file
        indexPath = path + "/world_01.bin";
        File.Create(indexPath).Close();

        // Block file
        blocksPath = path + "/world_02.bin";
        File.Create(blocksPath).Close();

        // Character file
        characterPath = path + "/character.bin";
        File.Create(characterPath).Close();

        SetWorld(new StorageWorld(Application.version, Application.unityVersion, seed, 0)); // Init map data
        SetCharacter(new StorageCharacter(INIT_HEALTH, INIT_THIRST, INIT_HUNGER, INIT_TEMPERATURE, new Vector3(0, 0, 0), 0, 0, 0, new List<StorageInventory>())); // Init character data
    }

    public static void SaveChunk(Vector3Int chunkPosition, List<StorageBlockData> blocks)
    {
        bool chunkContains = chunkSeek.ContainsKey(chunkPosition);

        if (chunkContains)
        {
            AppendBlocks(blocks, out int start);
            LinkChunk(chunkPosition, start, blocks.Count);
        }
        else
        {
            AppendBlocks(blocks, out int start);
            AppendIndex(chunkPosition, start, blocks.Count);
        }
    }

    public static void AppendBlocks(List<StorageBlockData> blocks, out int startPosition)
    {
        using (BinaryWriter writer = new BinaryWriter(File.Open(blocksPath, FileMode.Append)))
        {
            startPosition = (int)writer.BaseStream.Position; // Get start position for indextable

            foreach (StorageBlockData blockData in blocks)
            {
                writer.Write(blockData.position.x); // X position
                writer.Write(blockData.position.y); // Y position
                writer.Write(blockData.position.z); // Z position

                writer.Write(blockData.type); // Type
            }

            // Leave 8 bytes for chunk linking (start, length)
            writer.Write(-1);
            writer.Write(-1);
        }
    }

    public static void AppendIndex(Vector3Int chunkPosition, int start, int length)
    {
        using (BinaryWriter writer = new BinaryWriter(File.Open(indexPath, FileMode.Append)))
        {
            writer.Write(chunkPosition.x); // X position
            writer.Write(chunkPosition.y); // Y position
            writer.Write(chunkPosition.z); // Z position

            writer.Write(start); // Start
            writer.Write(length); // Length
        }

        chunkSeek.Add(chunkPosition, (start, length)); // Add to chunkSeek dictionary
    }

    public static void LinkChunk(Vector3Int chunkPosition, int newStart, int newLength)
    {
        int start = chunkSeek[chunkPosition].Item1; // Start
        int length = chunkSeek[chunkPosition].Item2; // Length

        int seek = 0;

        // Find last not linked chunk
        while (start != -1)
        {
            using (BinaryReader reader = new BinaryReader(File.Open(blocksPath, FileMode.Open)))
            {
                seek = length * blockDataBytes + start; // Calculate seek position

                reader.BaseStream.Seek(seek, SeekOrigin.Begin); // Seek to end of the chunk

                start = reader.ReadInt32();
                length = reader.ReadInt32();
            }
        }

        // Found
        using (BinaryWriter writer = new BinaryWriter(File.Open(blocksPath, FileMode.Open, FileAccess.Write)))
        {
            writer.BaseStream.Seek(seek, SeekOrigin.Begin);

            writer.Write(newStart); // Start
            writer.Write(newLength); // Length
        }
    }

    public static List<StorageBlockData> GetBlocks(Vector3Int chunkPosition)
    {
        List<StorageBlockData> blocks = new List<StorageBlockData>();

        if (chunkSeek.TryGetValue(chunkPosition, out (int, int) value))
        {
            int start = value.Item1;
            int length = value.Item2;

            while (start != -1)
                blocks.AddRange(ReadBlocks(start, length, out start, out length));
        }

        return blocks;
    }

    private static List<StorageBlockData> ReadBlocks(int start, int length, out int nextStart, out int nextLength)
    {
        List<StorageBlockData> blocks = new List<StorageBlockData>();

        using (BinaryReader reader = new BinaryReader(File.Open(blocksPath, FileMode.Open)))
        {
            reader.BaseStream.Seek(start, SeekOrigin.Begin);

            for (int i = 0; i < length; i++)
            {
                byte x = reader.ReadByte(); // X position
                byte y = reader.ReadByte(); // Y position
                byte z = reader.ReadByte(); // Z position

                byte type = reader.ReadByte(); // Type

                blocks.Add(new StorageBlockData(new Vector3Byte(x, y, z), type)); // Add block data to list
            }

            nextStart = reader.ReadInt32();
            nextLength = reader.ReadInt32();
        }

        return blocks;
    }

    public static void SetCharacter(StorageCharacter character)
    {
        using (BinaryWriter writer = new BinaryWriter(File.Open(characterPath, FileMode.Create)))
        {
            writer.Write(character.health); // Health
            writer.Write(character.thirst); // Thirst
            writer.Write(character.hunger); // Hunger
            writer.Write(character.temperature); // Temperature
            writer.Write(character.position.x); // X position
            writer.Write(character.position.y); // Y position
            writer.Write(character.position.z); // Z position
            writer.Write(character.rotation); // Rotation
            writer.Write(character.yaw); // Yaw
            writer.Write(character.pitch); // Pitch

            // Inventory
            foreach (StorageInventory item in character.inventory)
            {
                writer.Write(item.index); // Index
                writer.Write(item.type); // Type
                writer.Write(item.quantity); // Quantity
            }
        }
    }

    public static StorageCharacter GetCharacter()
    {
        using (BinaryReader reader = new BinaryReader(File.Open(characterPath, FileMode.Open)))
        {
            float health = reader.ReadSingle(); // Health
            float thirst = reader.ReadSingle(); // Thirst
            float hunger = reader.ReadSingle(); // Hunger
            float temperature = reader.ReadSingle(); // Temperature
            float x = reader.ReadSingle(); // X position
            float y = reader.ReadSingle(); // Y position
            float z = reader.ReadSingle(); // Z position
            float rotation = reader.ReadSingle(); // Rotation
            float yaw = reader.ReadSingle(); // Yaw
            float pitch = reader.ReadSingle(); // Pitch

            List<StorageInventory> inventory = new List<StorageInventory>();

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                byte index = reader.ReadByte(); // Index
                byte type = reader.ReadByte(); // Type
                byte quantity = reader.ReadByte(); // Quantity

                inventory.Add(new StorageInventory(index, type, quantity)); // Add item to inventory
            }

            return new StorageCharacter(health, thirst, hunger, temperature, new Vector3(x, y, z), rotation, yaw, pitch, inventory); // Return character data
        }
    }

    public static void SetWorld(StorageWorld map)
    {
        using (BinaryWriter writer = new BinaryWriter(File.Open(worldPath, FileMode.Create)))
        {
            writer.Write(map.unityVersion); // Unity version
            writer.Write(map.version); // Version
            writer.Write(map.seed); // Seed
            writer.Write(map.visit); // Visit
        }
    }

    public static StorageWorld GetWorld()
    {
        using (BinaryReader reader = new BinaryReader(File.Open(worldPath, FileMode.Open)))
        {
            string unityVersion = reader.ReadString(); // Unity version
            string version = reader.ReadString(); // Version
            uint seed = reader.ReadUInt32(); // Seed
            uint visit = reader.ReadUInt32(); // Visit

            return new StorageWorld(unityVersion, version, seed, visit); // Return map data
        }
    }
}

public struct StorageBlockData
{
    public Vector3Byte position;
    public byte type;

    public StorageBlockData(Vector3Byte position, byte type)
    {
        this.position = position;
        this.type = type;
    }
}

public struct StorageInventory
{
    public byte index;
    public byte type;
    public byte quantity;

    public StorageInventory(byte index, byte type, byte quantity)
    {
        this.index = index;
        this.type = type;
        this.quantity = quantity;
    }
}

public struct StorageCharacter
{
    public float health;
    public float thirst;
    public float hunger;
    public float temperature;
    public Vector3 position;
    public float rotation;
    public float yaw;
    public float pitch;
    public List<StorageInventory> inventory;

    public StorageCharacter(float health, float thirst, float hunger, float temperature, Vector3 position, float rotation, float yaw, float pitch, List<StorageInventory> inventory)
    {
        this.health = health;
        this.thirst = thirst;
        this.hunger = hunger;
        this.temperature = temperature;
        this.position = position;
        this.rotation = rotation;
        this.yaw = yaw;
        this.pitch = pitch;
        this.inventory = inventory;
    }
}

public struct StorageWorld
{
    public string version;
    public string unityVersion;
    public uint seed;
    public uint visit;

    public StorageWorld(string version, string unityVersion, uint seed, uint visit)
    {
        this.version = version;
        this.unityVersion = unityVersion;
        this.seed = seed;
        this.visit = visit;
    }
}

public struct Vector3Byte
{
    public byte x, y, z;

    public Vector3Byte(byte x, byte y, byte z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public override string ToString()
    {
        return $"({x}, {y}, {z})";
    }
}