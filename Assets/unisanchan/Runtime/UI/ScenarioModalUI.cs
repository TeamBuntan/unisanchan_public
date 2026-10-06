using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScenarioModalUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject backdrop;
    [SerializeField] private GameObject panel;

    [Header("Content")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image mainImage;
    [SerializeField] private TMP_Text captionText;
    [SerializeField] private TMP_Text bodyText;

    [Header("Button")]
    [SerializeField] private GameObject closeButton;
    [SerializeField] private TMP_Text closeButtonText;

    [Header("Input Hint")]
    [SerializeField] private TMP_Text inputHintText;

    public bool IsVisible
        => gameObject.activeSelf;

    private void Awake()
    {
        Hide();
    }

    public void Show(
        Sprite sprite,
        string title,
        string caption,
        string body,
        string buttonText)
    {
        SetText(
            titleText,
            title);

        SetImage(sprite);

        SetText(
            captionText,
            caption);

        SetText(
            bodyText,
            body);

        SetButton(buttonText);

        if (backdrop != null)
            backdrop.SetActive(true);

        if (panel != null)
            panel.SetActive(true);

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SetInputHint(string text)
    {
        SetText(
            inputHintText,
            text);
    }

    private void SetImage(Sprite sprite)
    {
        if (mainImage == null)
            return;

        mainImage.sprite = sprite;
        mainImage.preserveAspect = true;
        mainImage.gameObject.SetActive(sprite != null);
    }

    private void SetButton(string buttonText)
    {
        bool hasButton =
            !string.IsNullOrWhiteSpace(buttonText);

        if (closeButton != null)
            closeButton.SetActive(hasButton);

        if (closeButtonText != null)
        {
            closeButtonText.text =
                hasButton
                    ? buttonText.Trim()
                    : "";

            closeButtonText.gameObject.SetActive(
                hasButton);
        }
    }

    private static void SetText(
        TMP_Text target,
        string value)
    {
        if (target == null)
            return;

        bool hasText =
            !string.IsNullOrWhiteSpace(value);

        target.text =
            hasText
                ? value.Trim()
                : "";

        target.gameObject.SetActive(hasText);
    }
}