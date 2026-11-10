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
    [Tooltip("Заголовки разделов: «Фамильяры», «Жалобы», «Совет»")]
    [SerializeField] private string headerColor = "#8C755E";
    [SerializeField] private string goodColor = "#3F7A4A";

    [Header("Размер текста")]
    [Tooltip("Если строк много, шрифт уменьшается, чтобы отчёт влез в бумагу")]
    [SerializeField] private float minFontSize = 22f;
    [Tooltip("Зазор между последней строкой и «ИТОГ»")]
    [SerializeField] private float totalGap = 8f;

    [Header("Иконки валют (имена спрайтов в TMP Sprite Asset у текста)")]
    [SerializeField] private string coinSprite = "coin";
    [SerializeField] private string ectoSprite = "ecto";
    [Tooltip("Цвет иконок на бумаге отчёта: пастельные иконки из HUD на светлом бланке не видно")]
    [SerializeField] private string coinIconColor = "#A8873E";
    [SerializeField] private string ectoIconColor = "#4F86AD";

    private Sequence reveal;
    // Где бумага стоит в сцене: анимация въезжает именно сюда
    private Vector2 paperHome;
    private float bodyFontSize;

    private void Awake()
    {
        if (paper != null)
            paperHome = paper.anchoredPosition;
        bodyFontSize = bodyText.fontSize;
    }

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
        titleText.text = "ОТЧЁТ";
        bodyText.text = string.Empty;
        totalText.text = string.Empty;
        stamp.gameObject.SetActive(false);
        bookButton.gameObject.SetActive(false);

        reveal?.Kill();
        reveal = DOTween.Sequence().SetLink(gameObject);

        if (paper != null)
        {
            paper.anchoredPosition = paperHome + new Vector2(0f, -80f);
            reveal.Append(paper.DOAnchorPos(paperHome, 0.3f).SetEase(Ease.OutBack));
        }

        List<string> lines = BuildLines(s);
        FitFont(lines);

        foreach (string line in lines)
        {
            reveal.AppendCallback(() =>
            {
                bodyText.text += line + "\n";
                AudioHub.Play(SfxId.ReportLine);
            });
            reveal.AppendInterval(lineDelay);
        }

        reveal.Append(DOTween.To(() => 0, v => totalText.text = FormatTotal(v, s.ectoEarned), s.NetCoins, totalCountDuration));
        reveal.AppendCallback(() =>
        {
            stamp.gameObject.SetActive(true);
            stamp.localScale = Vector3.one * 2f;
        });
        reveal.Append(stamp.DOScale(1f, 0.2f).SetEase(Ease.InQuad));
        reveal.AppendCallback(() => AudioHub.Play(SfxId.Stamp));
        if (paper != null)
            reveal.Append(paper.DOShakeAnchorPos(0.15f, 8f));
        reveal.AppendCallback(() => bookButton.gameObject.SetActive(true));
    }

    // Текст печатается по строке: размер подбираем заранее по полному отчёту, чтобы он не прыгал
    private void FitFont(List<string> lines)
    {
        string full = string.Join("\n", lines);
        RectTransform body = bodyText.rectTransform;
        float width = body.rect.width;
        // Места по высоте: от верха текста до верха строки «ИТОГ» (поле текста заходит под неё)
        float height = body.rect.height;
        if (totalText != null)
        {
            Vector3[] c = new Vector3[4];
            totalText.rectTransform.GetWorldCorners(c);
            float totalTop = body.InverseTransformPoint(c[1]).y;
            height = Mathf.Min(height, body.rect.yMax - totalTop - totalGap);
        }

        float size = bodyFontSize;
        bodyText.fontSize = size;
        while (size > minFontSize && bodyText.GetPreferredValues(full, width, 0f).y > height)
        {
            size -= 1f;
            bodyText.fontSize = size;
        }
    }

    private string Row(string label, string value) => $"{label}<pos={valueColumn}%>{value}";

    // Заголовок раздела: пустая строка-отступ, потом мелкие разреженные буквы
    private string Header(string title) =>
        $"<size=40%> </size>\n<size=72%><color={headerColor}><cspace=0.15em>{title}</cspace></color></size>";

    private List<string> BuildLines(ShiftScore s)
    {
        List<string> lines = new()
        {
            Row("Доставлено", s.delivered.ToString()),
            Row("Снято верно", $"<color={goodColor}>{s.removedCorrect}</color>")
        };

        if (s.removedWrong > 0) lines.Add(Row("Снято зря", $"<color={badColor}>{s.removedWrong}</color>"));
        if (s.ghosts > 0) lines.Add(Row("Пойманные призраки", s.ghosts.ToString()));
        lines.Add(Row("Лучшее комбо", $"{s.bestStreak} (x{s.bestMult:0.#})"));

        // Помощь фамильяров отдельным разделом, у каждого свой цвет
        if (s.familiarCatches.Count > 0)
        {
            lines.Add(Header("ФАМИЛЬЯРЫ"));
            foreach (KeyValuePair<FamiliarDefinition, int> pair in s.familiarCatches)
            {
                string color = "#" + ColorUtility.ToHtmlStringRGB(pair.Key.reportColor);
                lines.Add($"<size=88%><color={color}>• {pair.Key.GetReportLine(pair.Value)}</color></size>");
            }
        }

        lines.Add(Header("ЖАЛОБЫ"));
        string complaints = s.coinsLost > 0
            ? $"<color={badColor}>{s.complaints}  (-{s.coinsLost} {Icon(coinSprite, coinIconColor)})</color>"
            : $"<color={goodColor}>0</color>";
        lines.Add(Row("Всего", complaints));

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
        if (s.unseenByKind.Count > 0)
        {
            lines.Add(Header("СОВЕТ"));
            foreach (KeyValuePair<ParcelKind, int> pair in s.unseenByKind)
            {
                KindRule rule = G.config.GetRule(pair.Key);
                string hint = string.IsNullOrEmpty(rule.upgradeHint) ? "" : $"\n<size=80%>  > {rule.upgradeHint}</size>";
                lines.Add($"<color={hintColor}>Камера не видела: {pair.Value}{hint}</color>");
            }
        }

        return lines;
    }

    // Иконка валюты прямо в строке, в своих цветах (не красится цветом текста)
    private static string Icon(string spriteName, string color) => $"<sprite name=\"{spriteName}\" color={color}>";

    private string FormatTotal(int coins, int ecto)
    {
        string text = $"ИТОГ: {coins:+#;-#;0} {Icon(coinSprite, coinIconColor)}";
        if (ecto > 0) text += $"   +{ecto} {Icon(ectoSprite, ectoIconColor)}";
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
