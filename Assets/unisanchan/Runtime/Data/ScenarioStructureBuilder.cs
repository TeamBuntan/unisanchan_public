using System;
using System.Collections.Generic;
using UnityEngine;

public static class ScenarioStructureBuilder
{
    public static bool TryBuild(
        List<ScenarioRow> sourceRows,
        List<ScenarioEvent> destinationEvents)
    {
        if (destinationEvents == null)
        {
            Debug.LogError(
                "[UnisanChi] ScenarioEventの出力先がnullです。");

            return false;
        }

        destinationEvents.Clear();

        if (sourceRows == null || sourceRows.Count == 0)
        {
            Debug.LogError(
                "[UnisanChi] シナリオ行が存在しないため、" +
                "ScenarioEventsを構築できません。");

            return false;
        }

        if (!ValidateSavePoints(sourceRows))
            return false;

        if (!ValidateBlockIds(sourceRows))
            return false;
        
        if (!ValidateModalBlocksAreExclusive(sourceRows))
            return false;

        if (!ValidateBlocksHaveSingleTrigger(sourceRows))
            return false;

        if (!ValidateBlocksDoNotCrossLabels(sourceRows))
            return false;

        string currentLabel = "";

        foreach (ScenarioRow row in sourceRows)
        {
            if (row == null)
                continue;

            if (IsLabelRow(row))
            {
                currentLabel = Normalize(row.EventID);
                continue;
            }

            string eventId = Normalize(row.EventID);
            string blockId = Normalize(row.BlockID);
            string rowTrigger = Normalize(row.Trigger);

            if (string.IsNullOrEmpty(eventId) ||
                string.IsNullOrEmpty(blockId))
            {
                continue;
            }

            ScenarioEvent scenarioEvent =
                destinationEvents.Find(
                    e => Normalize(e.EventID) == eventId);

            if (scenarioEvent == null)
            {
                scenarioEvent = new ScenarioEvent
                {
                    EventID = eventId
                };

                destinationEvents.Add(scenarioEvent);
            }

            ScenarioBlock block =
                scenarioEvent.Blocks.Find(
                    b => Normalize(b.BlockID) == blockId);

            if (block == null)
            {
                block = new ScenarioBlock
                {
                    EventID = eventId,
                    BlockID = blockId
                };

                scenarioEvent.Blocks.Add(block);
            }

            if (string.IsNullOrEmpty(rowTrigger))
            {
                if (block.TriggerGroups.Count == 0)
                {
                    if (string.Equals(
                            Normalize(row.Command),
                            "Choice",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        ScenarioTriggerGroup choiceGroup =
                            new ScenarioTriggerGroup
                            {
                                Trigger = ""
                            };

                        block.TriggerGroups.Add(choiceGroup);
                        choiceGroup.Rows.Add(row);

                        currentLabel = "";
                        continue;
                    }

                    // ラベル直後のTrigger空欄グループ
                    if (!string.IsNullOrEmpty(currentLabel))
                    {
                        ScenarioTriggerGroup labelEntryGroup =
                            new ScenarioTriggerGroup
                            {
                                Trigger = ""
                            };

                        block.TriggerGroups.Add(labelEntryGroup);
                        labelEntryGroup.Rows.Add(row);

                        currentLabel = "";
                        continue;
                    }

                    string excelRow =
                        string.IsNullOrWhiteSpace(row.ExcelRow)
                            ? "不明"
                            : row.ExcelRow;

                    Debug.LogError(
                        $"[UnisanChi] Excel行{excelRow}: " +
                        $"{eventId} / Block{blockId} のTriggerが空欄です。\n\n" +
                        "このBlockには、継承元となるTriggerがありません。\n\n" +
                        $"Command: {Normalize(row.Command)}\n\n" +
                        "【修正方法】\n" +
                        "Blockの最初の実行行に、" +
                        "Auto・Advance・Input1などのTriggerを指定してください。"
                    );

                    return false;
                }

                ScenarioTriggerGroup lastGroup =
                    block.TriggerGroups[
                        block.TriggerGroups.Count - 1];

                lastGroup.Rows.Add(row);
                continue;
            }

            ScenarioTriggerGroup triggerGroup =
                block.TriggerGroups.Find(
                    group => string.Equals(
                        Normalize(group.Trigger),
                        rowTrigger,
                        StringComparison.OrdinalIgnoreCase));

            if (triggerGroup == null)
            {
                triggerGroup = new ScenarioTriggerGroup
                {
                    Trigger = rowTrigger
                };

                block.TriggerGroups.Add(triggerGroup);
            }

            triggerGroup.Rows.Add(row);
        }

        SortBlocks(destinationEvents);

        return true;
    }

    private static bool ValidateSavePoints(
        List<ScenarioRow> sourceRows)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        bool isValid = true;

        foreach (ScenarioRow row in sourceRows)
        {
            if (row == null || IsLabelRow(row))
                continue;

            bool isSavePoint = string.Equals(
                Normalize(row.Command),
                "SavePoint",
                StringComparison.OrdinalIgnoreCase);

            bool isAutoSave = string.Equals(
                Normalize(row.Command),
                "AutoSave",
                StringComparison.OrdinalIgnoreCase);

            if (!isSavePoint && !isAutoSave)
                continue;

            string id = Normalize(row.Arg1);

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError(
                    $"[unisanchan] {row.Command} の Arg1 に " +
                    $"チェックポイント ID が必要です。Excel行: {row.ExcelRow}");
                isValid = false;
            }
            else if (!ids.Add(id))
            {
                Debug.LogError(
                    $"[unisanchan] チェックポイント ID が重複しています: " +
                    $"{id} / Excel行: {row.ExcelRow}");
                isValid = false;
            }

            int rowsInBlock = sourceRows.FindAll(other =>
                other != null &&
                !IsLabelRow(other) &&
                Normalize(other.EventID) == Normalize(row.EventID) &&
                Normalize(other.BlockID) == Normalize(row.BlockID)
            ).Count;

            if (rowsInBlock != 1)
            {
                Debug.LogError(
                    $"[unisanchan] {row.Command} は単独の Block に " +
                    $"置いてください。Excel行: {row.ExcelRow}");
                isValid = false;
            }
        }

        return isValid;
    }

    private static bool ValidateBlockIds(
        List<ScenarioRow> sourceRows)
    {
        bool isValid = true;

        foreach (ScenarioRow row in sourceRows)
        {
            if (row == null || IsLabelRow(row))
                continue;

            string blockId = Normalize(row.BlockID);

            if (string.IsNullOrEmpty(blockId))
                continue;

            bool isThreeDigitNumber =
                blockId.Length == 3;

            if (isThreeDigitNumber)
            {
                foreach (char character in blockId)
                {
                    if (character < '0' || character > '9')
                    {
                        isThreeDigitNumber = false;
                        break;
                    }
                }
            }

            if (isThreeDigitNumber)
                continue;

            string excelRow =
                string.IsNullOrWhiteSpace(row.ExcelRow)
                    ? "不明"
                    : row.ExcelRow;

            Debug.LogError(
                $"[unisanchan] Excel行{excelRow}: " +
                $"BlockID「{blockId}」は使用できません。\n\n" +
                "BlockIDには3桁の半角数字を指定してください。\n" +
                "例: 000、005、010、120\n\n" +
                $"EventID: {Normalize(row.EventID)}\n" +
                $"Command: {Normalize(row.Command)}");

            isValid = false;
        }

        return isValid;
    }

    private static bool ValidateModalBlocksAreExclusive(
        List<ScenarioRow> sourceRows)
    {
        Dictionary<string, List<ScenarioRow>> blockRows =
            new Dictionary<string, List<ScenarioRow>>();

        foreach (ScenarioRow row in sourceRows)
        {
            if (row == null ||
                IsLabelRow(row))
            {
                continue;
            }

            string eventId =
                Normalize(row.EventID);

            string blockId =
                Normalize(row.BlockID);

            if (string.IsNullOrEmpty(eventId) ||
                string.IsNullOrEmpty(blockId))
            {
                continue;
            }

            string blockKey =
                eventId + "|" + blockId;

            if (!blockRows.TryGetValue(
                    blockKey,
                    out List<ScenarioRow> rowsInBlock))
            {
                rowsInBlock =
                    new List<ScenarioRow>();

                blockRows.Add(
                    blockKey,
                    rowsInBlock);
            }

            rowsInBlock.Add(row);
        }

        bool isValid = true;

        foreach (KeyValuePair<string, List<ScenarioRow>> pair
            in blockRows)
        {
            List<ScenarioRow> rowsInBlock =
                pair.Value;

            ScenarioRow modalRow = null;

            foreach (ScenarioRow row in rowsInBlock)
            {
                if (string.Equals(
                        Normalize(row.Command),
                        "Modal",
                        StringComparison.OrdinalIgnoreCase))
                {
                    modalRow = row;
                    break;
                }
            }

            if (modalRow == null ||
                rowsInBlock.Count == 1)
            {
                continue;
            }

            isValid = false;

            List<string> commandDescriptions =
                new List<string>();

            foreach (ScenarioRow row in rowsInBlock)
            {
                string command =
                    Normalize(row.Command);

                if (string.IsNullOrEmpty(command))
                    command = "（空欄）";

                string excelRow =
                    string.IsNullOrWhiteSpace(row.ExcelRow)
                        ? "不明"
                        : row.ExcelRow;

                commandDescriptions.Add(
                    $"Excel行{excelRow}: {command}");
            }

            Debug.LogError(
                "[unisanchan] Modalは単独のBlockに記述してください。\n\n" +
                $"EventID: {Normalize(modalRow.EventID)}\n" +
                $"BlockID: {Normalize(modalRow.BlockID)}\n\n" +
                "同じBlockに存在するCommand:\n" +
                string.Join(
                    "\n",
                    commandDescriptions) +
                "\n\n【修正方法】\n" +
                "Modal以外のCommandを、前または次のBlockへ移動してください。"
            );
        }

        return isValid;
    }

    private static bool ValidateBlocksHaveSingleTrigger(
        List<ScenarioRow> sourceRows)
    {
        Dictionary<string, string> blockTriggers =
            new Dictionary<string, string>();

        Dictionary<string, string> firstTriggerRows =
            new Dictionary<string, string>();

        HashSet<string> reportedBlockKeys =
            new HashSet<string>();

        bool isValid = true;

        foreach (ScenarioRow row in sourceRows)
        {
            if (row == null ||
                IsLabelRow(row))
            {
                continue;
            }

            string eventId =
                Normalize(row.EventID);

            string blockId =
                Normalize(row.BlockID);

            string trigger =
                Normalize(row.Trigger);

            if (string.IsNullOrEmpty(eventId) ||
                string.IsNullOrEmpty(blockId) ||
                string.IsNullOrEmpty(trigger))
            {
                continue;
            }

            string blockKey =
                eventId + "|" + blockId;

            string excelRow =
                string.IsNullOrWhiteSpace(row.ExcelRow)
                    ? "不明"
                    : row.ExcelRow;

            if (!blockTriggers.TryGetValue(
                    blockKey,
                    out string firstTrigger))
            {
                blockTriggers[blockKey] =
                    trigger;

                firstTriggerRows[blockKey] =
                    excelRow;

                continue;
            }

            if (string.Equals(
                    firstTrigger,
                    trigger,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            isValid = false;

            if (!reportedBlockKeys.Add(blockKey))
                continue;

            Debug.LogError(
                "[UnisanChi] Scenario構造エラー: " +
                $"EventID「{eventId}」/ BlockID「{blockId}」で、" +
                $"Trigger「{firstTrigger}」と「{trigger}」が競合しています。\n\n" +

                $"最初のTrigger: {firstTrigger}\n" +
                $"Excel行: {firstTriggerRows[blockKey]}\n\n" +

                $"競合Trigger: {trigger}\n" +
                $"Excel行: {excelRow}\n\n" +

                "現在のバージョンでは、同一EventID・BlockIDへの" +
                "複数Trigger指定を処理できません。\n" +
                "該当するExcel行のTriggerまたはBlockIDを確認してください。"
            );
        }

        return isValid;
    }

    private static bool ValidateBlocksDoNotCrossLabels(
        List<ScenarioRow> sourceRows)
    {
        Dictionary<string, int> blockSections =
            new Dictionary<string, int>();

        HashSet<string> reportedBlockKeys =
            new HashSet<string>();

        int currentSection = 0;
        string currentLabel = "";
        bool isValid = true;

        foreach (ScenarioRow row in sourceRows)
        {
            if (row == null)
                continue;

            if (IsLabelRow(row))
            {
                currentSection++;
                currentLabel = Normalize(row.EventID);
                continue;
            }

            string eventId = Normalize(row.EventID);
            string blockId = Normalize(row.BlockID);

            if (string.IsNullOrEmpty(eventId) ||
                string.IsNullOrEmpty(blockId))
            {
                continue;
            }

            string blockKey = eventId + "|" + blockId;

            if (!blockSections.TryGetValue(
                    blockKey,
                    out int firstSection))
            {
                blockSections[blockKey] = currentSection;
                continue;
            }

            if (firstSection == currentSection)
                continue;

            if (!reportedBlockKeys.Add(blockKey))
                continue;

            string excelRow =
                string.IsNullOrWhiteSpace(row.ExcelRow)
                    ? "不明"
                    : row.ExcelRow;

            Debug.LogError(
                $"[UnisanChi] Excel行{excelRow}: " +
                $"{eventId} / Block{blockId} が " +
                $"ラベル {currentLabel} をまたいでいます。\n\n" +
                "同じEventID・BlockIDを、ラベルの前後に分けて" +
                "記述することはできません。\n\n" +
                $"EventID: {eventId}\n" +
                $"BlockID: {blockId}\n" +
                $"ラベル: {currentLabel}\n" +
                $"Command: {Normalize(row.Command)}\n\n" +
                "【修正方法】\n" +
                $"Excel行{excelRow}付近にある、ラベル後のBlockIDを" +
                "別の値へ変更してください。\n" +
                $"例: BlockID {blockId} → 115"
            );

            isValid = false;
        }

        return isValid;
    }

    private static void SortBlocks(
        List<ScenarioEvent> scenarioEvents)
    {
        foreach (ScenarioEvent scenarioEvent in scenarioEvents)
        {
            scenarioEvent.Blocks.Sort(
                (a, b) =>
                {
                    bool aParsed =
                        int.TryParse(
                            Normalize(a.BlockID),
                            out int aNumber);

                    bool bParsed =
                        int.TryParse(
                            Normalize(b.BlockID),
                            out int bNumber);

                    if (aParsed && bParsed)
                        return aNumber.CompareTo(bNumber);

                    if (aParsed)
                        return -1;

                    if (bParsed)
                        return 1;

                    return string.Compare(
                        Normalize(a.BlockID),
                        Normalize(b.BlockID),
                        StringComparison.OrdinalIgnoreCase);
                });
        }
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
}