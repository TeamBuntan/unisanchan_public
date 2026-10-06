using System.Collections.Generic;
using System.Linq;

public static class ScenarioRowNormalizer
{
    public static List<ScenarioRow> Normalize(
        List<ScenarioRow> sourceRows)
    {
        List<ScenarioRow> result =
            new List<ScenarioRow>();

        if (sourceRows == null || sourceRows.Count == 0)
            return result;

        int startIndex = 0;

        while (startIndex < sourceRows.Count)
        {
            string eventId =
                NormalizeText(sourceRows[startIndex].EventID);

            int endIndex = startIndex + 1;

            // 同じEventIDが連続している範囲を取得
            while (
                endIndex < sourceRows.Count &&
                NormalizeText(sourceRows[endIndex].EventID) == eventId)
            {
                endIndex++;
            }

            List<ScenarioRow> eventRows =
                sourceRows.GetRange(
                    startIndex,
                    endIndex - startIndex);

            // BlockIDを数値として昇順に整理。
            // 同じBlockID内ではTriggerごとに固める。
            IEnumerable<ScenarioRow> normalizedEventRows =
                eventRows
                    .Select((row, index) => new
                    {
                        Row = row,
                        OriginalIndex = index,
                        BlockNumber =
                            int.TryParse(
                                NormalizeText(row.BlockID),
                                out int blockNumber)
                                    ? blockNumber
                                    : int.MaxValue
                    })
                    .GroupBy(
                        item => NormalizeText(item.Row.BlockID))
                    .OrderBy(
                        group => group.Min(
                            item => item.BlockNumber))
                    .ThenBy(
                        group => group.Min(
                            item => item.OriginalIndex))
                    .SelectMany(
                        blockGroup =>
                            blockGroup
                                .GroupBy(
                                    item => NormalizeText(
                                        item.Row.Trigger))
                                .SelectMany(
                                    triggerGroup =>
                                        triggerGroup
                                            .OrderBy(
                                                item =>
                                                    item.OriginalIndex)
                                            .Select(
                                                item => item.Row)));

            result.AddRange(normalizedEventRows);
            startIndex = endIndex;
        }

        return result;
    }

    private static string NormalizeText(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? ""
            : value.Trim();
    }
}