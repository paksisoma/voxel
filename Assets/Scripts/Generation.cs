using UnityEngine;
using static Constants;

public class Generation : MonoBehaviour
{
    void Start()
    {
        /*for (byte x = 0; x < 63; x++)
            for (byte y = 0; y < 63; y++)
                for (byte z = 0; z < 63; z++)
                    if ((x + y + z) % 2 == 0)
                        chunk.SetBlock(x, y, z, BlockType.Stone);*/

        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                int aX = i * (CHUNK_SIZE - 2);
                int aZ = j * (CHUNK_SIZE - 2);

                Chunk chunk = gameObject.AddComponent<Chunk>();

                chunk.Initialize(aX, 0, aZ);

                for (byte x = 0; x < CHUNK_SIZE; x++)
                {
                    for (byte z = 0; z < CHUNK_SIZE; z++)
                    {
                        byte height = Noise(aX + x, aZ + z);

                        for (byte y = 0; y < Mathf.Min(height, CHUNK_SIZE); y++)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Stone);
                        }
                    }
                }

                chunk.Render();
            }
        }
    }

    byte Noise(int x, int y)
    {
        const byte surfaceBegin = 5;

        float height = 5f * GetNoiseValue(x, y, 30f);
        height = Mathf.Pow(height, 2);

        return (byte)Mathf.Round(height + surfaceBegin);
    }

    float GetNoiseValue(float x, float y, float frequency)
    {
        float a = x / frequency;
        float b = y / frequency;

        float height = Mathf.PerlinNoise(a, b);

        height = Mathf.Max(height, 0);
        height = Mathf.Min(height, 1);

        return height;
    }
}