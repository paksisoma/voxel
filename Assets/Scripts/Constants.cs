public static class Constants
{
    public const byte CHUNK_SIZE = 64;
    public const int CHUNK_HEIGHT = 5;
    public const byte WATER_HEIGHT = 50;
    public const int CLOUD_HEIGHT = CHUNK_SIZE * 4;
    public const byte HIGHEST_BLOCK = 250;
    public const byte MOUNTAIN_TRANSITION_START = 115;
    public const byte MOUNTAIN_TRANSITION_END = 130;
    public const int SNOW_BIOME_TRANSITION_START = 500;
    public const int SNOW_BIOME_TRANSITION_END = 550;
    public const int TREE_DENSITY = 7;

    public const int IRON_START_POSITION = 0;
    public const int COPPER_START_POSITION = 2000;
    public const int GOLD_START_POSITION = 4000;
    public const int RUBY_START_POSITION = 6000;
    public const int DIAMOND_START_POSITION = 8000;

    public const float PREDATOR_SPAWN_RATE = 0.1f;
    public const float PREY_SPAWN_RATE = 0.1f;

    public const float ARMOR_RECHARGE_DELAY = 15f;

    public const float PUNCH_DAMAGE = 0.05f;

    public const float INIT_HEALTH = 1f;
    public const float INIT_HUNGER = 0f;
    public const float INIT_THIRST = 0f;
    public const float INIT_TEMPERATURE = 0.5f;
    public const float INIT_TIME = 45f;

    public const int DEFAULT_FPS_LIMIT = 60;
    public const int DEFAULT_RENDER_DISTANCE = 5;
    public const int DEFAULT_VSYNC = 0;
    public const int DEFAULT_DISPLAY_MODE = 1;

    public const byte CHUNK_SIZE_NO_PADDING = CHUNK_SIZE - 2;
    public const byte MIN_CHUNK_HEIGHT = (CHUNK_SIZE + WATER_HEIGHT) / CHUNK_SIZE;
}