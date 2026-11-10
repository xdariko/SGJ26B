using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

// Внешний вид посылки. Слои-дети лежат на разных Layer, их показывает или прячет камера (CamVision).
// Спрайты стоят лицом к камере ленты (billboard), пивот у спрайтов посылок — снизу по центру.
// Ауры проклятия, мимика и призрака мигают: вспыхивают на onTime, гаснут на offTime, яркость на пике = strength
// (у проклятия, мимика и призрака свои настройки в GameStats, качаются в книге). Твины аур крутят только «свой» цвет, итог считается в LateUpdate.
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
    [Tooltip("Яркость ауры между ударами (доля от пика)")]
    [Range(0f, 1f)] [SerializeField] private float mimicDimAlpha = 0.6f;

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

    [Header("Мигание аур")]
    [Tooltip("Яркость свечения, пока идёт обучение: посылку должно быть хорошо видно")]
    [Range(0f, 1f)] [SerializeField] private float tutorialGlow = 0.9f;

    private Color curseColor;
    private Color mimicTint;
    private Color ghostTint;
    // false после снятия/доставки: дальше альфой управляют анимации исчезновения
    private bool glowLive;
    private bool ghostFlying;
    private float glowPhase;

    private Vector3 auraCenter;
    private Vector3 mimicBaseScale;
    private Vector3 ghostBaseScale;
    private SpriteRenderer[] renderers;
    private readonly List<Tween> loops = new();

    public Bounds ClickBounds => body.bounds;

    // Посылка вместе с включёнными аурами (для подсветки в обучении)
    public Bounds GlowBounds
    {
        get
        {
            Bounds b = body.bounds;
            if (aura.enabled) b.Encapsulate(aura.bounds);
            if (mimicAura.enabled) b.Encapsulate(mimicAura.bounds);
            if (ghostAura.enabled) b.Encapsulate(ghostAura.bounds);
            return b;
        }
    }

    private void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        curseColor = aura.color;
    }

    public void Show(ParcelSpec spec, GameConfig cfg, Quaternion facing)
    {
        KillLoops();
        glowLive = true;
        ghostFlying = false;
        // Разные посылки мигают вразнобой
        glowPhase = UnityEngine.Random.value * 10f;
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
        dim.a *= mimicDimAlpha;
        mimicTint = dim;

        Vector3 big = mimicBaseScale * mimicBeatScale;
        Vector3 mid = mimicBaseScale * Mathf.Lerp(1f, mimicBeatScale, 0.6f);

        Sequence s = DOTween.Sequence()
            .Append(t.DOScale(big, 0.09f).SetEase(Ease.OutQuad))
            .Join(MimicTo(mimicColor, 0.09f))
            .Append(t.DOScale(mimicBaseScale, 0.12f).SetEase(Ease.InQuad))
            .Join(MimicTo(dim, 0.12f))
            .Append(t.DOScale(mid, 0.08f).SetEase(Ease.OutQuad))
            .Join(MimicTo(mimicColor, 0.08f))
            .Append(t.DOScale(mimicBaseScale, 0.25f).SetEase(Ease.InOutSine))
            .Join(MimicTo(dim, 0.25f))
            .AppendInterval(mimicBeatPause)
            .SetLoops(-1)
            .SetLink(gameObject);
        // Разные посылки не бьются в унисон
        s.Goto(UnityEngine.Random.value * s.Duration(false), true);
        loops.Add(s);
    }

    private Tween MimicTo(Color c, float d) => DOTween.To(() => mimicTint, v => mimicTint = v, c, d);
    private Tween GhostTo(Color c, float d) => DOTween.To(() => ghostTint, v => ghostTint = v, c, d);

    // Медленное дыхание + мелкая беспокойная дрожь ауры (сама посылка не дрожит:
    // призрака выдаёт только мигающая аура)
    private void StartGhostShiver()
    {
        Transform t = ghostAura.transform;
        Color dim = ghostColor;
        dim.a *= 0.4f;
        ghostTint = dim;

        Tween breath = DOTween.Sequence()
            .Append(GhostTo(ghostColor, ghostBreathTime * 0.5f).SetEase(Ease.InOutSine))
            .Join(t.DOScale(ghostBaseScale * 1.06f, ghostBreathTime * 0.5f).SetEase(Ease.InOutSine))
            .Append(GhostTo(dim, ghostBreathTime * 0.5f).SetEase(Ease.InOutSine))
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

    private void LateUpdate()
    {
        if (!glowLive)
            return;

        GameStats s = G.stats;
        if (aura.enabled)
            aura.color = Fade(curseColor, GlowFactor(G.familiars != null && G.familiars.CurseSight ? s?.curseGlowSight : s?.curseGlow));
        if (mimicAura.enabled)
            mimicAura.color = Fade(mimicTint, GlowFactor(s?.mimicGlow));
        if (ghostAura.enabled && !ghostFlying)
            ghostAura.color = Fade(ghostTint, GlowFactor(s?.ghostGlow));
    }

    private static Color Fade(Color c, float k)
    {
        c.a *= k;
        return c;
    }

    // 0..strength: плавная вспышка на onTime, потом темнота на offTime
    private float GlowFactor(GlowStats s)
    {
        if (s == null)
            return 1f;

        if (G.IsTutorial)
            return Mathf.Max(s.strength, tutorialGlow);

        float on = Mathf.Max(0.05f, s.onTime);
        float t = Mathf.Repeat(Time.time + glowPhase, on + s.offTime);
        if (t >= on)
            return 0f;

        float fade = Mathf.Clamp(G.config.glowFade, 0.01f, on * 0.5f);
        return Mathf.Min(1f, t / fade, (on - t) / fade) * s.strength;
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
        glowLive = false;
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
        glowLive = false;
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

        ghostFlying = true;
        // Вылет видно всегда, даже если клик пришёлся на погасшую фазу мигания
        ghostAura.color = ghostColor;
        Transform g = ghostAura.transform;
        g.localPosition = auraCenter;
        g.DOLocalMoveY(auraCenter.y + ghostFlyHeight, ghostFlyDuration).SetEase(Ease.OutQuad).SetLink(gameObject);
        g.DOScale(ghostBaseScale * 1.6f, ghostFlyDuration).SetEase(Ease.OutQuad).SetLink(gameObject);
        ghostAura.DOFade(0f, ghostFlyDuration).SetEase(Ease.InQuad).SetLink(gameObject)
            .OnComplete(() => ghostAura.enabled = false);
    }
}
