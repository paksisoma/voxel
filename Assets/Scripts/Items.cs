using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class Items : MonoBehaviour
{
    public static Items Instance { get; private set; }

    // Init list
    public Item[] itemList;

    // Items
    public Dictionary<int, Item> items = new Dictionary<int, Item>();

    // Blocks
    public UnsafeHashMap<int, BlockProperties> blockProperties = new UnsafeHashMap<int, BlockProperties>(16, Allocator.Persistent);
    public Dictionary<int, Block> blocks = new Dictionary<int, Block>();
    public Dictionary<int, Material> materials = new Dictionary<int, Material>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        foreach (Item item in itemList)
        {
            // All
            items.Add(item.itemID, item);

            // Block
            if (item is Block)
            {
                Block block = (Block)item;

                int topHash = block.topMaterial.GetHashCode();
                int sideHash = block.sideMaterial.GetHashCode();
                int bottomHash = block.bottomMaterial.GetHashCode();

                blockProperties.Add(block.itemID, new BlockProperties(topHash, sideHash, bottomHash));
                blocks.Add(block.itemID, block);

                if (!materials.ContainsKey(topHash))
                    materials.Add(topHash, block.topMaterial);

                if (!materials.ContainsKey(sideHash))
                    materials.Add(sideHash, block.sideMaterial);

                if (!materials.ContainsKey(bottomHash))
                    materials.Add(bottomHash, block.bottomMaterial);
            }
        }
    }
}