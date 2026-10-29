using System;
using DG.Tweening;
using UnityEngine;

// Внешний вид посылки. Слои-дети лежат на разных Layer, их показывает или прячет камера (CamVision).
// Спрайты стоят лицом к камере ленты (billboard), пивот у спрайтов посылок — снизу по центру.
// Чтобы поменять арт: подменить спрайты в ParcelDefinition / GameConfig или прямо PNG-файлы.
public class ParcelVisual : MonoBehaviour
{
    [Header("Слои")]
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private SpriteRenderer stamp;
    [SerializeField] private SpriteRenderer aura;
    [SerializeField] private SpriteRenderer ghost;
    [SerializeField] private SpriteRenderer mimic;

    [Header("Снятие с ленты")]
    [Tooltip("Куда отлетает снятая посылка, по полу относительно ленты (y- = к камере)")]
    [SerializeField] private Vector2 removeOffset = new(0.6f, -2.2f);
    [SerializeField] private float removeHop = 0.9f;
    [SerializeField] private float removeDuration = 0.4f;

    [Header("Прочее")]
    [SerializeField] private float deliverDuration = 0.2f;
    [SerializeField] private float ghostFlyHeight = 2f;
    [SerializeField] private float ghostFlyDuration = 0.6f;

    private Vector3 ghostLocalPos;
    private SpriteRenderer[] renderers;

    public Bounds ClickBounds => body.bounds;

    private void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    public void Show(ParcelSpec spec, GameConfig cfg, Quaternion facing)
    {
        transform.rotation = facing;
        transform.localScale = Vector3.one * (spec.shape != null ? spec.shape.scale : 1f);
        body.sprite = spec.shape != null ? spec.shape.sprite : null;

        // Оверлеи встают по габаритам спрайта, так что новый арт не требует ручной подгонки
        Bounds b = body.sprite != null ? body.sprite.bounds : new Bounds(Vector3.up * 0.5f, Vector3.one);
        Vector2 stampOffset = spec.shape != null ? spec.shape.stampOffset : Vector2.zero;

        AddresseeInfo info = cfg.GetAddressee(spec.address);
        bool hasStamp = info != null && info.stamp != null && spec.kind != ParcelKind.Trash;
        stamp.enabled = hasStamp;
        if (hasStamp)
            stamp.sprite = info.stamp;
        stamp.transform.localPosition = (Vector2)b.center + stampOffset;

        aura.enabled = spec.kind == ParcelKind.Cursed;
        aura.transform.localPosition = b.center;
        mimic.enabled = spec.kind == ParcelKind.Mimic;
        mimic.transform.localPosition = b.center;

        ghostLocalPos = new Vector3(0f, b.max.y + 0.15f, 0f);
        ghost.enabled = spec.hasGhost;
        ghost.transform.localPosition = ghostLocalPos;
        ghost.color = Color.white;
    }

    public void PlayRemove(Action onComplete)
    {
        Transform root = transform.parent;
        Vector3 floorTarget = transform.localPosition + (Vector3)removeOffset;
        // «Вверх» над полом = -Z в пространстве ленты
        Vector3 up = root != null ? root.InverseTransformDirection(Vector3.back) : Vector3.back;

        Sequence seq = DOTween.Sequence().SetLink(gameObject);
        seq.Append(DOTween.To(() => 0f, t =>
        {
            float hop = 4f * t * (1f - t) * removeHop - t * 0.5f;
            transform.localPosition = Vector3.Lerp(Vector3.zero, floorTarget, t) + up * hop;
        }, 1f, removeDuration).SetEase(Ease.Linear));
        seq.Join(transform.DORotateQuaternion(transform.rotation * Quaternion.Euler(0, 0, -45f), removeDuration));
        foreach (SpriteRenderer sr in renderers)
            if (sr.enabled)
                seq.Join(sr.DOFade(0f, removeDuration).SetEase(Ease.InCubic));
        seq.OnComplete(() => onComplete?.Invoke());
    }

    public void PlayDeliver(Action onComplete)
    {
        transform.DOScale(0f, deliverDuration)
            .SetEase(Ease.InBack)
            .SetLink(gameObject)
            .OnComplete(() => onComplete?.Invoke());
    }

    public void PlayGhostRelease()
    {
        Transform g = ghost.transform;
        g.DOLocalMoveY(ghostLocalPos.y + ghostFlyHeight, ghostFlyDuration).SetEase(Ease.OutQuad).SetLink(gameObject);
        ghost.DOFade(0f, ghostFlyDuration).SetLink(gameObject).OnComplete(() => ghost.enabled = false);
    }
}
