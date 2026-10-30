using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Концовка после финальной смены: финальный комикс → титры со статистикой → главное меню.
// Игра пройдена, поэтому перед выходом в меню прогресс сбрасывается.
public class EndingView : MonoBehaviour
{
    [SerializeField] private ComicCutsceneUI finalComic;

    [Header("Титры")]
    [SerializeField] private GameObject creditsRoot;
    [Tooltip("Едет снизу вверх. Высота подгоняется под текст")]
    [SerializeField] private RectTransform creditsContent;
    [SerializeField] private TMP_Text creditsText;
    [TextArea(4, 12)]
    [SerializeField] private string credits = "ТЫКВЕННАЯ ПОЧТА\n\n<size=70%>Игра</size>\nxdariko\n\nСпасибо, что сортировали!";
    [SerializeField] private float scrollSpeed = 90f;
    [Tooltip("Через сколько секунд после начала титров клик / пробел / Escape их пропускает")]
    [SerializeField] private float skipDelay = 0.6f;
    [SerializeField] private string mutedColor = "#B9A58E";

    [Header("Реакция короля по жалобам в финальной смене")]
    [SerializeField] private int happyMaxComplaints = 3;
    [SerializeField] private int okMaxComplaints = 10;
    [SerializeField] private string happyTitle = "Король Тыкв в восторге!";
    [SerializeField] private string okTitle = "Король Тыкв доволен";
    [SerializeField] private string grumpyTitle = "Король Тыкв ворчит, но посылку принял";

    private Tween roll;
    private float skipAfter = -1f;

    private void Start()
    {
        G.main.OnPhaseChanged += HandlePhase;
        creditsRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (G.main != null)
            G.main.OnPhaseChanged -= HandlePhase;
        roll?.Kill();
    }

    private void HandlePhase(GamePhase phase)
    {
        if (phase != GamePhase.Ending)
            return;

        string text = BuildCredits(G.shift.Score);
        if (finalComic != null)
            finalComic.Play(() => RollCredits(text));
        else
            RollCredits(text);
    }

    private void RollCredits(string text)
    {
        creditsRoot.SetActive(true);
        creditsText.text = text;

        RectTransform area = (RectTransform)creditsContent.parent;
        float height = creditsText.GetPreferredValues(text, creditsContent.rect.width, 0f).y;
        creditsContent.sizeDelta = new Vector2(creditsContent.sizeDelta.x, height);

        // Пивот контента сверху по центру: стартуем под нижним краем, уезжаем за верхний
        float from = -area.rect.height;
        float to = height;
        creditsContent.anchoredPosition = new Vector2(0f, from);

        roll?.Kill();
        roll = creditsContent.DOAnchorPosY(to, (to - from) / scrollSpeed)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .SetLink(gameObject)
            .OnComplete(Finish);

        skipAfter = Time.unscaledTime + skipDelay;
    }

    private void Update()
    {
        if (skipAfter < 0f || Time.unscaledTime < skipAfter)
            return;

        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        bool pressed = (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                       || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        if (pressed)
            Finish();
    }

    private void Finish()
    {
        if (skipAfter < 0f)
            return;

        skipAfter = -1f;
        roll?.Kill();
        SaveData.ResetAll();
        Scenes.Load(Scenes.Menu);
    }

    private string BuildCredits(ShiftScore s)
    {
        string Row(string label, int value) => $"<color={mutedColor}>{label}</color>  {value}";

        return $"<size=120%>{KingTitle(s.complaints)}</size>\n\n"
               + Row("Ночей отработано", s.night) + "\n"
               + Row("Посылок через ленты", SaveData.TotalParcels) + "\n"
               + Row("Призраков поймано", SaveData.TotalGhosts) + "\n"
               + Row("Жалоб за всю игру", SaveData.TotalComplaints) + "\n"
               + Row("Медяков заработано", SaveData.TotalCoins) + "\n\n\n"
               + credits;
    }

    private string KingTitle(int complaints)
    {
        if (complaints <= happyMaxComplaints) return happyTitle;
        if (complaints <= okMaxComplaints) return okTitle;
        return grumpyTitle;
    }
}
