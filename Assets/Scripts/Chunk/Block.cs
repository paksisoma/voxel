using System.Collections.Generic;
using UnityEngine;

public enum BlockType : byte
{
    Air,
    Grass,
    Dirt,
    Stone,
    Water,
    Sand,
}

public static class BlockData
{
    public static readonly Dictionary<BlockType, BlockProperties> BlockProperties = new Dictionary<BlockType, BlockProperties>
    {
        { BlockType.Grass, new BlockProperties { topMaterial = GetMaterial("Grass Top"), sideMaterial = GetMaterial("Grass Side"), bottomMaterial = GetMaterial("Dirt") } },
        { BlockType.Dirt, new BlockProperties { topMaterial = GetMaterial("Dirt"), sideMaterial = GetMaterial("Dirt"), bottomMaterial = GetMaterial("Dirt") } },
        { BlockType.Stone, new BlockProperties { topMaterial = GetMaterial("Stone"), sideMaterial = GetMaterial("Stone"), bottomMaterial = GetMaterial("Stone") } },
        { BlockType.Water, new BlockProperties { topMaterial = GetMaterial("Water"), sideMaterial = GetMaterial("Water"), bottomMaterial = GetMaterial("Water") } },
        { BlockType.Sand, new BlockProperties { topMaterial = GetMaterial("Sand"), sideMaterial = GetMaterial("Sand"), bottomMaterial = GetMaterial("Sand") } },
    };

    private static Material GetMaterial(string filename)
    {
        return Resources.Load(filename, typeof(Material)) as Material;
    }
}

public struct BlockProperties
{
    public Material topMaterial;
    public Material sideMaterial;
    public Material bottomMaterial;

    public BlockProperties(Material topMaterial, Material sideMaterial, Material bottomMaterial)
    {
        this.topMaterial = topMaterial;
        this.sideMaterial = sideMaterial;
        this.bottomMaterial = bottomMaterial;
    }
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