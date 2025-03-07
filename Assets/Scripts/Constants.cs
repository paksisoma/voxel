public static class Constants
{
    public const byte CHUNK_SIZE = 64;
    public const byte RENDER_DISTANCE = 3;
    public const byte WATER_HEIGHT = 50;
    public const byte HIGHEST_BLOCK = 250;
    public const byte MOUNTAIN_HEIGHT_START = 115;
    public const int SNOW_BIOME_START = 500;

    public const byte CHUNK_SIZE_NO_PADDING = CHUNK_SIZE - 2;
    public const byte RENDER_DISTANCE_LENGTH = RENDER_DISTANCE * 2 + 1;
    public const byte MIN_CHUNK_HEIGHT = (CHUNK_SIZE + WATER_HEIGHT) / CHUNK_SIZE;
}