namespace SNEngine.API
{
    public static class NativeAPI
    {
        public static double GetFPS()
        {
            return SNEngine.Host.NativeFps;
        }
    }
}
