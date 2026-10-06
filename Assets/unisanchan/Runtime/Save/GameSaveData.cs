using System;

namespace unisanchan
{
    [Serializable]
    public class GameSaveData
    {
        public int Version = 1;

        public string CheckpointID;
        public string ScenePath;

        public string EventID;
        public string BlockID;
        public string Trigger;

        public string SavedAtUtc;

        public VariableSaveData GameVariables =
            new VariableSaveData();
    }
}