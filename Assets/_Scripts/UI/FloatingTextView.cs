using DG.Tweening;
using TMPro;
using UnityEngine;

// Всплывающие «+5» / «жалоба -5» над посылкой. Живёт в мире ленты, поэтому их видно в мониторе.
public class FloatingTextView : MonoBehaviour
{
    [SerializeField] private TextMeshPro prefab;
    [SerializeField] private float startHeight = 1.2f;
    [SerializeField] private float rise = 0.8f;
    [SerializeField] private float duration = 0.8f;

    [Header("Цвета")]
    [SerializeField] private Color coinColor = new(1f, 0.8f, 0.2f);
    [SerializeField] private Color ectoColor = new(0.6f, 1f, 0.9f);
    [SerializeField] private Color complaintColor = new(1f, 0.3f, 0.3f);
    [SerializeField] private Color lostColor = new(0.7f, 0.7f, 0.7f);

    private void Start()
    {
        G.shift.OnParcelResolved += HandleOutcome;
    }

    private void OnDestroy()
    {
        if (G.shift != null)
            G.shift.OnParcelResolved -= HandleOutcome;
    }

    private void HandleOutcome(ParcelOutcome o)
    {
        Quaternion facing = o.belt != null ? o.belt.Cam.transform.rotation : Quaternion.identity;

        if (o.ecto > 0)
            Spawn(o.position, facing, $"+{o.ecto} экто", ectoColor);
        else if (o.penalty > 0)
            Spawn(o.position, facing, $"жалоба -{o.penalty}", complaintColor);
        else if (o.coins > 0)
            Spawn(o.position, facing, $"+{o.coins}", coinColor);
        else if (!o.correct)
            Spawn(o.position, facing, "зря", lostColor);
    }

    private void Spawn(Vector3 position, Quaternion facing, string text, Color color)
    {
        TextMeshPro t = Instantiate(prefab, position, facing, transform);
        Vector3 up = t.transform.up;
        t.transform.position += up * startHeight;
        t.text = text;
        t.color = color;

        t.transform.DOMove(t.transform.position + up * rise, duration).SetEase(Ease.OutCubic).SetLink(t.gameObject);
        DOTween.To(() => t.alpha, a => t.alpha = a, 0f, duration).SetEase(Ease.InQuad).SetLink(t.gameObject)
            .OnComplete(() => Destroy(t.gameObject));
    }
}
