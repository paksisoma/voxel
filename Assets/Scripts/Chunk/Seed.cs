using Unity.Mathematics;

public static class Seed
{
    private static uint _seed;
    public static uint seed
    {
        get => _seed;
        set
        {
            _seed = value;

            Random random = new Random(value);

            offset1 = random.NextInt2(new int2(-500000, -500000), new int2(500000, 500000));
            offset2 = random.NextInt2(new int2(-500000, -500000), new int2(500000, 500000));
            offset3 = random.NextInt2(new int2(-500000, -500000), new int2(500000, 500000));
            offset4 = random.NextInt2(new int2(-500000, -500000), new int2(500000, 500000));
            offset5 = random.NextInt2(new int2(-500000, -500000), new int2(500000, 500000));
        }
    }

    public static int2 offset1 { get; private set; }
    public static int2 offset2 { get; private set; }
    public static int2 offset3 { get; private set; }
    public static int2 offset4 { get; private set; }
    public static int2 offset5 { get; private set; }
}