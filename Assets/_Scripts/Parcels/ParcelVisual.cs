using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

// Внешний вид посылки. Слои-дети лежат на разных Layer, их показывает или прячет камера (CamVision).
// Спрайты стоят лицом к камере ленты (billboard), пивот у спрайтов посылок — снизу по центру.
// Чтобы поменять арт: подменить спрайты в ParcelDefinition / GameConfig или прямо PNG-файлы.
public class ParcelVisual : MonoBehaviour
{
    [Header("Слои")]
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private SpriteRenderer stamp;
    [SerializeField] private SpriteRenderer aura;
    [FormerlySerializedAs("ghost")]
    [SerializeField] private SpriteRenderer ghostAura;
    [FormerlySerializedAs("mimic")]
    [SerializeField] private SpriteRenderer mimicAura;

    [Header("Аура мимика: красная, бьётся как сердце")]
    [SerializeField] private Color mimicColor = new(0.86f, 0.18f, 0.16f, 0.9f);
    [Tooltip("Размер ауры относительно большей стороны посылки")]
    [SerializeField] private float mimicSize = 1.7f;
    [SerializeField] private float mimicBeatScale = 1.15f;
    [Tooltip("Пауза между двойными ударами")]
    [SerializeField] private float mimicBeatPause = 0.55f;

    [Header("Аура призрака: бледная, дышит и дрожит")]
    [SerializeField] private Color ghostColor = new(0.62f, 0.95f, 0.9f, 0.75f);
    [SerializeField] private float ghostSize = 1.9f;
    [SerializeField] private float ghostBreathTime = 1.4f;
    [Tooltip("Сила дрожи в мировых единицах")]
    [SerializeField] private float ghostJitter = 0.035f;
    [SerializeField] private float ghostJitterSpeed = 18f;

    [Header("Снятие с ленты")]
    [Tooltip("Куда отлетает снятая посылка, по полу относительно ленты (y- = к камере)")]
    [SerializeField] private Vector2 removeOffset = new(0.6f, -2.2f);
    [SerializeField] private float removeHop = 0.9f;
    [SerializeField] private float removeDuration = 0.4f;

    [Header("Прочее")]
    [SerializeField] private float deliverDuration = 0.2f;
    [SerializeField] private float ghostFlyHeight = 2f;
    [SerializeField] private float ghostFlyDuration = 0.6f;

    private Vector3 auraCenter;
    private Vector3 mimicBaseScale;
    private Vector3 ghostBaseScale;
    private SpriteRenderer[] renderers;
    private readonly List<Tween> loops = new();

    public Bounds ClickBounds => body.bounds;

    private void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    public void Show(ParcelSpec spec, GameConfig cfg, Quaternion facing)
    {
        KillLoops();
        transform.rotation = facing;
        transform.localScale = Vector3.one * (spec.shape != null ? spec.shape.scale : 1f) * cfg.parcelScale;
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

        auraCenter = b.center;
        float side = Mathf.Max(b.size.x, b.size.y);

        mimicAura.enabled = spec.kind == ParcelKind.Mimic;
        mimicBaseScale = FitAura(mimicAura, side * mimicSize);
        if (mimicAura.enabled)
            StartMimicPulse();

        ghostAura.enabled = spec.hasGhost;
        ghostBaseScale = FitAura(ghostAura, side * ghostSize);
        if (ghostAura.enabled)
            StartGhostShiver();
    }

    // Подгоняет ауру под размер посылки, возвращает базовый масштаб
    private Vector3 FitAura(SpriteRenderer sr, float diameter)
    {
        Transform t = sr.transform;
        t.localPosition = auraCenter;
        float spriteSize = sr.sprite != null ? Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y) : 1f;
        t.localScale = Vector3.one * (diameter / spriteSize);
        return t.localScale;
    }

    // Двойной удар «тук-тук», пауза. Яркость вспыхивает вместе с ударом
    private void StartMimicPulse()
    {
        Transform t = mimicAura.transform;
        Color dim = mimicColor;
        dim.a *= 0.45f;
        mimicAura.color = dim;

        Vector3 big = mimicBaseScale * mimicBeatScale;
        Vector3 mid = mimicBaseScale * Mathf.Lerp(1f, mimicBeatScale, 0.6f);

        Sequence s = DOTween.Sequence()
            .Append(t.DOScale(big, 0.09f).SetEase(Ease.OutQuad))
            .Join(mimicAura.DOColor(mimicColor, 0.09f))
            .Append(t.DOScale(mimicBaseScale, 0.12f).SetEase(Ease.InQuad))
            .Join(mimicAura.DOColor(dim, 0.12f))
            .Append(t.DOScale(mid, 0.08f).SetEase(Ease.OutQuad))
            .Join(mimicAura.DOColor(mimicColor, 0.08f))
            .Append(t.DOScale(mimicBaseScale, 0.25f).SetEase(Ease.InOutSine))
            .Join(mimicAura.DOColor(dim, 0.25f))
            .AppendInterval(mimicBeatPause)
            .SetLoops(-1)
            .SetLink(gameObject);
        // Разные посылки не бьются в унисон
        s.Goto(UnityEngine.Random.value * s.Duration(false), true);
        loops.Add(s);
    }

    // Медленное дыхание + мелкая беспокойная дрожь ауры (сама посылка не дрожит,
    // иначе призрака было бы видно и без камеры 3-го уровня)
    private void StartGhostShiver()
    {
        Transform t = ghostAura.transform;
        Color dim = ghostColor;
        dim.a *= 0.4f;
        ghostAura.color = dim;

        Tween breath = DOTween.Sequence()
            .Append(ghostAura.DOColor(ghostColor, ghostBreathTime * 0.5f).SetEase(Ease.InOutSine))
            .Join(t.DOScale(ghostBaseScale * 1.06f, ghostBreathTime * 0.5f).SetEase(Ease.InOutSine))
            .Append(ghostAura.DOColor(dim, ghostBreathTime * 0.5f).SetEase(Ease.InOutSine))
            .Join(t.DOScale(ghostBaseScale, ghostBreathTime * 0.5f).SetEase(Ease.InOutSine))
            .SetLoops(-1)
            .SetLink(gameObject);
        breath.Goto(UnityEngine.Random.value * ghostBreathTime, true);
        loops.Add(breath);

        float seed = UnityEngine.Random.value * 100f;
        float time = 0f;
        Tween shiver = DOTween.To(() => 0f, _ => { }, 1f, 1f)
            .SetLoops(-1)
            .SetLink(gameObject)
            .OnUpdate(() =>
            {
                time += Time.deltaTime * ghostJitterSpeed;
                float x = Mathf.PerlinNoise(seed, time) * 2f - 1f;
                float y = Mathf.PerlinNoise(seed + 31.7f, time) * 2f - 1f;
                t.localPosition = auraCenter + new Vector3(x, y, 0f) * ghostJitter / Mathf.Max(0.0001f, transform.lossyScale.x);
            });
        loops.Add(shiver);
    }

    private void KillLoops()
    {
        foreach (Tween tw in loops)
            tw?.Kill();
        loops.Clear();
    }

    private void OnDestroy()
    {
        KillLoops();
    }

    public void PlayRemove(Action onComplete)
    {
        KillLoops();
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
        KillLoops();
        transform.DOScale(0f, deliverDuration)
            .SetEase(Ease.InBack)
            .SetLink(gameObject)
            .OnComplete(() => onComplete?.Invoke());
    }

    // Призрак вылетает: аура отрывается вверх, раздувается и тает
    public void PlayGhostRelease()
    {
        KillLoops();
        // пульс мимика (если посылка ещё и мимик) перезапускаем
        if (mimicAura.enabled)
        {
            mimicAura.transform.localScale = mimicBaseScale;
            StartMimicPulse();
        }

        Transform g = ghostAura.transform;
        g.localPosition = auraCenter;
        g.DOLocalMoveY(auraCenter.y + ghostFlyHeight, ghostFlyDuration).SetEase(Ease.OutQuad).SetLink(gameObject);
        g.DOScale(ghostBaseScale * 1.6f, ghostFlyDuration).SetEase(Ease.OutQuad).SetLink(gameObject);
        ghostAura.DOFade(0f, ghostFlyDuration).SetEase(Ease.InQuad).SetLink(gameObject)
            .OnComplete(() => ghostAura.enabled = false);
    }
}
