using System;
using System.Collections.Generic;
using DG.Tweening;
using GridSkillTree;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Пошаговое обучение: затемняет экран, оставляет «окно» вокруг нужной части,
// показывает плашку «Шаг N из M» со стрелкой. Пока оно идёт, ленты и таймер стоят (G.IsTutorial).
// Два вида: большое обучение в первую смену и мини-обучения, когда на ленту впервые выезжает
// проклятая посылка, мимик или призрак (окно тогда вокруг самой посылки на мониторе),
// и в первую ночь с новым фамильяром (окно вокруг его подушки).
// Ещё: первая ночь с марками на линиях (окно вокруг монитора линии) и первое появление
// финального узла в книге. Каждое показывается один раз (флаги в SaveData), пропускается кнопкой.
public class TutorialGuideView : MonoBehaviour
{
    public const string SeenId = "guide";
    public const string BookSeenId = "guide_book";
    public const string FinalSeenId = "guide_final";

    public enum Target
    {
        None,
        FirstMonitor,
        LockedMonitor,
        Clock,
        Coins,
        Combo,
        Shelf,
        // Посылка, из-за которой началось мини-обучение. Плашка встаёт на другую половину экрана сама
        Parcel,
        // Сломанный монитор, из-за которого началось мини-обучение (плашка тоже встаёт напротив)
        BrokenMonitor,
        // Ведьмина книга: монеты и эктоплазма внизу, узел дерева (Step.nodeId), кнопка «Продолжить»
        BookCurrency,
        BookNode,
        BookContinue,
        // Подушка фамильяра, из-за которого началось мини-обучение
        Familiar,
        // Монитор линии, из-за которой началось мини-обучение (марки)
        LineMonitor
    }

    public enum Trigger
    {
        Cursed,
        Mimic,
        Ghost,
        // Первый раз сломался монитор (triggerProgress не важен)
        BrokenMonitor,
        // Первая смена с купленным фамильяром (familiarEffect), через familiarDelay после начала
        Familiar,
        // Первая смена, когда у линии есть своя марка и камера их различает (через familiarDelay)
        Stamps
    }

    [Serializable]
    public class Step
    {
        public string title;
        [TextArea(2, 5)] public string text;
        public Target target;
        [Tooltip("Картинки на плашке (мусор, посылки)")]
        public Sprite[] icons;
        [Tooltip("Подпись под картинками")]
        public string iconsCaption;
        [Tooltip("Для цели BookNode: id узла дерева")]
        public string nodeId;
        [Tooltip("Где стоит плашка: якорь на экране (0..1) и сдвиг от него")]
        public Vector2 panelAnchor = new(0.5f, 0.5f);
        public Vector2 panelOffset;
    }

    [Serializable]
    public class ParcelGuide
    {
        [Tooltip("Флаг «уже показано» в SaveData")]
        public string id;
        public Trigger trigger;
        [Tooltip("Насколько посылка должна отъехать от начала ленты (0..1), чтобы её было хорошо видно")]
        [Range(0f, 1f)] public float triggerProgress = 0.3f;
        [Tooltip("Для Trigger.Familiar: unlockEffect фамильяра (fam_cat, fam_raven...)")]
        public string familiarEffect;
        public List<Step> steps = new();
    }

    [Header("Затемнение с окном: четыре прямоугольника вокруг цели")]
    [SerializeField] private RectTransform root;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private RectTransform dimTop;
    [SerializeField] private RectTransform dimBottom;
    [SerializeField] private RectTransform dimLeft;
    [SerializeField] private RectTransform dimRight;
    [SerializeField] private Image fullDim;
    [SerializeField] private RectTransform highlight;
    [SerializeField] private float holePadding = 14f;
    [Tooltip("Сколько пикселей спрайта рамки прозрачно снаружи линии (у button3 это 33). Рамку выносим наружу на столько, чтобы линия легла ровно на край затемнения")]
    [SerializeField] private float frameOuterInset = 33f;
    [Tooltip("Окно вокруг посылки делаем побольше: свечение шире самой коробки")]
    [SerializeField] private float parcelPadding = 24f;

    [Header("Стрелка (рисунок смотрит вправо)")]
    [SerializeField] private RectTransform arrow;
    [SerializeField] private float arrowGap = 20f;
    [SerializeField] private float arrowBob = 14f;
    [SerializeField] private float arrowBobSpeed = 4f;

    [Header("Плашка")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private TMP_Text stepText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private GameObject iconsRow;
    [SerializeField] private Image[] iconImages;
    [SerializeField] private TMP_Text iconsCaptionText;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text nextLabel;
    [SerializeField] private Button skipButton;

    [Header("Цели")]
    [SerializeField] private MonitorWallView monitorWall;
    [SerializeField] private RectTransform clock;
    [Tooltip("Окно «Монеты» тянется от этого элемента (иконка монеты)...")]
    [SerializeField] private RectTransform coins;
    [Tooltip("...до этого (число эктоплазмы)")]
    [SerializeField] private RectTransform ecto;
    [SerializeField] private RectTransform combo;
    [SerializeField] private RectTransform shelf;
    [Tooltip("Книга: число монет и число эктоплазмы на нижней панели")]
    [SerializeField] private RectTransform bookCoins;
    [SerializeField] private RectTransform bookEcto;
    [SerializeField] private RectTransform bookContinue;
    [Tooltip("Где искать кнопки узлов дерева")]
    [SerializeField] private RectTransform bookTree;

    [Header("Время")]
    [Tooltip("Сколько ждать после начала смены: дать прочитать «НОЧЬ 1» и выехать первым посылкам")]
    [SerializeField] private float startDelay = 2.4f;
    [SerializeField] private float fade = 0.25f;
    [Tooltip("Через сколько секунд смены показывать обучение нового фамильяра: заставка «НОЧЬ N» уже ушла")]
    [SerializeField] private float familiarDelay = 4.5f;

    [SerializeField] private string nextCaption = "Далее";
    [SerializeField] private string lastCaption = "Начать смену!";
    [Tooltip("Последняя кнопка мини-обучения")]
    [SerializeField] private string resumeCaption = "Понятно!";
    [SerializeField] private string stepFormat = "ШАГ {0} ИЗ {1}";
    [Tooltip("Над плашкой из одного шага вместо «ШАГ 1 ИЗ 1»")]
    [SerializeField] private string singleCaption = "НОВОЕ НА ЛЕНТЕ";
    [Tooltip("Над плашкой финального узла")]
    [SerializeField] private string finalCaption = "ЦЕЛЬ";

    [Header("Первая смена")]
    [SerializeField] private List<Step> steps = new();

    [Header("Первое открытие книги")]
    [SerializeField] private List<Step> bookSteps = new();
    [Tooltip("Пауза после открытия книги: дерево успевает построиться и проявиться")]
    [SerializeField] private float bookDelay = 0.5f;

    [Header("Финальный узел книги: подсказка, когда он впервые появился")]
    [SerializeField] private string finalNodeId = Effects.FinalOrder;
    [SerializeField] private List<Step> finalSteps = new();

    [Header("Мини-обучения при первой встрече")]
    [SerializeField] private List<ParcelGuide> parcelGuides = new();
    [Tooltip("Пауза между двумя мини-обучениями подряд, чтобы плашки не шли стеной")]
    [SerializeField] private float guideGap = 1.5f;

    private List<Step> current;
    private string currentId;
    private Parcel focus;
    private Belt focusBelt;
    private string focusFamiliar;
    private float shiftTime;
    private int index = -1;
    private float wait = -1f;
    private float bookWait = -1f;
    private float gap;
    private Rect hole;
    private Vector2 arrowBase;
    private Vector2 arrowDir;
    private readonly Vector3[] corners = new Vector3[4];

    private bool Active => index >= 0;

    private void Start()
    {
        group.alpha = 0f;
        group.blocksRaycasts = false;

        nextButton.onClick.AddListener(Next);
        skipButton.onClick.AddListener(Finish);

        G.shift.OnShiftStarted += HandleShiftStarted;
        G.main.OnPhaseChanged += HandlePhase;
        if (G.tree != null)
            G.tree.OnTreeChanged += HandleTreeChanged;
        if (G.shift.Current == ShiftController.State.Running)
            HandleShiftStarted();
    }

    private void OnDestroy()
    {
        // Вышли из сцены посреди обучения: статический флаг не должен заморозить следующую игру
        if (Active)
            G.IsTutorial = false;

        if (G.shift != null)
            G.shift.OnShiftStarted -= HandleShiftStarted;
        if (G.main != null)
            G.main.OnPhaseChanged -= HandlePhase;
        if (G.tree != null)
            G.tree.OnTreeChanged -= HandleTreeChanged;
    }

    private void HandlePhase(GamePhase phase)
    {
        bookWait = -1f;
        if (phase != GamePhase.Book)
            return;

        if ((bookSteps.Count > 0 && !SaveData.IsHintSeen(BookSeenId)) || FinalReady())
            bookWait = bookDelay;
    }

    // Купили последний узел перед финалом: финальный узел только что появился в дереве
    private void HandleTreeChanged()
    {
        if (!Active && bookWait < 0f && G.main.Phase == GamePhase.Book && FinalReady())
            bookWait = bookDelay;
    }

    // Финальный узел виден (все предыдущие куплены), сам ещё не куплен, подсказки не было
    private bool FinalReady()
    {
        if (finalSteps.Count == 0 || SaveData.IsHintSeen(FinalSeenId) || G.tree == null || G.tree.TreeData == null)
            return false;

        SkillNodeData node = G.tree.TreeData.nodes.Find(n => n.id == finalNodeId);
        return node != null && G.tree.GetLevel(node.id) == 0 && G.tree.IsUnlocked(node);
    }

    private void HandleShiftStarted()
    {
        shiftTime = 0f;
        if (SaveData.IsHintSeen(SeenId) || steps.Count == 0 || G.stats.finalShift)
            return;

        wait = startDelay;
    }

    private void Update()
    {
        if (Active)
        {
            // Мониторы раскладываются сеткой и могут поменять размер (окно, разрешение) — окно следует за целью
            UpdateHole();
            float bob = (Mathf.Sin(Time.unscaledTime * arrowBobSpeed) * 0.5f + 0.5f) * arrowBob;
            arrow.anchoredPosition = arrowBase + arrowDir * bob;
            return;
        }

        if (bookWait >= 0f)
        {
            if (G.IsPaused || ComicCutsceneUI.BlocksPause || G.main.Phase != GamePhase.Book)
                return;
            bookWait -= Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            if (bookWait >= 0f)
                return;

            if (bookSteps.Count > 0 && !SaveData.IsHintSeen(BookSeenId))
                Begin(bookSteps, BookSeenId, null);
            else if (FinalReady())
                Begin(finalSteps, FinalSeenId, null);
            return;
        }

        if (G.IsPaused || ComicCutsceneUI.BlocksPause || G.shift.Current != ShiftController.State.Running)
            return;

        shiftTime += Mathf.Min(Time.unscaledDeltaTime, 0.1f);

        if (wait >= 0f)
        {
            // Ограничиваем шаг: после подвисания при загрузке обучение не должно выскочить поверх заставки
            wait -= Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            if (wait < 0f)
                Begin(steps, SeenId, null);
            return;
        }

        if (gap > 0f)
        {
            gap -= Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            return;
        }

        CheckParcelGuides();
    }

    // Первая проклятая / мимик / призрак, отъехавшие от начала ленты, запускают своё мини-обучение
    private void CheckParcelGuides()
    {
        foreach (ParcelGuide guide in parcelGuides)
        {
            if (guide.steps.Count == 0 || SaveData.IsHintSeen(guide.id))
                continue;

            if (guide.trigger == Trigger.BrokenMonitor)
            {
                if (G.faults == null)
                    continue;

                foreach (Belt belt in G.shift.Belts)
                {
                    if (!belt.IsRunning || !G.faults.IsBroken(belt))
                        continue;

                    Begin(guide.steps, guide.id, null, belt);
                    return;
                }
                continue;
            }

            if (guide.trigger == Trigger.Familiar)
            {
                if (shiftTime < familiarDelay || !G.stats.HasUnlock(guide.familiarEffect)
                    || FindFamiliarSlot(guide.familiarEffect) == null)
                    continue;

                Begin(guide.steps, guide.id, null, null, guide.familiarEffect);
                return;
            }

            if (guide.trigger == Trigger.Stamps)
            {
                Belt line = shiftTime >= familiarDelay && G.stats.camLevel >= 1 ? FindStampLine() : null;
                if (line == null)
                    continue;

                Begin(guide.steps, guide.id, null, line);
                return;
            }

            foreach (Belt belt in G.shift.Belts)
            {
                if (!belt.IsRunning)
                    continue;

                float length = belt.Length;
                foreach (Parcel p in belt.Parcels)
                {
                    if (p == null || p.IsResolved || !Matches(guide.trigger, p) || p.Distance < length * guide.triggerProgress)
                        continue;

                    Begin(guide.steps, guide.id, p);
                    return;
                }
            }
        }
    }

    // Первая работающая линия со своей маркой
    private static Belt FindStampLine()
    {
        foreach (Belt belt in G.shift.Belts)
        {
            if (belt == null || !belt.IsRunning || belt.Addressee == Addressee.Any)
                continue;

            AddresseeInfo info = G.config.GetAddressee(belt.Addressee);
            if (info != null && info.stamp != null)
                return belt;
        }
        return null;
    }

    private static bool Matches(Trigger trigger, Parcel p)
    {
        return trigger switch
        {
            Trigger.Cursed => p.Kind == ParcelKind.Cursed,
            Trigger.Mimic => p.Kind == ParcelKind.Mimic,
            Trigger.Ghost => p.HasGhost,
            _ => false
        };
    }

    private void Begin(List<Step> list, string id, Parcel target, Belt belt = null, string familiar = null)
    {
        wait = -1f;
        if (list.Count == 0)
            return;

        focusFamiliar = familiar;

        current = list;
        currentId = id;
        focus = target;
        focusBelt = target != null ? target.Belt : belt;
        G.IsTutorial = true;
        index = 0;
        group.blocksRaycasts = true;
        group.DOKill();
        group.DOFade(1f, fade).SetUpdate(true).SetLink(gameObject);
        ShowStep();
    }

    private void Next()
    {
        AudioHub.Play(SfxId.UiClick);

        if (index + 1 >= current.Count)
        {
            Finish();
            return;
        }

        index++;
        panelGroup.DOKill();
        panelGroup.alpha = 0f;
        ShowStep();
    }

    private void ShowStep()
    {
        Step step = current[index];
        bool last = index + 1 >= current.Count;
        stepText.text = current.Count > 1 ? string.Format(stepFormat, index + 1, current.Count)
            : currentId == FinalSeenId ? finalCaption : singleCaption;
        titleText.text = step.title;
        titleText.gameObject.SetActive(!string.IsNullOrEmpty(step.title));
        bodyText.text = step.text;
        nextLabel.text = !last ? nextCaption : currentId == SeenId ? lastCaption : resumeCaption;

        bool icons = step.icons != null && step.icons.Length > 0;
        iconsRow.SetActive(icons);
        for (int i = 0; i < iconImages.Length; i++)
        {
            bool on = icons && i < step.icons.Length && step.icons[i] != null;
            iconImages[i].gameObject.SetActive(on);
            if (on)
            {
                iconImages[i].sprite = step.icons[i];
                iconImages[i].preserveAspect = true;
            }
        }

        bool caption = icons && !string.IsNullOrEmpty(step.iconsCaption);
        iconsCaptionText.gameObject.SetActive(caption);
        if (caption)
            iconsCaptionText.text = step.iconsCaption;

        if (step.target == Target.BookNode)
            RevealBookNode(step.nodeId);

        PlacePanel(step);
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);

        UpdateHole();

        panelGroup.DOFade(1f, fade).SetUpdate(true).SetLink(gameObject);
        panel.localScale = Vector3.one * 0.94f;
        panel.DOKill();
        panel.DOScale(1f, fade).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);

        highlight.DOKill();
        highlight.localScale = Vector3.one;
        Image frame = highlight.GetComponent<Image>();
        frame.DOKill();
        frame.color = new Color(frame.color.r, frame.color.g, frame.color.b, 1f);
        frame.DOFade(0.35f, 0.6f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true).SetLink(gameObject);
    }

    // Плашка мини-обучения встаёт на ту половину экрана, где посылки нет
    private void PlacePanel(Step step)
    {
        Vector2 anchor = step.panelAnchor;
        Vector2 offset = step.panelOffset;

        bool auto = step.target == Target.Parcel || step.target == Target.BrokenMonitor || step.target == Target.LineMonitor;
        if (auto && TryGetTargetRect(step.target, out Rect r))
        {
            bool parcelLeft = r.center.x < 0f;
            anchor = new Vector2(parcelLeft ? 1f : 0f, 0.5f);
            offset = new Vector2(parcelLeft ? -40f : 40f, 0f);
        }

        panel.anchorMin = panel.anchorMax = panel.pivot = anchor;
        panel.anchoredPosition = offset;
    }

    private void Finish()
    {
        if (!Active)
            return;

        index = -1;
        focus = null;
        focusBelt = null;
        G.IsTutorial = false;
        SaveData.MarkHintSeen(currentId);
        gap = guideGap;
        // После обучения книги сразу покажем финальный узел, если он уже открыт
        if (G.main.Phase == GamePhase.Book && FinalReady())
            bookWait = bookDelay;
        group.blocksRaycasts = false;
        group.DOKill();
        group.DOFade(0f, fade).SetUpdate(true).SetLink(gameObject);
        highlight.GetComponent<Image>().DOKill();
    }

    private void UpdateHole()
    {
        Step step = current[index];
        bool hasHole = TryGetTargetRect(step.target, out hole);

        fullDim.enabled = !hasHole;
        dimTop.gameObject.SetActive(hasHole);
        dimBottom.gameObject.SetActive(hasHole);
        dimLeft.gameObject.SetActive(hasHole);
        dimRight.gameObject.SetActive(hasHole);
        highlight.gameObject.SetActive(hasHole);
        arrow.gameObject.SetActive(hasHole);

        if (!hasHole)
            return;

        float pad = step.target == Target.Parcel ? parcelPadding : holePadding;
        Rect full = root.rect;
        hole.xMin = Mathf.Max(full.xMin, hole.xMin - pad);
        hole.yMin = Mathf.Max(full.yMin, hole.yMin - pad);
        hole.xMax = Mathf.Min(full.xMax, hole.xMax + pad);
        hole.yMax = Mathf.Min(full.yMax, hole.yMax + pad);

        SetRect(dimTop, new Rect(full.xMin, hole.yMax, full.width, full.yMax - hole.yMax));
        SetRect(dimBottom, new Rect(full.xMin, full.yMin, full.width, hole.yMin - full.yMin));
        SetRect(dimLeft, new Rect(full.xMin, hole.yMin, hole.xMin - full.xMin, hole.height));
        SetRect(dimRight, new Rect(hole.xMax, hole.yMin, full.xMax - hole.xMax, hole.height));
        SetRect(highlight, FrameRect(hole));

        PlaceArrow();
    }

    // Рамка нарезана 9-slice: прозрачные поля спрайта масштабируются вместе с pixelsPerUnitMultiplier
    private Rect FrameRect(Rect r)
    {
        Image img = highlight.GetComponent<Image>();
        float scale = img.pixelsPerUnit * img.pixelsPerUnitMultiplier;
        float outset = scale > 0f ? frameOuterInset / scale : 0f;
        return new Rect(r.x - outset, r.y - outset, r.width + outset * 2f, r.height + outset * 2f);
    }

    // Стрелка выходит из края плашки и смотрит на центр цели
    private void PlaceArrow()
    {
        Rect p = GetLocalRect(panel);
        Vector2 dir = hole.center - p.center;
        if (dir.sqrMagnitude < 1f)
            dir = Vector2.up;
        dir.Normalize();

        float length = arrow.rect.width;
        Vector2 edge = p.center + dir * DistanceToEdge(p, dir);
        arrowDir = dir;
        arrowBase = edge + dir * (arrowGap + length * 0.5f);
        arrow.anchorMin = arrow.anchorMax = new Vector2(0.5f, 0.5f);
        arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    private static float DistanceToEdge(Rect r, Vector2 dir)
    {
        float tx = Mathf.Abs(dir.x) > 0.0001f ? r.width * 0.5f / Mathf.Abs(dir.x) : float.MaxValue;
        float ty = Mathf.Abs(dir.y) > 0.0001f ? r.height * 0.5f / Mathf.Abs(dir.y) : float.MaxValue;
        return Mathf.Min(tx, ty);
    }

    private bool TryGetTargetRect(Target target, out Rect rect)
    {
        rect = default;
        switch (target)
        {
            case Target.FirstMonitor:
                return TryGetMonitor(false, out rect);
            case Target.LockedMonitor:
                return TryGetMonitor(true, out rect);
            case Target.Clock:
                return TryUnion(out rect, clock);
            case Target.Coins:
                return TryUnion(out rect, coins, ecto);
            case Target.Combo:
                return TryUnion(out rect, combo);
            case Target.Shelf:
                return TryUnion(out rect, shelf);
            case Target.Parcel:
                return TryGetParcel(out rect);
            case Target.BrokenMonitor:
            case Target.LineMonitor:
                return TryGetBeltMonitor(focusBelt, out rect);
            case Target.BookCurrency:
                return TryUnion(out rect, bookCoins, bookEcto);
            case Target.BookContinue:
                return TryUnion(out rect, bookContinue);
            case Target.BookNode:
                return TryGetBookNode(current[index].nodeId, out rect);
            case Target.Familiar:
                FamiliarSlotView slot = FindFamiliarSlot(focusFamiliar);
                return slot != null && TryUnion(out rect, (RectTransform)slot.transform);
            default:
                return false;
        }
    }

    private bool TryGetMonitor(bool locked, out Rect rect)
    {
        rect = default;
        if (monitorWall == null)
            return false;

        foreach (MonitorView m in monitorWall.GetComponentsInChildren<MonitorView>())
            if (m.gameObject.activeInHierarchy && (m.Belt == null) == locked)
                return TryUnion(out rect, (RectTransform)m.transform);

        return false;
    }

    private static FamiliarSlotView FindFamiliarSlot(string unlockEffect)
    {
        if (string.IsNullOrEmpty(unlockEffect))
            return null;

        foreach (FamiliarSlotView s in FindObjectsByType<FamiliarSlotView>(FindObjectsSortMode.None))
            if (s.Familiar != null && s.Familiar.def != null && s.Familiar.def.unlockEffect == unlockEffect)
                return s;
        return null;
    }

    private bool TryGetBookNode(string nodeId, out Rect rect)
    {
        rect = default;
        if (bookTree == null || string.IsNullOrEmpty(nodeId))
            return false;

        foreach (GridSkillTree.SkillNodeButton b in bookTree.GetComponentsInChildren<GridSkillTree.SkillNodeButton>())
            if (b.Node != null && b.Node.id == nodeId)
                return TryUnion(out rect, (RectTransform)b.transform);

        return false;
    }

    // Узел за краем видимой части дерева: прокручиваем дерево, чтобы он встал в центр
    private void RevealBookNode(string nodeId)
    {
        if (bookTree == null || string.IsNullOrEmpty(nodeId))
            return;

        foreach (SkillNodeButton b in bookTree.GetComponentsInChildren<SkillNodeButton>())
        {
            if (b.Node == null || b.Node.id != nodeId)
                continue;

            ScrollRect scroll = b.GetComponentInParent<ScrollRect>();
            if (scroll == null || scroll.content == null)
                return;

            RectTransform view = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
            RectTransform node = (RectTransform)b.transform;
            Vector3 nodeCenter = node.TransformPoint(node.rect.center);
            Vector2 inView = view.InverseTransformPoint(nodeCenter);
            Rect inner = view.rect;
            float margin = Mathf.Max(node.rect.width, node.rect.height);
            if (inView.x > inner.xMin + margin && inView.x < inner.xMax - margin
                && inView.y > inner.yMin + margin && inView.y < inner.yMax - margin)
                return;

            Vector3 shift = view.TransformPoint(inner.center) - nodeCenter;
            scroll.StopMovement();
            scroll.content.position += shift;
            return;
        }
    }

    private bool TryGetBeltMonitor(Belt belt, out Rect rect)
    {
        rect = default;
        if (belt == null || monitorWall == null)
            return false;

        foreach (MonitorView m in monitorWall.GetComponentsInChildren<MonitorView>())
            if (m.Belt == belt)
                return TryUnion(out rect, (RectTransform)m.transform);

        return false;
    }

    // Посылка на экране своего монитора (с учётом рыбьего глаза)
    private bool TryGetParcel(out Rect rect)
    {
        rect = default;
        if (focus == null || monitorWall == null)
            return false;

        foreach (MonitorView m in monitorWall.GetComponentsInChildren<MonitorView>())
        {
            if (m.Belt != focus.Belt || !m.TryGetScreenCorners(focus.Visual.GlowBounds, out Vector3 min, out Vector3 max))
                continue;

            Vector2 a = WorldToLocal(min, null);
            Vector2 b = WorldToLocal(max, null);
            rect = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            return true;
        }

        return false;
    }

    private bool TryUnion(out Rect rect, params RectTransform[] targets)
    {
        rect = default;
        bool any = false;
        foreach (RectTransform t in targets)
        {
            if (t == null || !t.gameObject.activeInHierarchy)
                continue;

            Rect r = GetLocalRect(t);
            if (!any)
                rect = r;
            else
                rect = Rect.MinMaxRect(Mathf.Min(rect.xMin, r.xMin), Mathf.Min(rect.yMin, r.yMin),
                    Mathf.Max(rect.xMax, r.xMax), Mathf.Max(rect.yMax, r.yMax));
            any = true;
        }

        return any;
    }

    // Прямоугольник элемента из любого Overlay-канваса в координатах нашего корня
    private Rect GetLocalRect(RectTransform t)
    {
        t.GetWorldCorners(corners);
        // У текста берём сами буквы, а не рамку поля: иначе окно кривое (поле выше иконки рядом)
        if (t.TryGetComponent(out TMP_Text text) && !string.IsNullOrEmpty(text.text))
        {
            text.ForceMeshUpdate();
            Bounds tb = text.textBounds;
            corners[0] = t.TransformPoint(new Vector3(tb.min.x, tb.min.y));
            corners[1] = t.TransformPoint(new Vector3(tb.min.x, tb.max.y));
            corners[2] = t.TransformPoint(new Vector3(tb.max.x, tb.max.y));
            corners[3] = t.TransformPoint(new Vector3(tb.max.x, tb.min.y));
        }

        Canvas canvas = t.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

        Vector2 min = new(float.MaxValue, float.MaxValue);
        Vector2 max = new(float.MinValue, float.MinValue);
        for (int i = 0; i < 4; i++)
        {
            Vector2 local = WorldToLocal(corners[i], cam);
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }

        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private Vector2 WorldToLocal(Vector3 world, Camera cam)
    {
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, world);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out Vector2 local);
        return local;
    }

    // Все прямоугольники затемнения живут с якорем в центре корня
    private static void SetRect(RectTransform rt, Rect r)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(Mathf.Max(0f, r.width), Mathf.Max(0f, r.height));
        rt.anchoredPosition = r.center;
    }
}
