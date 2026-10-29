using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ведьмина книга между сменами. Здесь будет дерево (шаг 6), пока кнопки «Начать ночь» и «Новая игра».
public class BookView : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text startLabel;
    [SerializeField] private Button newGameButton;
    [SerializeField] private TMP_Text newGameLabel;

    [Tooltip("Сколько секунд кнопка «Новая игра» ждёт второго нажатия")]
    [SerializeField] private float confirmTime = 3f;

    private float confirmUntil;

    private void Start()
    {
        G.main.OnPhaseChanged += HandlePhase;
        startButton.onClick.AddListener(OnStartClicked);
        newGameButton.onClick.AddListener(OnNewGameClicked);
        HandlePhase(G.main.Phase);
    }

    private void OnDestroy()
    {
        if (G.main != null)
            G.main.OnPhaseChanged -= HandlePhase;
        startButton.onClick.RemoveListener(OnStartClicked);
        newGameButton.onClick.RemoveListener(OnNewGameClicked);
    }

    private void HandlePhase(GamePhase phase)
    {
        bool open = phase == GamePhase.Book;
        root.SetActive(open);
        if (!open)
            return;

        titleText.text = "Ведьмина книга";
        startLabel.text = $"Начать ночь {SaveData.Night}";
        newGameLabel.text = "Новая игра";
        confirmUntil = 0f;
    }

    private void Update()
    {
        if (confirmUntil > 0f && Time.unscaledTime > confirmUntil)
        {
            confirmUntil = 0f;
            newGameLabel.text = "Новая игра";
        }
    }

    private void OnStartClicked()
    {
        G.main.BeginNight();
    }

    private void OnNewGameClicked()
    {
        if (confirmUntil > 0f)
        {
            G.main.NewGame();
            return;
        }

        confirmUntil = Time.unscaledTime + confirmTime;
        newGameLabel.text = "Точно? Нажми ещё раз";
    }
}
