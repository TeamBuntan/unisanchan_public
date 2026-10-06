using System.Collections.Generic;
using UnityEngine;
using unisanchan;

public class ScenarioChoiceHandler
{
    private readonly ScenarioChoiceUI choiceUI;

    private readonly List<ScenarioChoiceOption> currentChoices =
        new List<ScenarioChoiceOption>();

    private int selectedChoiceIndex = 0;

    private readonly GameObject dialogWindow;
    private readonly GameObject speakerNameWindow;
    private readonly ScenarioVariableStore variableStore;

    public ScenarioChoiceHandler(
        ScenarioChoiceUI choiceUI,
        GameObject dialogWindow,
        GameObject speakerNameWindow,
        ScenarioVariableStore variableStore)
    {
        this.choiceUI = choiceUI;
        this.dialogWindow = dialogWindow;
        this.speakerNameWindow = speakerNameWindow;
        this.variableStore = variableStore;
    }

    public void SetChoices(
        IEnumerable<ScenarioChoiceOption> choices)
    {
        currentChoices.Clear();
        currentChoices.AddRange(choices);

        selectedChoiceIndex = 0;
    }

    public List<string> GetChoiceTexts()
    {
        List<string> choiceTexts =
            new List<string>();

        foreach (ScenarioChoiceOption choice in currentChoices)
        {
            choiceTexts.Add(choice.Text);
        }

        return choiceTexts;
    }

    public int SelectedChoiceIndex
    {
        get
        {
            return selectedChoiceIndex;
        }
    }

    public void MoveSelection(int direction)
    {
        if (currentChoices.Count == 0)
            return;

        selectedChoiceIndex += direction;

        if (selectedChoiceIndex < 0)
        {
            selectedChoiceIndex =
                currentChoices.Count - 1;
        }
        else if (selectedChoiceIndex >= currentChoices.Count)
        {
            selectedChoiceIndex = 0;
        }

        RefreshChoices();
    }

    public string GetSelectedTargetLabel()
    {
        if (currentChoices.Count == 0)
            return "";

        if (selectedChoiceIndex < 0 ||
            selectedChoiceIndex >= currentChoices.Count)
        {
            return "";
        }

        return currentChoices[selectedChoiceIndex].TargetLabel;
    }

    public void ClearChoices()
    {
        currentChoices.Clear();
        selectedChoiceIndex = 0;
    }

    public void ShowChoices()
    {
        choiceUI.ShowChoices(
            GetChoiceTexts(),
            SelectedChoiceIndex
        );
    }

    public void RefreshChoices()
    {
        choiceUI.RefreshChoices(
            GetChoiceTexts(),
            SelectedChoiceIndex
        );
    }

    public void HideChoices()
    {
        choiceUI.HideChoices();
    }

    public void HideChoiceScopeTips(
    ScenarioTipsUI tipsUI)
    {
        if (tipsUI != null)
        {
            tipsUI.HideIfChoiceScope();
        }
    }

    public void CompleteChoice(
    ScenarioTipsUI tipsUI)
    {
        HideChoices();
        HideChoiceScopeTips(tipsUI);
        ClearChoices();
    }

    public bool TryGetSelectedTargetLabel(
        out string targetLabel)
    {
        targetLabel = GetSelectedTargetLabel();

        return !string.IsNullOrWhiteSpace(targetLabel);
    }

    public void PrepareChoiceView()
    {
        if (dialogWindow != null)
            dialogWindow.SetActive(false);

        if (speakerNameWindow != null)
            speakerNameWindow.SetActive(false);
    }

    public List<ScenarioChoiceOption> CollectChoiceOptions(
        IEnumerable<ScenarioRow> rows,
        string eventId,
        string blockId)
    {
        List<ScenarioChoiceOption> choices =
            new List<ScenarioChoiceOption>();

        foreach (ScenarioRow row in rows)
        {
            if (Normalize(row.EventID) != Normalize(eventId))
                continue;

            if (Normalize(row.BlockID) != Normalize(blockId))
                continue;

            if (Normalize(row.Command) != "Choice")
                continue;

            if (!EvaluateCondition(row.Arg2))
                continue;
            
            choices.Add(
                new ScenarioChoiceOption(
                    row.Text,
                    row.Arg1,
                    row.Arg2
                )
            );
        }

        return choices;
    }

    private bool EvaluateCondition(string condition)
    {
        if (string.IsNullOrWhiteSpace(condition))
            return true;

        if (variableStore == null)
            return false;

        if (!unisanchan.ExpressionEvaluator.TryEvaluateCondition(
            condition,
            variableStore.ResolveNumber,
            variableStore.ResolveBool,
            out bool result))
        {
            Debug.LogWarning(
                $"[unisanchan] Choice条件式を評価できませんでした: {condition}");

            return false;
        }

        return result;
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? ""
            : value.Trim();
    }

    public void BeginChoice(
        IEnumerable<ScenarioRow> rows,
        ScenarioRow row)
    {
        PrepareChoiceView();

        SetChoices(
            CollectChoiceOptions(
                rows,
                row.EventID,
                row.BlockID
            )
        );

        ShowChoices();
    }
}