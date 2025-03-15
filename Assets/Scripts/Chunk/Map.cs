using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Constants;

[BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
public struct TerrainJob : IJobParallelFor
{
    [WriteOnly]
    public NativeArray<int> Map;

    [ReadOnly]
    public int2 Position;

    public void Execute(int i)
    {
        float2 position = new float2(i / CHUNK_SIZE + Position.x, i % CHUNK_SIZE + Position.y);

        // Mountains
        float noise1000 = Noise(position, 1000);

        float a = 1f * Noise(position, 5000) +
                        1f * Noise(position, 2000) +
                        1f * noise1000 * Noise(position, 500) +
                        0.5f * noise1000 * Noise(position, 100) +
                        0.25f * noise1000 * Noise(position, 50) +
                        0.1f * noise1000 * Noise(position, 30);

        a /= 1f + 1f + 1f + 0.5f + 0.25f + 0.1f;
        a = math.pow(a, 4f);

        // Hills
        float2 position10000 = position + 10000;

        float b = 1f * Noise(position10000, 1000) +
                0.5f * Noise(position10000, 500) +
                0.25f * Noise(position10000, 250) +
                0.1f * Noise(position10000, 250) * Noise(position10000, 50);

        b /= 1f + 0.5f + 0.25f + 0.1f;
        b *= 0.5f;

        // Height map
        a += b;

        Map[i] = Mathf.RoundToInt(a * (HIGHEST_BLOCK - 2)) + 2;
    }

    public float Noise(float2 position, float frequency)
    {
        position /= frequency;
        return (noise.snoise(position) + 1) / 2;
    }
}

[BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
public struct NoiseJob : IJobParallelFor
{
    [WriteOnly]
    public NativeArray<float> Map;

    [ReadOnly]
    public int MapWidth;

    [ReadOnly]
    public int2 Position;

    [ReadOnly]
    public int Frequency;

    public void Execute(int i)
    {
        float2 position = new float2(i / MapWidth + Position.x, i % MapWidth + Position.y);
        Map[i] = Noise(position, Frequency);
    }

    public float Noise(float2 position, float frequency)
    {
        return (noise.snoise(position / (float)frequency) + 1) / 2;
    }
}

[BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
public struct PeakJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float> InputMap;

    [ReadOnly]
    public int InputMapWidth;

    [WriteOnly]
    public NativeArray<bool> OutputMap;

    [ReadOnly]
    public int OutputMapWidth;

    [ReadOnly]
    public int Radius;

    public void Execute(int i)
    {
        int x = i / OutputMapWidth;
        int z = i % OutputMapWidth;

        float value = InputMap[(z + Radius) + InputMapWidth * (x + Radius)];

        for (int j = -Radius; j <= Radius; j++)
            for (int k = -Radius; k <= Radius; k++)
                if (InputMap[(z + Radius + j) + InputMapWidth * (x + Radius + k)] > value)
                    return;

        OutputMap[i] = true;
    }
}