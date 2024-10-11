using System.Threading.Tasks;
using UnityEngine;
using static Constants;

public class Generation : MonoBehaviour
{
    private Chunk[,] loadedChunks;

    private void Awake()
    {
        loadedChunks = new Chunk[RENDER_DISTANCE_LENGTH, RENDER_DISTANCE_LENGTH];
    }

    private void Update()
    {
        Vector3Int playerChunk = Player.Instance.chunkPosition;

        for (int x = -RENDER_DISTANCE; x <= RENDER_DISTANCE; x++)
        {
            for (int z = -RENDER_DISTANCE; z <= RENDER_DISTANCE; z++)
            {
                Vector3Int relativePosition = new Vector3Int((x + playerChunk.x) * CHUNK_SIZE_NO_PADDING, 0, (z + playerChunk.z) * CHUNK_SIZE_NO_PADDING);
                Vector3Int chunkPosition = new Vector3Int(relativePosition.x / CHUNK_SIZE_NO_PADDING, relativePosition.y / CHUNK_SIZE_NO_PADDING, relativePosition.z / CHUNK_SIZE_NO_PADDING);

                // Does this chunk already exist
                //if (loadedChunks.Cast<Chunk>().Any(chunk => chunk != null && chunk.chunkPosition == chunkPosition)) continue;
                if (ChunkExists(chunkPosition)) continue;

                int a, b;

                // Find a null or far chunk
                for (int i = 0; i < RENDER_DISTANCE_LENGTH; i++)
                {
                    for (int j = 0; j < RENDER_DISTANCE_LENGTH; j++)
                    {
                        if (loadedChunks[i, j] == null)
                        {
                            a = i;
                            b = j;
                            goto FoundNull;
                        }

                        if (loadedChunks[i, j].chunkPosition.x > playerChunk.x + RENDER_DISTANCE ||
                            loadedChunks[i, j].chunkPosition.x < playerChunk.x - RENDER_DISTANCE ||
                            loadedChunks[i, j].chunkPosition.z > playerChunk.z + RENDER_DISTANCE ||
                            loadedChunks[i, j].chunkPosition.z < playerChunk.z - RENDER_DISTANCE)
                        {
                            a = i;
                            b = j;
                            goto FoundFar;
                        }
                    }
                }

                // If there is no null or far chunk
                continue;

                // If there is no null but there is far
                FoundFar:

                // Destroy old chunk
                Destroy(loadedChunks[a, b].gameObject);

                // If there is null
                FoundNull:

                // Generate new chunk
                loadedChunks[a, b] = new Chunk(relativePosition);

                Task.Run(() =>
                {
                    loadedChunks[a, b].LoadChunk();
                    loadedChunks[a, b].CalculateMeshData();
                })
                .ContinueWith(task =>
                {
                    loadedChunks[a, b].gameObject = CreateObject(loadedChunks[a, b].meshes);
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }
    }

    public bool ChunkExists(Vector3Int chunkPosition)
    {
        for (int i = 0; i < RENDER_DISTANCE_LENGTH; i++)
            for (int j = 0; j < RENDER_DISTANCE_LENGTH; j++)
                if (loadedChunks[i, j] != null && loadedChunks[i, j].chunkPosition == chunkPosition)
                    return true;

        return false;
    }

    private GameObject CreateObject(ChunkMesh[] meshes)
    {
        GameObject parentObject = new GameObject("Chunk");

        for (int i = 0; i < meshes.Length; i++)
        {
            ChunkMesh chunkMesh = meshes[i];

            GameObject childObject = new GameObject(chunkMesh.blockType.ToString());
            childObject.transform.parent = parentObject.transform;

            MeshFilter meshFilter = childObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = childObject.AddComponent<MeshRenderer>();
            MeshCollider meshCollider = childObject.AddComponent<MeshCollider>();

            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.vertices = chunkMesh.vertices;
            mesh.triangles = chunkMesh.triangles;

            mesh.RecalculateNormals();

            meshRenderer.material = BlockData.BlockProperties[chunkMesh.blockType].material;
            meshFilter.mesh = mesh;
            meshCollider.sharedMesh = mesh;
        }

        return parentObject;
    }
}