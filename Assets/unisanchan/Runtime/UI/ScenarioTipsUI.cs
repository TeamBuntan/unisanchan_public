using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScenarioTipsUI : MonoBehaviour
{
    private enum TipsScope
    {
        Manual,
        Choice
    }

    [Header("Tips UI")]
    [SerializeField] private GameObject background;
    [SerializeField] private TMP_Text tipsText;
    [SerializeField] private Image tipsImage;

    private TipsScope currentScope = TipsScope.Manual;

    private void Awake()
    {
        if (tipsImage != null)
            tipsImage.gameObject.SetActive(false);

        gameObject.SetActive(false);
    }

    public void ShowText(string text, string scope = "")
    {
        currentScope = ParseScope(scope);

        if (tipsText != null)
        {
            tipsText.text = text;
            tipsText.gameObject.SetActive(true);
        }

        if (background != null)
            background.SetActive(true);

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        currentScope = TipsScope.Manual;
        gameObject.SetActive(false);
    }

    public void HideIfChoiceScope()
    {
        if (currentScope == TipsScope.Choice)
            Hide();
    }

    private TipsScope ParseScope(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return TipsScope.Manual;

        switch (value.Trim().ToLowerInvariant())
        {
            case "choice":
                return TipsScope.Choice;

            default:
                // 不正値は空欄と同じ＝Manual扱い
                return TipsScope.Manual;
        }
    }
}