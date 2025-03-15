public static class Constants
{
    public const byte CHUNK_SIZE = 64;
    public const int CHUNK_HEIGHT = 5;
    public const byte RENDER_DISTANCE = 3;
    public const byte WATER_HEIGHT = 50;
    public const int CLOUD_HEIGHT = CHUNK_SIZE * 4;
    public const byte HIGHEST_BLOCK = 250;
    public const byte MOUNTAIN_TRANSITION_START = 115;
    public const byte MOUNTAIN_TRANSITION_END = 130;
    public const int SNOW_BIOME_TRANSITION_START = 500;
    public const int SNOW_BIOME_TRANSITION_END = 550;
    public const int TREE_DENSITY = 7;

    public const byte CHUNK_SIZE_NO_PADDING = CHUNK_SIZE - 2;
    public const byte RENDER_DISTANCE_LENGTH = RENDER_DISTANCE * 2 + 1;
    public const byte MIN_CHUNK_HEIGHT = (CHUNK_SIZE + WATER_HEIGHT) / CHUNK_SIZE;
}