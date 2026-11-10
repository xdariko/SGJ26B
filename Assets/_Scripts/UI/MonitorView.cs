using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Один монитор на стене: показывает RenderTexture камеры ленты и пересылает клик в логику.
public class MonitorView : MonoBehaviour, IPointerDownHandler
{
    [Header("Живой экран")]
    [SerializeField] private GameObject liveRoot;
    [SerializeField] private RawImage screen;
    [SerializeField] private TMP_Text label;
    [Tooltip("Марка адресата этой ленты, встаёт сразу после подписи. На общей ленте скрыта")]
    [SerializeField] private Image stampIcon;
    [SerializeField] private float stampGap = 10f;
    [SerializeField] private TMP_Text timecode;

    [Header("Закрытый слот")]
    [SerializeField] private GameObject lockedRoot;
    [SerializeField] private TMP_Text lockedText;

    [Header("Эффекты")]
    [SerializeField] private RectTransform shakeTarget;
    [SerializeField] private float shakeStrength = 12f;
    [SerializeField] private float glitchDuration = 0.4f;

    [Header("Поломка: монитор теряет сигнал, чинится ударами кулака")]
    [Tooltip("Надпись «НЕТ СИГНАЛА» поверх экрана")]
    [SerializeField] private GameObject brokenRoot;
    [SerializeField] private TMP_Text brokenText;
    [Tooltip("Кулак над монитором (не ловит клики)")]
    [SerializeField] private RectTransform fist;
    [Tooltip("Где висит кулак, пока ждёт удара (якорь вверху по центру монитора)")]
    [SerializeField] private Vector2 fistRest = new(0f, 60f);
    [Tooltip("Куда кулак опускается при ударе")]
    [SerializeField] private Vector2 fistImpact = new(0f, 10f);
    [SerializeField] private float fistPunchTime = 0.07f;
    [Tooltip("Общий размер кулака")]
    [SerializeField] private float fistSize = 0.85f;
    [Tooltip("При такой ширине монитора кулак в полном размере; мониторы меньше — кулак меньше")]
    [SerializeField] private float fistReferenceWidth = 1000f;
    [SerializeField] private float fistMinScale = 0.5f;
    [Tooltip("Сколько снега на экране сразу после поломки; с каждым ударом меньше")]
    [Range(0f, 1f)] [SerializeField] private float brokenStatic = 0.92f;
    [SerializeField] private float hitShake = 9f;

    [Header("Таймкод: 23:00 → 00:00")]
    [SerializeField] private int startHour = 23;
    [SerializeField] private int clockMinutes = 60;

    private static readonly int NoiseId = Shader.PropertyToID("_Noise");
    private static readonly int GlitchId = Shader.PropertyToID("_Glitch");
    private static readonly int DistortionId = Shader.PropertyToID("_Distortion");
    private static readonly int StaticId = Shader.PropertyToID("_Static");
    private static readonly int AberrationId = Shader.PropertyToID("_Aberration");

    private Belt belt;
    private int index;
    private Material material;
    private float baseNoise;
    private float baseAberration;
    private float glitch;
    private int faultHits;
    private Tween fistIdle;
    // Масштаб кулака под текущий размер монитора: все сдвиги кулака умножаются на него
    private float fistK = 1f;

    public Belt Belt => belt;

    private void Awake()
    {
        // Свой экземпляр материала, чтобы у каждого монитора были свои помехи
        material = new Material(screen.material);
        screen.material = material;
        baseNoise = material.GetFloat(NoiseId);
        baseAberration = material.GetFloat(AberrationId);
    }

    private void Start()
    {
        G.shift.OnParcelResolved += HandleOutcome;
        if (G.faults != null)
        {
            G.faults.OnBroken += HandleBroken;
            G.faults.OnHit += HandleHit;
            G.faults.OnRepaired += HandleRepaired;
        }
        HideFault();
    }

    private void OnDestroy()
    {
        if (G.shift != null)
            G.shift.OnParcelResolved -= HandleOutcome;
        if (G.faults != null)
        {
            G.faults.OnBroken -= HandleBroken;
            G.faults.OnHit -= HandleHit;
            G.faults.OnRepaired -= HandleRepaired;
        }
        if (belt != null)
            belt.Cam.OnOutputChanged -= HandleOutputChanged;
        Destroy(material);
    }

    public void Bind(Belt target, int slotIndex)
    {
        if (belt != null)
            belt.Cam.OnOutputChanged -= HandleOutputChanged;

        belt = target;
        index = slotIndex;
        gameObject.SetActive(true);
        liveRoot.SetActive(true);
        lockedRoot.SetActive(false);

        belt.Cam.OnOutputChanged += HandleOutputChanged;
        screen.texture = belt.Cam.Output;

        AddresseeInfo info = G.config.GetAddressee(belt.Addressee);
        string title = info != null ? info.displayName.ToUpper() : "ОБЩАЯ";
        label.text = $"CAM-{index + 1} · {title}";
        ShowStamp(info);

        material.SetFloat(NoiseId, baseNoise * G.stats.camNoise);
        // «Протереть глаза» убирает и цветную двоящуюся кайму: с ней картинка мыльная при любом разрешении
        material.SetFloat(AberrationId, baseAberration * G.stats.camNoise);
        SetGlitch(0f);
        HideFault();
    }

    // Чтобы игрок знал, какая марка «своя» для этой ленты
    private void ShowStamp(AddresseeInfo info)
    {
        if (stampIcon == null)
            return;

        bool show = info != null && info.stamp != null;
        stampIcon.gameObject.SetActive(show);
        if (!show)
            return;

        stampIcon.sprite = info.stamp;
        stampIcon.preserveAspect = true;
        // Подпись прижата влево, марку ставим сразу за последней буквой
        label.ForceMeshUpdate();
        RectTransform labelRect = label.rectTransform;
        RectTransform icon = stampIcon.rectTransform;
        float textEnd = labelRect.anchoredPosition.x + (0f - labelRect.pivot.x) * labelRect.rect.width
                        + label.margin.x + label.GetRenderedValues(true).x;
        // Середина самих букв, а не всей рамки подписи: текст прижат кверху
        float textMiddle = labelRect.anchoredPosition.y + label.textBounds.center.y;
        icon.anchorMin = labelRect.anchorMin;
        icon.anchorMax = labelRect.anchorMax;
        icon.pivot = new Vector2(0f, 0.5f);
        icon.anchoredPosition = new Vector2(textEnd + stampGap, textMiddle);
    }

    public void ShowLocked(int slotIndex, string hint)
    {
        if (stampIcon != null)
            stampIcon.gameObject.SetActive(false);

        if (belt != null)
            belt.Cam.OnOutputChanged -= HandleOutputChanged;

        belt = null;
        index = slotIndex;
        HideFault();
        gameObject.SetActive(true);
        liveRoot.SetActive(false);
        lockedRoot.SetActive(true);
        // Пустой слот = белый шум без надписей; подпись оставлена на случай, если её вернут в префаб
        if (lockedText != null)
            lockedText.text = hint;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // Где на экране монитора видна область мира (например, посылка): мировые углы UI-прямоугольника.
    // Обратная к CctvMath.Distort считается итерациями: искажение небольшое, пары шагов хватает
    public bool TryGetScreenCorners(Bounds world, out Vector3 min, out Vector3 max)
    {
        min = max = Vector3.zero;
        if (belt == null || !liveRoot.activeInHierarchy)
            return false;

        Camera cam = belt.Cam.Camera;
        float distortion = material.GetFloat(DistortionId);
        Rect rect = screen.rectTransform.rect;
        Vector2 lo = new(float.MaxValue, float.MaxValue);
        Vector2 hi = new(float.MinValue, float.MinValue);

        for (int i = 0; i < 8; i++)
        {
            Vector3 sign = new((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1);
            Vector3 vp = cam.WorldToViewportPoint(world.center + Vector3.Scale(world.extents, sign));
            if (vp.z <= 0f)
                continue;

            Vector2 target = vp;
            Vector2 uv = target;
            for (int k = 0; k < 6; k++)
                uv += target - CctvMath.Distort(uv, distortion);

            Vector2 local = Rect.NormalizedToPoint(rect, new Vector2(Mathf.Clamp01(uv.x), Mathf.Clamp01(uv.y)));
            lo = Vector2.Min(lo, local);
            hi = Vector2.Max(hi, local);
        }

        if (lo.x > hi.x)
            return false;

        min = screen.rectTransform.TransformPoint(lo);
        max = screen.rectTransform.TransformPoint(hi);
        return true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (belt == null || G.IsFrozen || eventData.button != PointerEventData.InputButton.Left)
            return;

        // Сломанный монитор: клик = удар кулаком, посылки не выбираются
        if (G.faults != null && G.faults.Hit(belt))
            return;

        RectTransform rt = screen.rectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.position, eventData.pressEventCamera, out Vector2 local))
            return;

        Vector2 uv = Rect.PointToNormalized(rt.rect, local);
        uv = CctvMath.Distort(uv, material.GetFloat(DistortionId));

        G.shift.HandleClick(ParcelPicker.PickViewport(belt, belt.Cam.Camera, uv));
    }

    private void Update()
    {
        if (belt == null || timecode == null || G.shift == null)
            return;

        float minutes = G.shift.Progress01 * clockMinutes;
        int whole = Mathf.FloorToInt(minutes);
        int hour = (startHour + whole / 60) % 24;
        int seconds = Mathf.FloorToInt((minutes - whole) * 60f);
        timecode.text = $"{hour:00}:{whole % 60:00}:{seconds:00}";
    }

    private void HandleOutputChanged(Texture texture)
    {
        screen.texture = texture;
    }

    private void HandleOutcome(ParcelOutcome o)
    {
        if (belt == null || o.belt != belt || o.penalty <= 0)
            return;

        Shake(shakeStrength);
        Glitch(1f);
    }

    // ---------- Поломка ----------

    private void HandleBroken(Belt b)
    {
        if (b != belt)
            return;

        faultHits = G.faults.HitsLeft(belt);
        material.SetFloat(StaticId, brokenStatic);
        if (brokenRoot != null)
            brokenRoot.SetActive(true);
        UpdateBrokenText(faultHits);
        Glitch(1f);
        Shake(shakeStrength);

        if (fist == null)
            return;

        // Кулак спускается сверху и покачивается в ожидании
        fistIdle?.Kill();
        fist.DOKill();
        fist.gameObject.SetActive(true);
        fistK = FistScale();
        fist.localScale = Vector3.one * fistK;
        fist.anchoredPosition = (fistRest + Vector2.up * 220f) * fistK;
        SetFistAlpha(1f);
        fist.DOAnchorPos(fistRest * fistK, 0.35f).SetEase(Ease.OutBack).SetLink(gameObject)
            .OnComplete(StartFistIdle);
    }

    private void HandleHit(Belt b, int hitsLeft)
    {
        if (b != belt)
            return;

        // С каждым ударом снег редеет
        float left = faultHits > 0 ? hitsLeft / (float)faultHits : 0f;
        material.SetFloat(StaticId, brokenStatic * Mathf.Lerp(0.45f, 1f, left));
        UpdateBrokenText(hitsLeft);
        Glitch(0.8f);

        if (fist == null)
        {
            Shake(hitShake);
            return;
        }

        fistIdle?.Kill();
        fist.DOKill(true);
        fist.localScale = Vector3.one * fistK;
        // Замах вверх, удар вниз, встряска монитора, обратно
        Sequence s = DOTween.Sequence().SetLink(gameObject).SetTarget(fist);
        s.Append(fist.DOAnchorPos((fistRest + Vector2.up * 25f) * fistK, fistPunchTime).SetEase(Ease.OutQuad));
        s.Append(fist.DOAnchorPos(fistImpact * fistK, fistPunchTime).SetEase(Ease.InQuad));
        s.AppendCallback(() => Shake(hitShake));
        s.Append(fist.DOPunchScale(new Vector3(0.12f, -0.12f, 0f) * fistK, 0.15f, 8));
        if (hitsLeft > 0)
        {
            s.Append(fist.DOAnchorPos(fistRest * fistK, 0.18f).SetEase(Ease.OutQuad));
            s.OnComplete(StartFistIdle);
        }
    }

    private void HandleRepaired(Belt b)
    {
        if (b != belt)
            return;

        material.SetFloat(StaticId, 0f);
        if (brokenRoot != null)
            brokenRoot.SetActive(false);
        Glitch(0.6f);

        if (fist == null || !fist.gameObject.activeSelf)
            return;

        // Кулак уходит наверх, дав последнему удару доиграть
        fistIdle?.Kill();
        Sequence s = DOTween.Sequence().SetLink(gameObject);
        s.AppendInterval(fistPunchTime * 2f + 0.2f);
        s.Append(fist.DOAnchorPos((fistRest + Vector2.up * 200f) * fistK, 0.35f).SetEase(Ease.InBack));
        s.Join(DOTween.To(() => 1f, SetFistAlpha, 0f, 0.35f));
        s.OnComplete(() => fist.gameObject.SetActive(false));
    }

    private void StartFistIdle()
    {
        if (fist == null)
            return;

        fistIdle?.Kill();
        fist.anchoredPosition = fistRest * fistK;
        fistIdle = fist.DOAnchorPosY((fistRest.y + 10f) * fistK, 0.45f).SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo).SetLink(gameObject);
    }

    private float FistScale()
    {
        float width = ((RectTransform)transform).rect.width;
        float k = fistReferenceWidth > 0f ? Mathf.Clamp(width / fistReferenceWidth, fistMinScale, 1f) : 1f;
        return fistSize * k;
    }

    private void UpdateBrokenText(int hitsLeft)
    {
        if (brokenText != null)
            brokenText.text = "НЕТ СИГНАЛА\n<size=70%>СТУКНИ!</size>";
    }

    private void HideFault()
    {
        if (material != null)
            material.SetFloat(StaticId, 0f);
        if (brokenRoot != null)
            brokenRoot.SetActive(false);
        if (fist != null)
        {
            fistIdle?.Kill();
            fist.DOKill();
            fist.gameObject.SetActive(false);
        }

        // Монитор могли привязать к ленте, которая уже сломана
        if (belt != null && G.faults != null && G.faults.IsBroken(belt))
            HandleBroken(belt);
    }

    private void SetFistAlpha(float a)
    {
        if (fist != null && fist.TryGetComponent(out Image img))
        {
            Color c = img.color;
            c.a = a;
            img.color = c;
        }
    }

    private void Shake(float strength)
    {
        if (shakeTarget == null)
            return;

        shakeTarget.DOKill(true);
        shakeTarget.DOShakeAnchorPos(0.25f, strength, 30).SetLink(gameObject);
    }

    private void Glitch(float from)
    {
        DOTween.Kill(this);
        DOTween.To(() => from, SetGlitch, 0f, glitchDuration).SetTarget(this).SetLink(gameObject);
    }

    private void SetGlitch(float value)
    {
        glitch = value;
        material.SetFloat(GlitchId, glitch);
    }
}
