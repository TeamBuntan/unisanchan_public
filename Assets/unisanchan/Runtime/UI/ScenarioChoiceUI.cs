using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScenarioChoiceUI : MonoBehaviour
{
    [Header("Choice UI")]
    [SerializeField] private GameObject choiceWindow;

    [Header("Optional")]
    [SerializeField] private GameObject dimPanel;

    [Header("Choice Items")]
    [SerializeField] private Transform choiceList;
    [SerializeField] private GameObject choiceItemPrefab;
    [SerializeField] private RectTransform cursorImage;

    private readonly List<RectTransform> generatedItems = new();

    public void ShowChoices(
        IReadOnlyList<string> choices,
        int selectedIndex)
    {
        if (choiceWindow != null)
            choiceWindow.SetActive(true);

        // β版標準は DialogReplace。
        // DimPanel は将来 Overlay 型で使用するため、現在は表示しない。
        if (dimPanel != null)
            dimPanel.SetActive(false);

        CreateChoiceItems(choices);
        StartCoroutine(RefreshChoicesNextFrame(choices, selectedIndex));
    }

    private void CreateChoiceItems(IReadOnlyList<string> choices)
    {
        ClearChoiceItems();

        if (choiceList == null || choiceItemPrefab == null)
            return;

        for (int i = 0; i < choices.Count; i++)
        {
            GameObject item = Instantiate(choiceItemPrefab, choiceList);

            TMP_Text text = item.GetComponentInChildren<TMP_Text>();

            if (text != null)
                text.text = choices[i];

            RectTransform rect = item.GetComponent<RectTransform>();

            if (rect != null)
                generatedItems.Add(rect);
        }
    }

    public void RefreshChoices(
        IReadOnlyList<string> choices,
        int selectedIndex)
    {
        if (generatedItems.Count == 0)
            return;

        if (selectedIndex < 0 || selectedIndex >= generatedItems.Count)
            return;

        if (cursorImage == null)
            return;

        Canvas.ForceUpdateCanvases();

        if (choiceList is RectTransform choiceListRect)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(choiceListRect);
        }

        cursorImage.gameObject.SetActive(true);

        RectTransform selectedItem = generatedItems[selectedIndex];

        Vector3 cursorPosition = cursorImage.position;
        cursorPosition.y = selectedItem.position.y;
        cursorImage.position = cursorPosition;
    }

    public void HideChoices()
    {
        if (choiceWindow != null)
            choiceWindow.SetActive(false);

        if (dimPanel != null)
            dimPanel.SetActive(false);

        if (cursorImage != null)
            cursorImage.gameObject.SetActive(false);

        ClearChoiceItems();
    }

    private void ClearChoiceItems()
    {
        foreach (RectTransform item in generatedItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }

        generatedItems.Clear();
    }

    private void Awake()
    {
        if (choiceWindow != null)
            choiceWindow.SetActive(false);

        if (dimPanel != null)
            dimPanel.SetActive(false);

        if (cursorImage != null)
            cursorImage.gameObject.SetActive(false);
    }

    private System.Collections.IEnumerator RefreshChoicesNextFrame(
        IReadOnlyList<string> choices,
        int selectedIndex)
    {
        yield return null;

        RefreshChoices(choices, selectedIndex);
    }
}