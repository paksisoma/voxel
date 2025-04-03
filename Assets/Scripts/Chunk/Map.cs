using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Constants;

[BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
public struct TerrainJob : IJobParallelFor
{
    [WriteOnly]
    public NativeArray<int> Map;

    [ReadOnly]
    public int2 Position1;

    [ReadOnly]
    public int2 Position2;

    public void Execute(int i)
    {
        float2 xy = new float2(i / CHUNK_SIZE, i % CHUNK_SIZE);

        float2 position1 = Position1 + xy;

        // Position 1
        float noise1000 = simplexNoise(position1, 1000);

        float a = 1f * simplexNoise(position1, 5000) +
                        1f * simplexNoise(position1, 2000) +
                        1f * noise1000 * simplexNoise(position1, 500) +
                        0.5f * noise1000 * simplexNoise(position1, 100) +
                        0.25f * noise1000 * simplexNoise(position1, 50) +
                        0.1f * noise1000 * simplexNoise(position1, 30);

        a /= 1f + 1f + 1f + 0.5f + 0.25f + 0.1f;
        a = math.pow(a, 4f);

        // Position 2
        float2 position2 = Position2 + xy;

        float b = 1f * simplexNoise(position2, 1000) +
                0.5f * simplexNoise(position2, 500) +
                0.25f * simplexNoise(position2, 250) +
                0.1f * simplexNoise(position2, 250) * simplexNoise(position2, 50);

        b /= 1f + 0.5f + 0.25f + 0.1f;
        b *= 0.5f;

        // Height map
        a += b;

        Map[i] = (int)(a * (HIGHEST_BLOCK - 2)) + 2;
    }

    public float simplexNoise(float2 position, float frequency)
    {
        return (noise.snoise(position / frequency) + 1) / 2;
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
        Map[i] = simplexNoise(position, Frequency);
    }

    public float simplexNoise(float2 position, float frequency)
    {
        return (noise.snoise(position / frequency) + 1) / 2;
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