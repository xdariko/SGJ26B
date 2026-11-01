using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Строка-ссылка: иконка + ник. Клик по любой части строки открывает url,
// при наведении иконка и текст плавно перекрашиваются.
// Ставится на корень строки вместе с Button (Transition = None).
[RequireComponent(typeof(Button))]
public class LinkEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private string url = "https://";
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text label;

    [Header("Цвета")]
    [SerializeField] private Color normalColor = new Color32(0xEE, 0xE9, 0xF2, 0xFF);
    [SerializeField] private Color hoverColor = new Color32(0xD1, 0xAE, 0x6A, 0xFF);
    [SerializeField] private float fadeTime = 0.15f;

    private Button button;
    private Tween tween;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(Open);
        SetColor(normalColor);
    }

    private void OnDisable()
    {
        tween?.Kill();
        SetColor(normalColor);
    }

    private void OnDestroy()
    {
        tween?.Kill();
        button.onClick.RemoveListener(Open);
    }

    public void OnPointerEnter(PointerEventData eventData) => FadeTo(hoverColor);
    public void OnPointerExit(PointerEventData eventData) => FadeTo(normalColor);

    private void Open()
    {
        if (!string.IsNullOrEmpty(url))
            Application.OpenURL(url);
    }

    private void FadeTo(Color target)
    {
        tween?.Kill();
        Color from = label != null ? label.color : icon.color;
        tween = DOTween.To(() => from, c => { from = c; SetColor(c); }, target, fadeTime)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    private void SetColor(Color c)
    {
        if (icon != null) icon.color = c;
        if (label != null) label.color = c;
    }
}
