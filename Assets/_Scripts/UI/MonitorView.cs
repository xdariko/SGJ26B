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

    [Header("Таймкод: 23:00 → 00:00")]
    [SerializeField] private int startHour = 23;
    [SerializeField] private int clockMinutes = 60;

    private static readonly int NoiseId = Shader.PropertyToID("_Noise");
    private static readonly int GlitchId = Shader.PropertyToID("_Glitch");
    private static readonly int DistortionId = Shader.PropertyToID("_Distortion");

    private Belt belt;
    private int index;
    private Material material;
    private float baseNoise;
    private float glitch;

    public Belt Belt => belt;

    private void Awake()
    {
        // Свой экземпляр материала, чтобы у каждого монитора были свои помехи
        material = new Material(screen.material);
        screen.material = material;
        baseNoise = material.GetFloat(NoiseId);
    }

    private void Start()
    {
        G.shift.OnParcelResolved += HandleOutcome;
    }

    private void OnDestroy()
    {
        if (G.shift != null)
            G.shift.OnParcelResolved -= HandleOutcome;
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
        SetGlitch(0f);
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

    public void OnPointerDown(PointerEventData eventData)
    {
        if (belt == null || G.IsPaused || eventData.button != PointerEventData.InputButton.Left)
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

        if (shakeTarget != null)
        {
            shakeTarget.DOKill(true);
            shakeTarget.DOShakeAnchorPos(0.3f, shakeStrength, 30).SetLink(gameObject);
        }

        DOTween.Kill(this);
        DOTween.To(() => 1f, SetGlitch, 0f, glitchDuration).SetTarget(this).SetLink(gameObject);
    }

    private void SetGlitch(float value)
    {
        glitch = value;
        material.SetFloat(GlitchId, glitch);
    }
}
