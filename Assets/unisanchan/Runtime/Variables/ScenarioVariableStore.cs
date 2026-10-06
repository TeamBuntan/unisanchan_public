using System;
using System.Globalization;
using System.Collections.Generic;

namespace unisanchan
{
    public class ScenarioVariableStore
    {
        private readonly Dictionary<string, object> values = new();
        private readonly Dictionary<string, VariableType> types = new();

        public bool Contains(string variableId)
        {
            return values.ContainsKey(variableId);
        }

        public void Set(string variableId, object value)
        {
            values[variableId] = value;

            if (value is int)
            {
                types[variableId] = VariableType.Int;
            }
            else if (value is float)
            {
                types[variableId] = VariableType.Float;
            }
            else if (value is bool)
            {
                types[variableId] = VariableType.Bool;
            }
            else if (value is string)
            {
                types[variableId] = VariableType.String;
            }
        }

        public bool TryGet(string variableId, out object value)
        {
            return values.TryGetValue(variableId, out value);
        }

        public VariableSaveData Capture(
            IEnumerable<VariableDefinition> definitions,
            VariableSaveType saveType)
        {
            var data = new VariableSaveData();

            if (definitions == null)
                return data;

            foreach (VariableDefinition definition in definitions)
            {
                if (definition == null || definition.Save != saveType)
                    continue;

                string id = definition.VariableID?.Trim();

                if (string.IsNullOrEmpty(id) ||
                    !values.TryGetValue(id, out object value))
                {
                    continue;
                }

                data.Variables.Add(new VariableSaveEntry
                {
                    VariableID = id,
                    Type = definition.Type,
                    Value = Convert.ToString(
                        value,
                        CultureInfo.InvariantCulture) ?? ""
                });
            }

            return data;
        }

        public bool Restore(
            VariableSaveData data,
            IEnumerable<VariableDefinition> definitions,
            VariableSaveType saveType)
        {
            if (data?.Variables == null || definitions == null)
                return false;

            var allowedTypes = new Dictionary<string, VariableType>();

            foreach (VariableDefinition definition in definitions)
            {
                if (definition == null || definition.Save != saveType)
                    continue;

                string id = definition.VariableID?.Trim();

                if (!string.IsNullOrEmpty(id) && values.ContainsKey(id))
                    allowedTypes[id] = definition.Type;
            }

            var restoredValues = new Dictionary<string, object>();

            foreach (VariableSaveEntry entry in data.Variables)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.VariableID))
                    return false;

                string id = entry.VariableID.Trim();

                // 定義から削除された変数は読み飛ばす。
                if (!allowedTypes.TryGetValue(id, out VariableType type))
                    continue;

                if (entry.Type != type || restoredValues.ContainsKey(id))
                    return false;

                object value;

                switch (type)
                {
                    case VariableType.Bool:
                        if (!bool.TryParse(entry.Value, out bool boolValue))
                            return false;
                        value = boolValue;
                        break;

                    case VariableType.Int:
                        if (!int.TryParse(
                                entry.Value,
                                NumberStyles.Integer,
                                CultureInfo.InvariantCulture,
                                out int intValue))
                            return false;
                        value = intValue;
                        break;

                    case VariableType.Float:
                        if (!float.TryParse(
                                entry.Value,
                                NumberStyles.Float,
                                CultureInfo.InvariantCulture,
                                out float floatValue) ||
                            float.IsNaN(floatValue) ||
                            float.IsInfinity(floatValue))
                        {
                            return false;
                        }
                        value = floatValue;
                        break;

                    case VariableType.String:
                        value = entry.Value ?? "";
                        break;

                    default:
                        return false;
                }

                restoredValues.Add(id, value);
            }

            // 全件の検証が済んでから反映する。
            foreach (var item in restoredValues)
                values[item.Key] = item.Value;

            return true;
        }

        public double? ResolveNumber(string variableId)
        {
            if (string.IsNullOrWhiteSpace(variableId))
                return null;

            string normalizedId = variableId.Trim();

            if (!values.TryGetValue(
                    normalizedId,
                    out object value))
            {
                return null;
            }

            if (!types.TryGetValue(
                    normalizedId,
                    out VariableType variableType))
            {
                return null;
            }

            switch (variableType)
            {
                case VariableType.Int:
                    return value is int intValue
                        ? intValue
                        : null;

                case VariableType.Float:
                    return value is float floatValue
                        ? floatValue
                        : null;

                default:
                    return null;
            }
        }

        public bool? ResolveBool(string variableId)
        {
            if (string.IsNullOrWhiteSpace(variableId))
                return null;

            string normalizedId = variableId.Trim();

            if (!values.TryGetValue(
                    normalizedId,
                    out object value))
            {
                return null;
            }

            if (!types.TryGetValue(
                    normalizedId,
                    out VariableType variableType))
            {
                return null;
            }

            if (variableType != VariableType.Bool)
                return null;

            return value is bool boolValue
                ? boolValue
                : null;
        }

        public bool Apply(ValueExpression expression)
        {
            string variableId =
                expression.VariableID?.Trim() ?? "";

            if (string.IsNullOrEmpty(variableId))
                return false;

            if (!values.ContainsKey(variableId))
                return false;

            if (!types.TryGetValue(
                variableId,
                out VariableType variableType))
            {
                return false;
            }

            if (variableType == VariableType.Int)
            {
                if (!ExpressionEvaluator.TryEvaluateNumber(
                        expression.Value,
                        ResolveNumber,
                        out double evaluatedValue))
                {
                    return false;
                }

                int operand = (int)evaluatedValue;

                int currentValue =
                    values[variableId] is int intValue
                        ? intValue
                        : 0;

                switch (expression.Operator)
                {
                    case ValueOperator.Set:
                        values[variableId] = operand;
                        return true;

                    case ValueOperator.Add:
                        values[variableId] =
                            currentValue + operand;
                        return true;

                    case ValueOperator.Subtract:
                        values[variableId] =
                            currentValue - operand;
                        return true;
                    
                    case ValueOperator.Multiply:
                        values[variableId] =
                            currentValue * operand;
                        return true;

                    case ValueOperator.Divide:
                        if (operand == 0)
                            return false;

                        values[variableId] =
                            currentValue / operand;
                        return true;
                    
                    case ValueOperator.Modulo:
                        if (operand == 0)
                            return false;

                        values[variableId] =
                            currentValue % operand;
                        return true;
                }
            }

            return false;
        }

        public void Initialize(IEnumerable<VariableDefinition> definitions)
        {
            values.Clear();
            types.Clear();

            if (definitions == null)
                return;

            foreach (VariableDefinition definition in definitions)
            {
                if (definition == null)
                    continue;

                string variableId =
                    definition.VariableID?.Trim() ?? "";

                if (string.IsNullOrEmpty(variableId))
                    continue;

                object initialValue = definition.Type switch
                {
                    VariableType.Bool =>
                        bool.TryParse(
                            definition.InitialValue,
                            out bool boolValue)
                            ? boolValue
                            : false,

                    VariableType.Int =>
                        int.TryParse(
                            definition.InitialValue,
                            out int intValue)
                            ? intValue
                            : 0,

                    VariableType.Float =>
                        float.TryParse(
                            definition.InitialValue,
                            out float floatValue)
                            ? floatValue
                            : 0f,

                    VariableType.String =>
                        definition.InitialValue ?? "",

                    _ => null
                };

                values[variableId] = initialValue;
                types[variableId] = definition.Type;
            }
        }

        public void Clear()
        {
            values.Clear();
            types.Clear();
        }
    }
}