using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using static Constants;

public class ChunkRenderer : MonoBehaviour
{
    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private GameObject meshObject;

    private Dictionary<byte, Dictionary<byte, ulong[]>> data = new Dictionary<byte, Dictionary<byte, ulong[]>>(); // Axis, y, x, z

    private int relativeX, relativeY, relativeZ;

    private void Awake()
    {
        meshObject = new GameObject();

        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshCollider = meshObject.AddComponent<MeshCollider>();
    }

    void OnDestroy()
    {
        Destroy(meshObject);
    }

    public void Initialize(BlockType blockType, int relativeX, int relativeY, int relativeZ)
    {
        meshRenderer.material = BlockData.BlockProperties[blockType].material;

        this.relativeX = relativeX;
        this.relativeY = relativeY;
        this.relativeZ = relativeZ;
    }

    public void AddData(byte axis, byte x, byte y, byte z)
    {
        if (!data.TryGetValue(axis, out var a))
            data[axis] = a = new Dictionary<byte, ulong[]>();

        if (!data[axis].TryGetValue(y, out var b))
            data[axis][y] = b = new ulong[CHUNK_SIZE];

        data[axis][y][x] |= 1ul << z;
    }

    public void Render()
    {
        // STOPWATCH START
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();

        // COUNT ARRAY LENGTH

        uint trianglesLength = 0;
        uint verticesLength = 0;

        foreach (var a in data)
        {
            byte axis = a.Key;
            Dictionary<byte, ulong[]> blocks = a.Value;

            foreach (var b in blocks.Where(b => b.Key > 0 && b.Key <= CHUNK_SIZE - 2)) // PADDDING REMOVE
            {
                byte y = b.Key;
                ulong[] xz = (ulong[])b.Value.Clone();

                for (byte i = 1; i < xz.Length - 1; i++)
                {
                    // PADDDING REMOVE
                    xz[i] = (xz[i] & ~1ul) & ~(1ul << CHUNK_SIZE - 1);

                    while (xz[i] != 0)
                    {
                        int trailingZeros = math.tzcnt(xz[i]);
                        int trailingOnes = math.tzcnt(~xz[i] >> trailingZeros);
                        ulong mask = ((1ul << trailingOnes) - 1ul) << trailingZeros;

                        byte height = 1;

                        for (int j = i + 1; j < xz.Length - 1 && (xz[j] & mask) == mask; j++)
                        {
                            // PADDDING REMOVE
                            xz[j] = (xz[j] & ~1ul) & ~(1ul << CHUNK_SIZE - 1);

                            xz[j] ^= mask;
                            height++;
                        }

                        xz[i] ^= mask;

                        trianglesLength += 6;
                        verticesLength += 4;
                    }
                }
            }
        }

        // ADD VERTICES AND TRIANGLES TO LIST USING BINARY GREEDY MESHING

        Vector3[] vertices = new Vector3[verticesLength];
        int[] triangles = new int[trianglesLength];

        int verticesIndex = 0;
        int trianglesIndex = 0;

        foreach (var a in data)
        {
            byte axis = a.Key;
            Dictionary<byte, ulong[]> blocks = a.Value;

            foreach (var b in blocks.Where(b => b.Key > 0 && b.Key <= CHUNK_SIZE - 2)) // PADDDING REMOVE
            {
                byte y = b.Key;
                ulong[] xz = (ulong[])b.Value.Clone();

                for (byte i = 1; i < xz.Length - 1; i++)
                {
                    // PADDDING REMOVE
                    xz[i] = (xz[i] & ~1ul) & ~(1ul << CHUNK_SIZE - 1);

                    while (xz[i] != 0)
                    {
                        int trailingZeros = math.tzcnt(xz[i]);
                        int trailingOnes = math.tzcnt(~xz[i] >> trailingZeros);
                        ulong mask = ((1ul << trailingOnes) - 1ul) << trailingZeros;

                        byte height = 1;

                        for (int j = i + 1; j < xz.Length - 1 && (xz[j] & mask) == mask; j++)
                        {
                            // PADDDING REMOVE
                            xz[j] = (xz[j] & ~1ul) & ~(1ul << CHUNK_SIZE - 1);

                            xz[j] ^= mask;
                            height++;
                        }

                        xz[i] ^= mask;

                        // TRIANGLES

                        triangles[trianglesIndex] = verticesIndex;
                        triangles[trianglesIndex + 1] = verticesIndex + 1;
                        triangles[trianglesIndex + 2] = verticesIndex + 2;

                        triangles[trianglesIndex + 3] = verticesIndex;
                        triangles[trianglesIndex + 4] = verticesIndex + 2;
                        triangles[trianglesIndex + 5] = verticesIndex + 3;

                        // VERTICES

                        if (axis == 0) // Bottom
                        {
                            vertices[verticesIndex] = new Vector3(i, y, trailingZeros) + new Vector3(relativeX, relativeY, relativeZ);
                            vertices[verticesIndex + 1] = new Vector3(i + height, y, trailingZeros) + new Vector3(relativeX, relativeY, relativeZ);
                            vertices[verticesIndex + 2] = new Vector3(i + height, y, trailingZeros + trailingOnes) + new Vector3(relativeX, relativeY, relativeZ);
                            vertices[verticesIndex + 3] = new Vector3(i, y, trailingZeros + trailingOnes) + new Vector3(relativeX, relativeY, relativeZ);
                        }
                        else if (axis == 1) // Top
                        {
                            vertices[verticesIndex] = new Vector3(i + height, y + 1, trailingZeros) + new Vector3(relativeX, relativeY, relativeZ);
                            vertices[verticesIndex + 1] = new Vector3(i, y + 1, trailingZeros) + new Vector3(relativeX, relativeY, relativeZ);
                            vertices[verticesIndex + 2] = new Vector3(i, y + 1, trailingZeros + trailingOnes) + new Vector3(relativeX, relativeY, relativeZ);
                            vertices[verticesIndex + 3] = new Vector3(i + height, y + 1, trailingZeros + trailingOnes) + new Vector3(relativeX, relativeY, relativeZ);
                        }
                        else if (axis == 2) // Left
                        {
                            vertices[verticesIndex] = new Vector3(y, trailingZeros, i) + new Vector3(relativeX, relativeY, relativeZ);
                            vertices[verticesIndex + 1] = new Vector3(y, trailingZeros, i + height) + new Vector3(relativeX, relativeY, relativeZ);
                            vertices[verticesIndex + 2] = new Vector3(y, trailingZeros + trailingOnes, i + height) + new Vector3(relativeX, relativeY, relativeZ);
                            vertices[verticesIndex + 3] = new Vector3(y, trailingZeros + trailingOnes, i) + new Vector3(relativeX, relativeY, relativeZ);
                        }
                        else if (axis == 3) // Right
                        {
                            vertices[verticesIndex] = new Vector3(y + 1, trailingZeros, i + height) + new Vector3(relativeX, relativeY, relativeZ);
                            vertices[verticesIndex + 1] = new Vector3(y + 1, trailingZeros, i) + new Vector3(relativeX, relativeY, relativeZ);
                            vertices[verticesIndex + 2] = new Vector3(y + 1, trailingZeros + trailingOnes, i) + new Vector3(relativeX, relativeY, relativeZ);
                            vertices[verticesIndex + 3] = new Vector3(y + 1, trailingZeros + trailingOnes, i + height) + new Vector3(relativeX, relativeY, relativeZ);
                        }
                        else if (axis == 4) // Back
                        {
                            vertices[verticesIndex] = new Vector3(i + height, trailingZeros, y) + new Vector3(relativeX, relativeY, relativeZ);
                            vertices[verticesIndex + 1] = new Vector3(i, trailingZeros, y) + new Vector3(relativeX, relativeY, relativeZ);
                            vertices[verticesIndex + 2] = new Vector3(i, trailingZeros + trailingOnes, y) + new Vector3(relativeX, relativeY, relativeZ);
                            vertices[verticesIndex + 3] = new Vector3(i + height, trailingZeros + trailingOnes, y) + new Vector3(relativeX, relativeY, relativeZ);
                        }
                        else if (axis == 5) // Front
                        {
                            vertices[verticesIndex] = new Vector3(i, trailingZeros, y + 1) + new Vector3(relativeX, relativeY, relativeZ);
                            vertices[verticesIndex + 1] = new Vector3(i + height, trailingZeros, y + 1) + new Vector3(relativeX, relativeY, relativeZ);
                            vertices[verticesIndex + 2] = new Vector3(i + height, trailingZeros + trailingOnes, y + 1) + new Vector3(relativeX, relativeY, relativeZ);
                            vertices[verticesIndex + 3] = new Vector3(i, trailingZeros + trailingOnes, y + 1) + new Vector3(relativeX, relativeY, relativeZ);
                        }

                        trianglesIndex += 6;
                        verticesIndex += 4;
                    }
                }
            }
        }

        // RENDER

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;

        // STOPWATCH STOP

        stopWatch.Stop();
        TimeSpan ts = stopWatch.Elapsed;
        UnityEngine.Debug.Log("Chunk Render: " + ts.Milliseconds + "ms");
    }
}