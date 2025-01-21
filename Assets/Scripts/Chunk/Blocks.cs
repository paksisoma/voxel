using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class Blocks : MonoBehaviour
{
    public static Blocks Instance { get; private set; }

    public Block[] blocks;
    public UnsafeHashMap<int, BlockProperties> blockProperties = new UnsafeHashMap<int, BlockProperties>(16, Allocator.Persistent);
    public Dictionary<int, Material> materials = new Dictionary<int, Material>();
    public Dictionary<int, Block> blocksID = new Dictionary<int, Block>();

    void Awake()
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

        foreach (Block block in blocks)
        {
            int topHash = block.topMaterial.GetHashCode();
            int sideHash = block.sideMaterial.GetHashCode();
            int bottomHash = block.bottomMaterial.GetHashCode();

            blockProperties.Add(block.itemID, new BlockProperties(topHash, sideHash, bottomHash));
            blocksID.Add(block.itemID, block);

            if (!materials.ContainsKey(topHash))
                materials.Add(topHash, block.topMaterial);

            if (!materials.ContainsKey(sideHash))
                materials.Add(sideHash, block.sideMaterial);

            if (!materials.ContainsKey(bottomHash))
                materials.Add(bottomHash, block.bottomMaterial);
        }
    }

    /*private void OnDestroy()
    {
        blockProperties.Dispose();
    }*/

    public struct BlockProperties
    {
        public readonly int top;
        public readonly int side;
        public readonly int bottom;

        public BlockProperties(int top, int side, int bottom)
        {
            this.top = top;
            this.side = side;
            this.bottom = bottom;
        }
    }
}