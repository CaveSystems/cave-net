namespace Test
{
    public static class Settings
    {
#if DEBUG

        #region Public Fields

        public const int Timeout = 60 * 1000;

        #endregion Public Fields

#else
        public const int Timeout = 5 * 60 * 1000;
#endif
    }
}
