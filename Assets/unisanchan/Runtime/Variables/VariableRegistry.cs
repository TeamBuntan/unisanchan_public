using System.Collections.Generic;
using UnityEngine;

namespace unisanchan
{
    [CreateAssetMenu(
        fileName = "VariableRegistry",
        menuName = "unisanchan/Variable Registry")]
    public class VariableRegistry : ScriptableObject
    {
        [SerializeField]
        private VariableSettings settings = new();

        public IReadOnlyList<VariableDefinition> Variables
            => settings.Variables;
    }
}