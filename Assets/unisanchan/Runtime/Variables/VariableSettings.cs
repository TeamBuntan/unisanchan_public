using System;
using System.Collections.Generic;

namespace unisanchan
{
    [Serializable]
    public class VariableSettings
    {
        public List<VariableDefinition> Variables = new();
    }
}