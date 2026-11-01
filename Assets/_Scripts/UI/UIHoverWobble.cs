using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

// Покачивание при наведении. Вешается на объект, который ловит курсор (его хитбокс не двигается),
// а качает отдельный дочерний visual. Так край ноды не уезжает из-под мыши и анимация не зацикливается.
public class UIHoverWobble : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Что качать. Не должен ловить лучи (raycastTarget у его графики выключен)")]
    [SerializeField] private RectTransform visual;
    [SerializeField] private float angle = 7f;
    [SerializeField] private float duration = 0.45f;
    [SerializeField] private int vibrato = 7;
    [Range(0f, 1f)]
    [SerializeField] private float elasticity = 0.6f;
    [SerializeField] private float hoverScale = 1.06f;
    [SerializeField] private float scaleTime = 0.15f;

    private Tween wobble;
    private Tween scale;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (visual == null)
            return;

        wobble?.Kill();
        visual.localRotation = Quaternion.identity;
        wobble = visual.DOPunchRotation(new Vector3(0f, 0f, angle), duration, vibrato, elasticity)
            .SetUpdate(true)
            .SetLink(gameObject);

        ScaleTo(hoverScale, Ease.OutBack);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (visual == null)
            return;

        ScaleTo(1f, Ease.OutQuad);
    }

    private void ScaleTo(float k, Ease ease)
    {
        scale?.Kill();
        scale = visual.DOScale(k, scaleTime).SetEase(ease).SetUpdate(true).SetLink(gameObject);
    }

    private void OnDisable()
    {
        wobble?.Kill();
        scale?.Kill();
        if (visual != null)
        {
            visual.localRotation = Quaternion.identity;
            visual.localScale = Vector3.one;
        }
    }
}
