using System.Collections.Generic;
using UnityEngine;

public enum BlockType : byte
{
    Air,
    Grass,
    Dirt,
    Stone
}

public class BlockProperties
{
    public Material material { get; set; }
}

public static class BlockData
{
    public static readonly Dictionary<BlockType, BlockProperties> BlockProperties = new Dictionary<BlockType, BlockProperties>
    {
        { BlockType.Grass, new BlockProperties { material = Resources.Load("Grass", typeof(Material)) as Material } },
        { BlockType.Dirt, new BlockProperties { material = Resources.Load("Dirt", typeof(Material)) as Material } },
        { BlockType.Stone, new BlockProperties { material = Resources.Load("Stone", typeof(Material)) as Material } },
    };
}

public struct Vector3Byte
{
    public byte x;
    public byte y;
    public byte z;

    public Vector3Byte(byte x, byte y, byte z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}