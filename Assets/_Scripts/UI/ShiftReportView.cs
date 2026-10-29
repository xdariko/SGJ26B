using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Бланк отчёта после смены: строки по одной, итог считается вверх, в конце печать.
public class ShiftReportView : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private RectTransform paper;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text totalText;
    [SerializeField] private RectTransform stamp;
    [SerializeField] private Button bookButton;

    [Header("Анимация")]
    [SerializeField] private float lineDelay = 0.22f;
    [SerializeField] private float totalCountDuration = 0.6f;
    [SerializeField] private int maxComplaintLines = 3;
    [Tooltip("Где начинается колонка значений, в % ширины строки")]
    [SerializeField] private int valueColumn = 72;

    [Header("Цвета (hex для TMP)")]
    [SerializeField] private string badColor = "#A0302A";
    [SerializeField] private string hintColor = "#6B3FA0";
    [SerializeField] private string mutedColor = "#6E5A48";

    private Sequence reveal;

    private void Start()
    {
        G.main.OnPhaseChanged += HandlePhase;
        bookButton.onClick.AddListener(OnBookClicked);
        HandlePhase(G.main.Phase);
    }

    private void OnDestroy()
    {
        if (G.main != null)
            G.main.OnPhaseChanged -= HandlePhase;
        bookButton.onClick.RemoveListener(OnBookClicked);
    }

    private void HandlePhase(GamePhase phase)
    {
        if (phase == GamePhase.Report)
            Show(G.shift.Score);
        else
            Hide();
    }

    private void Show(ShiftScore s)
    {
        root.SetActive(true);
        titleText.text = $"ОТЧЁТ · НОЧЬ {s.night}";
        bodyText.text = string.Empty;
        totalText.text = string.Empty;
        stamp.gameObject.SetActive(false);
        bookButton.gameObject.SetActive(false);

        reveal?.Kill();
        reveal = DOTween.Sequence().SetLink(gameObject);

        if (paper != null)
        {
            paper.anchoredPosition = new Vector2(0f, -80f);
            reveal.Append(paper.DOAnchorPosY(0f, 0.3f).SetEase(Ease.OutBack));
        }

        foreach (string line in BuildLines(s))
        {
            reveal.AppendCallback(() => bodyText.text += line + "\n");
            reveal.AppendInterval(lineDelay);
        }

        reveal.Append(DOTween.To(() => 0, v => totalText.text = FormatTotal(v, s.ectoEarned), s.NetCoins, totalCountDuration));
        reveal.AppendCallback(() =>
        {
            stamp.gameObject.SetActive(true);
            stamp.localScale = Vector3.one * 2f;
        });
        reveal.Append(stamp.DOScale(1f, 0.2f).SetEase(Ease.InQuad));
        if (paper != null)
            reveal.Append(paper.DOShakeAnchorPos(0.15f, 8f));
        reveal.AppendCallback(() => bookButton.gameObject.SetActive(true));
    }

    private string Row(string label, string value) => $"{label}<pos={valueColumn}%>{value}";

    private List<string> BuildLines(ShiftScore s)
    {
        List<string> lines = new()
        {
            Row("Доставлено", s.delivered.ToString()),
            Row("Снято верно", s.removedCorrect.ToString())
        };

        if (s.removedWrong > 0) lines.Add(Row("Снято зря", s.removedWrong.ToString()));
        if (s.ghosts > 0) lines.Add(Row("Пойманные призраки", s.ghosts.ToString()));
        foreach (KeyValuePair<FamiliarDefinition, int> pair in s.familiarCatches)
            lines.Add(Row($"  {pair.Key.displayName} помог", pair.Value.ToString()));
        lines.Add(Row("Лучшее комбо", $"{s.bestStreak} (x{s.bestMult:0.#})"));

        string complaints = s.coinsLost > 0 ? $"<color={badColor}>{s.complaints}  (-{s.coinsLost})</color>" : "0";
        lines.Add(Row("Жалобы", complaints));

        // Одинаковые жалобы склеиваем: «Мумия недовольна... ×12»
        Dictionary<string, int> grouped = new();
        foreach (string c in s.complaintLog)
        {
            grouped.TryGetValue(c, out int n);
            grouped[c] = n + 1;
        }

        int shownComplaints = 0;
        foreach (KeyValuePair<string, int> pair in grouped)
        {
            if (shownComplaints++ >= maxComplaintLines)
                break;
            string count = pair.Value > 1 ? $" x{pair.Value}" : "";
            lines.Add($"<size=80%><color={mutedColor}>  · {pair.Key}{count}</color></size>");
        }

        // Главная подсказка: что камера пропустила и что купить
        foreach (KeyValuePair<ParcelKind, int> pair in s.unseenByKind)
        {
            KindRule rule = G.config.GetRule(pair.Key);
            string hint = string.IsNullOrEmpty(rule.upgradeHint) ? "" : $"\n<size=80%>  > {rule.upgradeHint}</size>";
            lines.Add($"<color={hintColor}>Камера не видела: {pair.Value}{hint}</color>");
        }

        return lines;
    }

    private static string FormatTotal(int coins, int ecto)
    {
        string text = $"ИТОГ: {coins:+#;-#;0} медяков";
        if (ecto > 0) text += $", +{ecto} эктоплазмы";
        return text;
    }

    private void Hide()
    {
        reveal?.Kill();
        root.SetActive(false);
    }

    private void OnBookClicked()
    {
        G.main.OpenBook();
    }
}
