using System;
using System.Collections.Generic;

namespace unisanchan
{
    [Serializable]
    public class VariableSaveData
    {
        public List<VariableSaveEntry> Variables =
            new List<VariableSaveEntry>();
    }

    [Serializable]
    public class VariableSaveEntry
    {
        public string VariableID;
        public VariableType Type;
        public string Value;
    }
}