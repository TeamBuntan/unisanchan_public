using System;
using System.Collections.Generic;
using UnityEngine;

public class ModalCommandHandler
{
    private readonly ScenarioModalUI modalUI;
    private readonly ImageRegistry imageRegistry;
    private readonly List<ModalPresetRow> presets;
    private readonly Func<string, string> formatText;

    private readonly List<ModalPresetRow> currentPages =
        new List<ModalPresetRow>();

    private int currentPageIndex;

    public bool IsVisible =>
        modalUI != null &&
        modalUI.IsVisible;

    public ModalCommandHandler(
        ScenarioModalUI modalUI,
        ImageRegistry imageRegistry,
        List<ModalPresetRow> presets,
        Func<string, string> formatText = null)
    {
        this.modalUI = modalUI;
        this.imageRegistry = imageRegistry;

        this.presets =
            presets ?? new List<ModalPresetRow>();

        this.formatText =
            formatText ?? (text => text ?? "");
    }

    public bool TryExecute(ScenarioRow row)
    {
        if (row == null)
            return false;

        if (!string.Equals(
                row.Command?.Trim(),
                "Modal",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string operation =
            row.Arg1?.Trim() ?? "";

        if (string.Equals(
                operation,
                "Show",
                StringComparison.OrdinalIgnoreCase))
        {
            Show(row.Arg2);
            return true;
        }

        if (string.Equals(
                operation,
                "Hide",
                StringComparison.OrdinalIgnoreCase))
        {
            Hide();
            return true;
        }

        Debug.LogError(
            $"[unisanchan] Modalの操作「{operation}」は未対応です。"
            + " Show または Hide を指定してください。");

        return true;
    }

    public void Show(string presetId)
    {
        string normalizedId =
            presetId?.Trim() ?? "";

        if (string.IsNullOrEmpty(normalizedId))
        {
            Debug.LogError(
                "[unisanchan] Modal / Show のPresetIDが空欄です。");

            return;
        }

        currentPages.Clear();

        foreach (ModalPresetRow preset in presets)
        {
            if (preset == null)
                continue;

            if (string.Equals(
                    preset.PresetID?.Trim(),
                    normalizedId,
                    StringComparison.OrdinalIgnoreCase))
            {
                currentPages.Add(preset);
            }
        }

        if (currentPages.Count == 0)
        {
            Debug.LogError(
                $"[unisanchan] ModalPreset「{normalizedId}」が見つかりません。");

            return;
        }

        currentPages.Sort(
            (left, right) =>
            {
                int.TryParse(
                    left.Page,
                    out int leftPage);

                int.TryParse(
                    right.Page,
                    out int rightPage);

                return leftPage.CompareTo(
                    rightPage);
            });

        currentPageIndex = 0;
        ShowCurrentPage();
    }

    public void NextPage()
    {
        if (!IsVisible ||
            currentPages.Count == 0)
        {
            return;
        }

        if (currentPageIndex >=
            currentPages.Count - 1)
        {
            return;
        }

        currentPageIndex++;
        ShowCurrentPage();
    }

    public void PreviousPage()
    {
        if (!IsVisible ||
            currentPages.Count == 0)
        {
            return;
        }

        if (currentPageIndex <= 0)
            return;

        currentPageIndex--;
        ShowCurrentPage();
    }

    public void Hide()
    {
        currentPages.Clear();
        currentPageIndex = 0;

        modalUI?.Hide();
    }

    private void ShowCurrentPage()
    {
        if (modalUI == null)
        {
            Debug.LogError(
                "[unisanchan] ScenarioModalUIが設定されていません。");

            return;
        }

        ModalPresetRow page =
            currentPages[currentPageIndex];

        Sprite sprite = null;

        if (!string.IsNullOrWhiteSpace(
                page.ImageID))
        {
            if (imageRegistry == null)
            {
                Debug.LogError(
                    "[unisanchan] ImageRegistryが設定されていません。");

                return;
            }

            sprite =
                imageRegistry.FindSprite(
                    page.ImageID);

            if (sprite == null)
            {
                Debug.LogError(
                    $"[unisanchan] ImageID「{page.ImageID}」が見つかりません。");

                return;
            }
        }

        // β1.0はキーボード・ゲームパッド操作のため、
        // タッチ用CloseButtonは表示しない。
        modalUI.Show(
            sprite,
            FormatText(page.Title),
            FormatText(page.Caption),
            FormatText(page.Body),
            "");

        modalUI.SetInputHint(
            FormatText(page.InputHint));
    }

    private string FormatText(string text)
    {
        return formatText?.Invoke(text) ??
            text ??
            "";
    }
}