using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Кнопка чуть увеличивается при наведении и проседает при нажатии.
// Вешается на объект с Button (или любой Selectable). Время unscaled — работает и на паузе.
public class UIHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float pressScale = 0.95f;
    [SerializeField] private float duration = 0.15f;
    [SerializeField] private Ease ease = Ease.OutBack;

    private Selectable selectable;
    private Vector3 baseScale;
    private bool hovered;
    private Tween tween;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
        baseScale = transform.localScale;
    }

    private void OnDisable()
    {
        // Панель могли закрыть, пока курсор был на кнопке
        tween?.Kill();
        hovered = false;
        transform.localScale = baseScale;
    }

    private void OnDestroy()
    {
        tween?.Kill();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        if (CanAnimate())
            ScaleTo(hoverScale, ease);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        ScaleTo(1f, Ease.OutQuad);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (CanAnimate())
            ScaleTo(pressScale, Ease.OutQuad, duration * 0.5f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ScaleTo(hovered && CanAnimate() ? hoverScale : 1f, ease);
    }

    private bool CanAnimate()
    {
        return selectable == null || selectable.IsInteractable();
    }

    private void ScaleTo(float k, Ease e, float time = -1f)
    {
        tween?.Kill();
        tween = transform.DOScale(baseScale * k, time < 0f ? duration : time)
            .SetEase(e)
            .SetUpdate(true)
            .SetLink(gameObject);
    }
}
