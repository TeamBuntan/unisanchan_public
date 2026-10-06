using System;
using System.Collections.Generic;
using UnityEngine;

namespace unisanchan
{
    public static class ScenarioCheckpointResolver
    {
        public static bool ValidateCheckpoints(
            IReadOnlyList<ScenarioRow> rows,
            ScenarioNavigator navigator)
        {
            if (rows == null || navigator == null)
                return false;

            var ids = new HashSet<string>(StringComparer.Ordinal);
            bool isValid = true;

            foreach (ScenarioRow row in rows)
            {
                if (row == null)
                    continue;

                string command = row.Command?.Trim();

                if (!string.Equals(
                        command, "SavePoint",
                        StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(
                        command, "AutoSave",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string id = row.Arg1?.Trim();

                if (string.IsNullOrEmpty(id) || !ids.Add(id))
                {
                    Debug.LogError(
                        $"[unisanchan] チェックポイント ID が空欄、または" +
                        $"重複しています: {id} / Excel行: {row.ExcelRow}");
                    isValid = false;
                }

                string resumeLabel = row.Arg2?.Trim();

                if (string.IsNullOrEmpty(resumeLabel) ||
                    !navigator.ContainsLabel(resumeLabel))
                {
                    Debug.LogError(
                        $"[unisanchan] チェックポイントの Arg2 に" +
                        $"有効な再開用ラベルが必要です。\n" +
                        $"ID: {id} / Arg2: {resumeLabel} / " +
                        $"Excel行: {row.ExcelRow}");
                    isValid = false;
                }
            }

            return isValid;
        }
        
        public static bool TryResolve(
            IReadOnlyList<ScenarioRow> rows,
            ScenarioNavigator navigator,
            string checkpointID,
            out int resumeIndex)
        {
            resumeIndex = -1;

            if (rows == null ||
                navigator == null ||
                string.IsNullOrWhiteSpace(checkpointID))
            {
                return false;
            }

            foreach (ScenarioRow checkpoint in rows)
            {
                if (checkpoint == null ||
                    !string.Equals(
                        checkpoint.Arg1?.Trim(),
                        checkpointID.Trim(),
                        StringComparison.Ordinal))
                {
                    continue;
                }

                string command = checkpoint.Command?.Trim();

                if (!string.Equals(
                        command, "SavePoint",
                        StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(
                        command, "AutoSave",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!navigator.TryGetTarget(
                        checkpoint.Arg2,
                        out string eventID,
                        out string blockID,
                        out string trigger))
                {
                    return false;
                }

                for (int i = 0; i < rows.Count; i++)
                {
                    ScenarioRow candidate = rows[i];

                    if (candidate != null &&
                        string.Equals(
                            candidate.EventID?.Trim(), eventID,
                            StringComparison.Ordinal) &&
                        string.Equals(
                            candidate.BlockID?.Trim(), blockID,
                            StringComparison.Ordinal) &&
                        string.Equals(
                            candidate.Trigger?.Trim() ?? "", trigger,
                            StringComparison.Ordinal))
                    {
                        resumeIndex = i;
                        return true;
                    }
                }

                return false;
            }

            return false;
        }
    }
}