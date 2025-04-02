public struct BlockProperties
{
    public readonly int top;
    public readonly int side;
    public readonly int bottom;

    public BlockProperties(int top, int side, int bottom)
    {
        this.top = top;
        this.side = side;
        this.bottom = bottom;
    }
}