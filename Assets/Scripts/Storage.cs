using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static Constants;

public static class Storage
{
    private static Dictionary<Vector3Int, (int, int)> blocksSeek; // Chunk position, start, length
    private static Dictionary<Vector2Int, (int, int)> specialsSeek; // Chunk position, start, length

    private static string worldPath;
    private static string blocksIndexPath;
    private static string blocksPath;
    private static string specialsIndexPath;
    private static string specialsPath;
    private static string characterPath;

    private const int blockDataBytes = 4;
    private const int specialDataBytes = 13;

    public static void LoadMap(string mapName)
    {
        string path = Application.dataPath + "/Save/" + mapName + "/";

        worldPath = path + "/world_00.bin";
        blocksIndexPath = path + "/world_01.bin";
        blocksPath = path + "/world_02.bin";
        specialsIndexPath = path + "/world_03.bin";
        specialsPath = path + "/world_04.bin";
        characterPath = path + "/character.bin";

        // Load blocks index table
        blocksSeek = new Dictionary<Vector3Int, (int, int)>();

        using (BinaryReader reader = new BinaryReader(File.Open(blocksIndexPath, FileMode.Open)))
        {
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                int x = reader.ReadInt32();
                int y = reader.ReadInt32();
                int z = reader.ReadInt32();
                int start = reader.ReadInt32();
                int length = reader.ReadInt32();

                blocksSeek.Add(new Vector3Int(x, y, z), (start, length));
            }
        }

        // Load specials index table
        specialsSeek = new Dictionary<Vector2Int, (int, int)>();

        using (BinaryReader reader = new BinaryReader(File.Open(specialsIndexPath, FileMode.Open)))
        {
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                int x = reader.ReadInt32();
                int y = reader.ReadInt32();
                int start = reader.ReadInt32();
                int length = reader.ReadInt32();

                specialsSeek.Add(new Vector2Int(x, y), (start, length));
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

        // Blocks index table file
        blocksIndexPath = path + "/world_01.bin";
        File.Create(blocksIndexPath).Close();

        // Blocks file
        blocksPath = path + "/world_02.bin";
        File.Create(blocksPath).Close();

        // Specials index table file
        specialsIndexPath = path + "/world_03.bin";
        File.Create(specialsIndexPath).Close();

        // Specials file
        specialsPath = path + "/world_04.bin";
        File.Create(specialsPath).Close();

        // Character file
        characterPath = path + "/character.bin";
        File.Create(characterPath).Close();

        SetWorld(new StorageWorld(Application.version, Application.unityVersion, seed, 0, INIT_TIME)); // Init map data
        SetCharacter(new StorageCharacter(INIT_HEALTH, INIT_THIRST, INIT_HUNGER, INIT_TEMPERATURE, new Vector3(0, 0, 0), 0, 0, 0, 0, new List<StorageItem>())); // Init character data
    }

    public static void SaveBlocks(Vector3Int chunkPosition, List<StorageBlockData> blocks)
    {
        bool chunkContains = blocksSeek.ContainsKey(chunkPosition);

        if (chunkContains)
        {
            AppendBlocks(blocks, out int start);
            LinkBlocks(chunkPosition, start, blocks.Count);
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
        using (BinaryWriter writer = new BinaryWriter(File.Open(blocksIndexPath, FileMode.Append)))
        {
            writer.Write(chunkPosition.x); // X position
            writer.Write(chunkPosition.y); // Y position
            writer.Write(chunkPosition.z); // Z position

            writer.Write(start); // Start
            writer.Write(length); // Length
        }

        blocksSeek.Add(chunkPosition, (start, length)); // Add to blocksSeek dictionary
    }

    public static void LinkBlocks(Vector3Int chunkPosition, int newStart, int newLength)
    {
        int start = blocksSeek[chunkPosition].Item1; // Start
        int length = blocksSeek[chunkPosition].Item2; // Length

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

        if (blocksSeek.TryGetValue(chunkPosition, out (int, int) value))
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

    public static void SaveSpecials(Vector2Int chunkPosition, List<StorageSpecialData> specials)
    {
        bool seekContains = specialsSeek.ContainsKey(chunkPosition);

        if (seekContains)
        {
            AppendSpecials(specials, out int start);
            LinkSpecials(chunkPosition, start, specials.Count);
        }
        else
        {
            AppendSpecials(specials, out int start);
            AppendSpecialsIndex(chunkPosition, start, specials.Count);
        }
    }

    public static void AppendSpecials(List<StorageSpecialData> specials, out int startPosition)
    {
        using (BinaryWriter writer = new BinaryWriter(File.Open(specialsPath, FileMode.Append)))
        {
            startPosition = (int)writer.BaseStream.Position; // Get start position for indextable

            foreach (StorageSpecialData specialData in specials)
            {
                writer.Write(specialData.position.x); // X position
                writer.Write(specialData.position.y); // Y position
                writer.Write(specialData.position.z); // Z position

                writer.Write(specialData.type); // Type
            }

            // Leave 8 bytes for linking (start, length)
            writer.Write(-1);
            writer.Write(-1);
        }
    }

    public static void AppendSpecialsIndex(Vector2Int chunkPosition, int start, int length)
    {
        using (BinaryWriter writer = new BinaryWriter(File.Open(specialsIndexPath, FileMode.Append)))
        {
            writer.Write(chunkPosition.x); // X position
            writer.Write(chunkPosition.y); // Y position

            writer.Write(start); // Start
            writer.Write(length); // Length
        }

        specialsSeek.Add(chunkPosition, (start, length)); // Add to specialsSeek dictionary
    }

    public static void LinkSpecials(Vector2Int chunkPosition, int newStart, int newLength)
    {
        int start = specialsSeek[chunkPosition].Item1; // Start
        int length = specialsSeek[chunkPosition].Item2; // Length

        int seek = 0;

        // Find last not linked chunk
        while (start != -1)
        {
            using (BinaryReader reader = new BinaryReader(File.Open(specialsPath, FileMode.Open)))
            {
                seek = length * specialDataBytes + start; // Calculate seek position

                reader.BaseStream.Seek(seek, SeekOrigin.Begin); // Seek to end of the chunk

                start = reader.ReadInt32();
                length = reader.ReadInt32();
            }
        }

        // Found
        using (BinaryWriter writer = new BinaryWriter(File.Open(specialsPath, FileMode.Open, FileAccess.Write)))
        {
            writer.BaseStream.Seek(seek, SeekOrigin.Begin);

            writer.Write(newStart); // Start
            writer.Write(newLength); // Length
        }
    }

    public static List<StorageSpecialData> GetSpecials(Vector2Int chunkPosition)
    {
        List<StorageSpecialData> specials = new List<StorageSpecialData>();

        if (specialsSeek.TryGetValue(chunkPosition, out (int, int) value))
        {
            int start = value.Item1;
            int length = value.Item2;

            while (start != -1)
                specials.AddRange(ReadSpecials(start, length, out start, out length));
        }

        return specials;
    }

    private static List<StorageSpecialData> ReadSpecials(int start, int length, out int nextStart, out int nextLength)
    {
        List<StorageSpecialData> specials = new List<StorageSpecialData>();

        using (BinaryReader reader = new BinaryReader(File.Open(specialsPath, FileMode.Open)))
        {
            reader.BaseStream.Seek(start, SeekOrigin.Begin);

            for (int i = 0; i < length; i++)
            {
                int x = reader.ReadInt32(); // X position
                int y = reader.ReadInt32(); // Y position
                int z = reader.ReadInt32(); // Z position

                byte type = reader.ReadByte(); // Type

                specials.Add(new StorageSpecialData(new Vector3Int(x, y, z), type)); // Add special data to list
            }

            nextStart = reader.ReadInt32();
            nextLength = reader.ReadInt32();
        }

        return specials;
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

            writer.Write(character.armor); // Armor

            // Inventory
            foreach (StorageItem item in character.inventory)
            {
                writer.Write(item.slot); // Slot
                writer.Write(item.id); // ID
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

            byte armor = reader.ReadByte(); // Armor

            List<StorageItem> inventory = new List<StorageItem>();

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                byte slot = reader.ReadByte(); // Slot
                byte id = reader.ReadByte(); // ID
                byte quantity = reader.ReadByte(); // Quantity

                inventory.Add(new StorageItem(slot, id, quantity)); // Add item to inventory
            }

            return new StorageCharacter(health, thirst, hunger, temperature, new Vector3(x, y, z), rotation, yaw, pitch, armor, inventory); // Return character data
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
            writer.Write(map.time); // time
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
            float time = reader.ReadSingle(); // Time

            return new StorageWorld(unityVersion, version, seed, visit, time); // Return map data
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

public struct StorageSpecialData
{
    public Vector3Int position;
    public byte type;

    public StorageSpecialData(Vector3Int position, byte type)
    {
        this.position = position;
        this.type = type;
    }
}

public struct StorageItem
{
    public byte slot;
    public byte id;
    public byte quantity;

    public StorageItem(byte slot, byte id, byte quantity)
    {
        this.slot = slot;
        this.id = id;
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
    public byte armor;
    public List<StorageItem> inventory;

    public StorageCharacter(float health, float thirst, float hunger, float temperature, Vector3 position, float rotation, float yaw, float pitch, byte armor, List<StorageItem> inventory)
    {
        this.health = health;
        this.thirst = thirst;
        this.hunger = hunger;
        this.temperature = temperature;
        this.position = position;
        this.rotation = rotation;
        this.yaw = yaw;
        this.pitch = pitch;
        this.armor = armor;
        this.inventory = inventory;
    }
}

public struct StorageWorld
{
    public string version;
    public string unityVersion;
    public uint seed;
    public uint visit;
    public float time;

    public StorageWorld(string version, string unityVersion, uint seed, uint visit, float time)
    {
        this.version = version;
        this.unityVersion = unityVersion;
        this.seed = seed;
        this.visit = visit;
        this.time = time;
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