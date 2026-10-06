using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScenarioGuideUI : MonoBehaviour
{
    [Header("Guide UI")]
    [SerializeField] private GameObject background;
    [SerializeField] private TMP_Text guideText;
    [SerializeField] private Image guideImage;

    private void Awake()
    {
        if (guideImage != null)
            guideImage.gameObject.SetActive(false);

        gameObject.SetActive(false);
    }

    public void ShowText(string text)
    {
        if (guideText != null)
        {
            guideText.text = text;
            guideText.gameObject.SetActive(true);
        }

        if (guideImage != null)
            guideImage.gameObject.SetActive(false);

        if (background != null)
            background.SetActive(true);

        gameObject.SetActive(true);
    }

    public void ShowImage(Sprite sprite)
    {
        if (guideImage != null)
        {
            guideImage.sprite = sprite;
            guideImage.gameObject.SetActive(sprite != null);
        }

        if (guideText != null)
            guideText.gameObject.SetActive(false);

        if (background != null)
            background.SetActive(true);

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}