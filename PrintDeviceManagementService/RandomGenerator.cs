namespace PrintDeviceManagementService
{
    internal static class RandomGenerator
    {
        readonly static Random random = new();

        public static TimeSpan GetTime()
        {
            return new(0, 0, 0, 0, random.Next(1000, 4000));
        }
        public static bool GetResult()
        {
            return random.Next(2) != 0;
        }
    }
}
