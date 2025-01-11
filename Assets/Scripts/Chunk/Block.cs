using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public static class Block
{
    public static readonly Dictionary<byte, Material> Materials = new Dictionary<byte, Material>
    {
        { 1, GetMaterial("Grass Top") },
        { 2, GetMaterial("Grass Side") },
        { 3, GetMaterial("Dirt") },
        { 4, GetMaterial("Stone") },
        { 5, GetMaterial("Water") },
        { 6, GetMaterial("Sand") },
    };

    public static readonly UnsafeHashMap<byte, BlockProperties> blockProperties = new UnsafeHashMap<byte, BlockProperties>(10, Allocator.Persistent)
    {
        { 1, new BlockProperties(1, 2, 3) }, // Grass
        { 2, new BlockProperties(4, 4, 4) }, // Stone
        { 3, new BlockProperties(5, 5, 5) }, // Water
        { 4, new BlockProperties(6, 6, 6) }, // Sand
    };

    private static Material GetMaterial(string filename)
    {
        return Resources.Load(filename, typeof(Material)) as Material;
    }

    public struct BlockProperties
    {
        public readonly byte top;
        public readonly byte side;
        public readonly byte bottom;

        public BlockProperties(byte top, byte side, byte bottom)
        {
            this.top = top;
            this.side = side;
            this.bottom = bottom;
        }
    }
}