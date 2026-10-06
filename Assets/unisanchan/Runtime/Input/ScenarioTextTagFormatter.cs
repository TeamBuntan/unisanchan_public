using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public static class ScenarioTextTagFormatter
{
    private static readonly Regex InputTagPattern =
        new Regex(
            @"\{input:([A-Za-z0-9_]+)\}",
            RegexOptions.Compiled |
            RegexOptions.IgnoreCase);

    public static string Format(
        string text,
        ScenarioInputController inputController)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        if (inputController == null)
            return text;

        return InputTagPattern.Replace(
            text,
            match => ResolveTag(
                match,
                inputController));
    }

    private static string ResolveTag(
        Match match,
        ScenarioInputController inputController)
    {
        string tagName =
            match.Groups[1].Value.Trim();

        switch (tagName.ToLowerInvariant())
        {
            case "modalprevious":
            case "previous":
            case "back":
                return GetCombinedNames(
                    inputController,
                    ScenarioInputAction.Left);

            case "modalnext":
            case "next":
                return GetCombinedNames(
                    inputController,
                    ScenarioInputAction.Advance,
                    ScenarioInputAction.Right);

            case "confirm":
            case "advance":
                return GetCombinedNames(
                    inputController,
                    ScenarioInputAction.Advance);

            case "close":
            case "cancel":
                return GetCombinedNames(
                    inputController,
                    ScenarioInputAction.Cancel);
        }

        if (Enum.TryParse(
                tagName,
                true,
                out ScenarioInputAction action))
        {
            return GetCombinedNames(
                inputController,
                action);
        }

        Debug.LogWarning(
            $"[unisanchan] 未対応の入力タグです: {match.Value}");

        // 誤記を発見しやすいよう、
        // 不明なタグは消さずにそのまま表示する。
        return match.Value;
    }

    private static string GetCombinedNames(
        ScenarioInputController inputController,
        params ScenarioInputAction[] actions)
    {
        List<string> names =
            new List<string>();

        foreach (ScenarioInputAction action in actions)
        {
            string displayName =
                inputController.GetBindingDisplayName(
                    action);

            if (string.IsNullOrEmpty(displayName))
                continue;

            if (!names.Contains(displayName))
            {
                names.Add(displayName);
            }
        }

        if (names.Count == 0)
            return "未設定";

        return string.Join(" / ", names);
    }
}