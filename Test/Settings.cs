namespace Test
{
    public static class Settings
    {
#if DEBUG
        public const int Timeout = 60 * 1000;
#else
        public const int Timeout = 5 * 60 * 1000;
#endif
    }
}
