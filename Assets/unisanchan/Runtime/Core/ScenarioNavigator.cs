using System.Collections.Generic;
using UnityEngine;

public class ScenarioNavigator
{
    private readonly Dictionary<string, string> labelTargets =
        new Dictionary<string, string>();

    private readonly Dictionary<string, string> labelScenarioNames =
        new Dictionary<string, string>();

    public void BuildLabelTargets(
        List<ScenarioRow> sourceRows,
        string scenarioName)
    {
        if (sourceRows == null || sourceRows.Count == 0)
            return;

        for (int i = 0; i < sourceRows.Count; i++)
        {
            ScenarioRow row = sourceRows[i];

            if (!IsLabelRow(row))
                continue;

            string label = Normalize(row.EventID);

            if (labelTargets.ContainsKey(label))
            {
                labelScenarioNames.TryGetValue(
                    label,
                    out string existingScenarioName);

                Debug.LogError(
                    "[unisanchan] 同名のLabelが複数のScenarioに存在します。\n" +
                    $"Label: {label}\n" +
                    $"登録済みScenario: {existingScenarioName}\n" +
                    $"重複Scenario: {scenarioName}"
                );

                continue;
            }

            labelScenarioNames[label] = scenarioName;

            bool targetFound = false;

            for (int j = i + 1; j < sourceRows.Count; j++)
            {
                ScenarioRow targetRow = sourceRows[j];

                if (targetRow == null)
                    continue;

                if (IsLabelRow(targetRow))
                    continue;

                string eventId = Normalize(targetRow.EventID);
                string blockId = Normalize(targetRow.BlockID);

                if (string.IsNullOrEmpty(eventId) ||
                    string.IsNullOrEmpty(blockId))
                {
                    continue;
                }

                string trigger = Normalize(targetRow.Trigger);

                labelTargets[label] =
                    eventId + "|" + blockId + "|" + trigger;

                targetFound = true;

                break;
            }

            if (!targetFound)
            {
                Debug.LogError(
                    "[unisanchan] Labelに有効な遷移先がありません。\n" +
                    $"Label: {label}\n" +
                    $"Scenario: {scenarioName}"
                );
            }
        }
    }

    public bool TryGetTarget(
        string label,
        out string eventId,
        out string blockId,
        out string trigger)
    {
        eventId = "";
        blockId = "";
        trigger = "";

        string targetLabel = Normalize(label);

        if (string.IsNullOrEmpty(targetLabel))
            return false;

        if (!labelTargets.TryGetValue(
                targetLabel,
                out string targetKey))
        {
            return false;
        }

        string[] parts = targetKey.Split('|');

        if (parts.Length != 3)
            return false;

        eventId = Normalize(parts[0]);
        blockId = Normalize(parts[1]);
        trigger = Normalize(parts[2]);

        return true;
    }

    public bool ContainsLabel(string label)
    {
        string targetLabel = Normalize(label);

        if (string.IsNullOrEmpty(targetLabel))
            return false;

        return labelTargets.ContainsKey(targetLabel);
    }

    private static bool IsLabelRow(ScenarioRow row)
    {
        if (row == null)
            return false;

        return Normalize(row.EventID).StartsWith("*");
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? ""
            : value.Trim();
    }

    public void Clear()
    {
        labelTargets.Clear();
        labelScenarioNames.Clear();
    }
}