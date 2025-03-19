using System.Runtime.CompilerServices;
using UnityEngine;
using static Constants;

public static class Utils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int WorldPositionToChunkPosition(Vector3Int worldPosition)
    {
        int x = Mathf.FloorToInt((float)worldPosition.x / CHUNK_SIZE_NO_PADDING);
        int y = Mathf.FloorToInt((float)worldPosition.y / CHUNK_SIZE_NO_PADDING);
        int z = Mathf.FloorToInt((float)worldPosition.z / CHUNK_SIZE_NO_PADDING);

        return new Vector3Int(x, y, z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int WorldPositionToChunkRelativePosition(Vector3Int chunkPosition, Vector3Int worldPosition)
    {
        return worldPosition - (chunkPosition * CHUNK_SIZE_NO_PADDING) + new Vector3Int(1, 1, 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ManhattanDistance(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z);
    }
}