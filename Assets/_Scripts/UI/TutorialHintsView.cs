using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// Обучение: короткие одноразовые подсказки в углу экрана. Каждая появляется,
// когда игрок впервые с чем-то сталкивается, и больше не показывается (флаги в SaveData).
// Игру не останавливает, закрывается сама или кликом.
public class TutorialHintsView : MonoBehaviour, IPointerClickHandler
{
    [Serializable]
    public class Hint
    {
        public string id;
        [TextArea(2, 4)] public string text;
    }

    public const string Trash = "trash";
    public const string Complaint = "complaint";
    public const string Unseen = "unseen";
    public const string Book = "book";
    public const string Stamps = "stamps";
    public const string Lines = "lines";
    public const string Curse = "curse";
    public const string Spirits = "spirits";
    public const string Combo = "combo";
    public const string Cat = "cat";
    public const string Raven = "raven";
    public const string Owl = "owl";
    public const string Fox = "fox";

    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_Text text;

    [Header("Где стоит плашка")]
    [Tooltip("На смене: левый нижний угол, между мониторами и подушками")]
    [SerializeField] private Vector2 shiftAnchor = Vector2.zero;
    [SerializeField] private Vector2 shiftPosition = new(24f, 24f);
    [Tooltip("В книге: сверху по центру, чтобы не закрывать кошелёк и кнопку")]
    [SerializeField] private Vector2 bookAnchor = new(0.5f, 1f);
    [SerializeField] private Vector2 bookPosition = new(0f, -32f);

    [SerializeField] private float startDelay = 3.8f;
    [SerializeField] private float fadeIn = 0.3f;
    [SerializeField] private float baseHold = 3.5f;
    [SerializeField] private float holdPerChar = 0.045f;
    [SerializeField] private float fadeOut = 0.4f;
    [SerializeField] private float gap = 0.6f;

    [SerializeField] private List<Hint> hints = new()
    {
        new Hint { id = Trash, text = "Камни, кости и гнилые тыквы снимай кликом.\nВсё остальное пусть едет к адресатам." },
        new Hint { id = Complaint, text = "Мусор доехал до адресата: жалоба и штраф." },
        new Hint { id = Unseen, text = "Камера этого не видит.\nИщи улучшение в Ведьминой книге." },
        new Hint { id = Book, text = "Трать монеты на улучшения в книге.\nНаведи на узел, чтобы узнать, что он даёт." },
        new Hint { id = Stamps, text = "Теперь видно марки. Марка ленты нарисована\nрядом с её названием. Посылки с чужой маркой снимай." },
        new Hint { id = Lines, text = "У каждой ленты своя марка, она рядом с названием.\nПосылки с чужой маркой снимай, их переотправят." },
        new Hint { id = Curse, text = "Фиолетовая аура — проклятие.\nСнимай обязательно, иначе большая жалоба!" },
        new Hint { id = Spirits, text = "Красная аура — мимик, снимай.\nБирюзовая — призрак: кликни, и он отдаст эктоплазму." },
        new Hint { id = Combo, text = "Верные клики подряд увеличивают награду.\nКаждые 10 подряд дают каплю эктоплазмы." },
        new Hint { id = Cat, text = "Кот сам снимает мусор с ленты." },
        new Hint { id = Raven, text = "Ворон снимает посылки с чужой маркой." },
        new Hint { id = Owl, text = "Сова снимает проклятые посылки." },
        new Hint { id = Fox, text = "Лиса ловит мимиков и выпускает призраков." },
    };

    // Подсказки, которые показываются только в своей фазе (книга или смена)
    private struct Pending
    {
        public string id;
        public GamePhase phase;
    }

    private readonly List<Pending> queue = new();
    private float wait;
    private bool showing;
    private Sequence current;

    private void Start()
    {
        group.alpha = 0f;
        group.blocksRaycasts = false;

        G.shift.OnShiftStarted += HandleShiftStarted;
        G.shift.OnParcelResolved += HandleParcel;
        G.main.OnPhaseChanged += HandlePhase;
    }

    private void OnDestroy()
    {
        if (G.shift != null)
        {
            G.shift.OnShiftStarted -= HandleShiftStarted;
            G.shift.OnParcelResolved -= HandleParcel;
        }

        if (G.main != null)
            G.main.OnPhaseChanged -= HandlePhase;
    }

    private void HandleShiftStarted()
    {
        // Сначала дать прочитать заставку «НОЧЬ N»
        wait = startDelay;

        GameStats s = G.stats;
        Enqueue(Trash, GamePhase.Shift);
        // Вторая линия открывается только после «Ровной резьбы», её подсказка заменяет подсказку о печатях
        if (s.lines >= 2)
            Enqueue(Lines, GamePhase.Shift);
        else if (s.camLevel >= 1)
            Enqueue(Stamps, GamePhase.Shift);
        if (s.camLevel >= 2)
            Enqueue(Curse, GamePhase.Shift);
        if (s.camLevel >= 3)
            Enqueue(Spirits, GamePhase.Shift);
        if (s.famCat)
            Enqueue(Cat, GamePhase.Shift);
        if (s.famRaven)
            Enqueue(Raven, GamePhase.Shift);
        if (s.famOwl)
            Enqueue(Owl, GamePhase.Shift);
        if (s.famBat)
            Enqueue(Fox, GamePhase.Shift);
    }

    private void HandleParcel(ParcelOutcome o)
    {
        if (o.familiar != null)
            return;

        if (o.penalty > 0)
            Enqueue(o.unseen ? Unseen : Complaint, GamePhase.Shift, true);
        else if (o.comboMult > 1.01f)
            Enqueue(Combo, GamePhase.Shift);
    }

    private void HandlePhase(GamePhase phase)
    {
        if (phase == GamePhase.Book)
            Enqueue(Book, GamePhase.Book, true);

        // Подсказка из другой фазы не должна висеть поверх отчёта или книги
        if (showing && phase != GamePhase.Shift)
            Hide();
    }

    private void Enqueue(string id, GamePhase phase, bool first = false)
    {
        if (SaveData.IsHintSeen(id) || queue.Exists(p => p.id == id))
            return;

        Pending p = new() { id = id, phase = phase };
        if (first)
            queue.Insert(0, p);
        else
            queue.Add(p);
    }

    private void Update()
    {
        if (showing || G.IsPaused || ComicCutsceneUI.BlocksPause || queue.Count == 0)
            return;

        // Ограничиваем шаг: после подвисания (загрузка, свёрнутое окно) подсказка не должна выскочить раньше заставки
        wait -= Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        if (wait > 0f)
            return;

        GamePhase phase = G.main.Phase;
        if (phase == GamePhase.Shift && G.shift.Current != ShiftController.State.Running)
            return;

        int index = queue.FindIndex(p => p.phase == phase);
        if (index < 0)
            return;

        string id = queue[index].id;
        queue.RemoveAt(index);
        PlaceFor(phase);
        Show(id);
    }

    private void PlaceFor(GamePhase phase)
    {
        RectTransform rt = (RectTransform)transform;
        bool book = phase == GamePhase.Book;
        Vector2 anchor = book ? bookAnchor : shiftAnchor;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = book ? bookPosition : shiftPosition;
    }

    private void Show(string id)
    {
        Hint hint = hints.Find(h => h.id == id);
        SaveData.MarkHintSeen(id);
        if (hint == null || string.IsNullOrEmpty(hint.text))
            return;

        text.text = hint.text;
        showing = true;
        group.blocksRaycasts = true;

        current?.Kill();
        group.alpha = 0f;
        current = DOTween.Sequence().SetUpdate(true).SetLink(gameObject)
            .Append(group.DOFade(1f, fadeIn))
            .AppendInterval(baseHold + hint.text.Length * holdPerChar)
            .Append(group.DOFade(0f, fadeOut))
            .OnComplete(Finish);
    }

    private void Hide()
    {
        current?.Kill();
        current = DOTween.Sequence().SetUpdate(true).SetLink(gameObject)
            .Append(group.DOFade(0f, fadeOut))
            .OnComplete(Finish);
    }

    private void Finish()
    {
        showing = false;
        group.blocksRaycasts = false;
        wait = gap;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (showing)
            Hide();
    }
}
