using System;
using System.Collections;
using UnityEngine;

public class ScenarioUICommandHandler
{
    private readonly ScenarioTipsUI tipsUI;

    private readonly Func<string, string> getSpeakerDisplayName;
    private readonly Action<string> setSpeakerName;
    private readonly Action<string> applyText;
    private readonly Action<bool> setCanAdvance;
    private readonly Func<IEnumerator, Coroutine> startCoroutine;


    public ScenarioUICommandHandler(
        ScenarioTipsUI tipsUI,
        Func<string, string> getSpeakerDisplayName,
        Action<string> setSpeakerName,
        Action<string> applyText,
        Action<bool> setCanAdvance,
        Func<IEnumerator, Coroutine> startCoroutine)
    {
        this.tipsUI = tipsUI;
        this.getSpeakerDisplayName = getSpeakerDisplayName;
        this.setSpeakerName = setSpeakerName;
        this.applyText = applyText;
        this.setCanAdvance = setCanAdvance;
        this.startCoroutine = startCoroutine;
    }

    public bool TryExecute(ScenarioRow row)
    {
        if (row == null)
            return false;

        switch (Normalize(row.Command))
        {
            case "Dialog":
                ExecuteDialog(row);
                return true;

            case "Tips":
                ExecuteTips(row);
                return true;

            default:
                return false;
        }
    }

    private void ExecuteDialog(ScenarioRow row)
    {
        float inputLockSeconds =
            ParseFloat(row.Arg1, 2f);

        if (startCoroutine == null)
        {
            Debug.LogWarning(
                "[UnisanChi] Dialogを実行できません。" +
                "Coroutineの実行元が設定されていません。");

            return;
        }

        startCoroutine.Invoke(
            DialogRoutine(
                row.Speaker,
                row.Text,
                inputLockSeconds));
    }

    private void ExecuteTips(ScenarioRow row)
    {
        if (tipsUI == null)
            return;

        string mode =
            Normalize(row.Arg1).ToLowerInvariant();

        if (mode == "hide")
        {
            tipsUI.Hide();
            return;
        }

        tipsUI.ShowText(
            row.Text,
            row.Arg2
        );
    }
    
    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? ""
            : value.Trim();
    }

    private static float ParseFloat(string value, float defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        if (float.TryParse(
            value.Trim(),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out float result))
        {
            return result;
        }

        return defaultValue;
    }

    private IEnumerator DialogRoutine(
        string speaker,
        string text,
        float inputLockSeconds)
    {
        setSpeakerName?.Invoke(
            getSpeakerDisplayName?.Invoke(speaker));

        applyText?.Invoke(text);

        setCanAdvance?.Invoke(false);

        if (inputLockSeconds > 0f)
            yield return new WaitForSeconds(inputLockSeconds);

        setCanAdvance?.Invoke(true);
    }
}