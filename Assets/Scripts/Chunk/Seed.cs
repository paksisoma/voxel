using Unity.Mathematics;

class Seed
{
    public static readonly int seed;
    public static readonly int2 offset1;
    public static readonly int2 offset2;
    public static readonly int2 offset3;
    public static readonly int2 offset4;
    public static readonly int2 offset5;

    static Seed()
    {
        Random random = new Random(2);

        offset1 = random.NextInt2(new int2(-500000, -500000), new int2(500000, 500000));
        offset2 = random.NextInt2(new int2(-500000, -500000), new int2(500000, 500000));
        offset3 = random.NextInt2(new int2(-500000, -500000), new int2(500000, 500000));
        offset4 = random.NextInt2(new int2(-500000, -500000), new int2(500000, 500000));
        offset5 = random.NextInt2(new int2(-500000, -500000), new int2(500000, 500000));
    }
}