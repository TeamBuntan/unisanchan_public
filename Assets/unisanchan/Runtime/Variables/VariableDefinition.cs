using System;

namespace unisanchan
{
    [Serializable]
    public class VariableDefinition
    {
        public string VariableID;
        public VariableType Type;
        public string InitialValue;
        public VariableSaveType Save;
    }

    public enum VariableType
    {
        Bool,
        Int,
        Float,
        String
    }

    public enum VariableSaveType
    {
        Game,
        System,
        No,
        Fixed
    }
}