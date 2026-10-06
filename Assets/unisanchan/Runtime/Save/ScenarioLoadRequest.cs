namespace unisanchan
{
    public static class ScenarioLoadRequest
    {
        private static GameSaveData pendingData;

        public static void Queue(GameSaveData data)
        {
            pendingData = data;
        }

        public static bool TryPeek(out GameSaveData data)
        {
            data = pendingData;
            return data != null;
        }

        public static void Clear()
        {
            pendingData = null;
        }
    }
}