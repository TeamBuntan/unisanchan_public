using System;
using UnityEngine;
using unisanchan;

public class VariableCommandHandler
{
    private readonly ScenarioVariableStore variableStore;

    public VariableCommandHandler(
        ScenarioVariableStore variableStore)
    {
        this.variableStore = variableStore;
    }

    public bool TryExecute(ScenarioRow row)
    {
        if (row == null ||
            !string.Equals(
                row.Command?.Trim(),
                "ChangeValue",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!ValueExpressionParser.TryParse(
                row.Arg1,
                out ValueExpression expression))
        {
            Debug.LogError(
                $"[unisanchan] ChangeValueの式が不正です。" +
                $" Excel行: {row.ExcelRow} / 式: {row.Arg1}");
            return true;
        }

        if (!variableStore.Apply(expression))
        {
            Debug.LogError(
                $"[unisanchan] ChangeValueを実行できませんでした。" +
                $" Excel行: {row.ExcelRow} / 式: {row.Arg1}");
        }

        return true;
    }
}