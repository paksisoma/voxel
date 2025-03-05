using System.Runtime.CompilerServices;
using UnityEngine;
using static Constants;

public static class Utils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int WorldPositionToChunkPosition(Vector3Int position)
    {
        if (position.x < 0)
            position.x -= CHUNK_SIZE_NO_PADDING - 1;

        if (position.z < 0)
            position.z -= CHUNK_SIZE_NO_PADDING - 1;

        position /= CHUNK_SIZE_NO_PADDING;

        return position;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Int WorldPositionToChunkRelativePosition(Vector3Int position)
    {
        Vector3Int chunkPosition = WorldPositionToChunkPosition(position);
        return position - (chunkPosition * CHUNK_SIZE_NO_PADDING) + new Vector3Int(1, 1, 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ManhattanDistance(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z);
    }
}