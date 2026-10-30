using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Главное меню: играть / продолжить, новая игра, звук, автор и ссылка.
public class MainMenuView : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private TMP_Text playLabel;
    [SerializeField] private Button newGameButton;
    [SerializeField] private TMP_Text newGameLabel;

    [Header("Настройки звука")]
    [SerializeField] private Button settingsButton;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button settingsCloseButton;

    [Header("Автор")]
    [SerializeField] private TMP_Text authorText;
    [SerializeField] private string authorName = "xdariko";
    [SerializeField] private Button linkButton;
    [Tooltip("Пусто = кнопка ссылки скрыта")]
    [SerializeField] private string linkUrl = "";

    [Tooltip("Сколько секунд кнопка «Новая игра» ждёт второго нажатия")]
    [SerializeField] private float confirmTime = 3f;

    private float confirmUntil;

    private void Start()
    {
        bool hasProgress = SaveData.HasProgress;
        playLabel.text = hasProgress ? $"Продолжить · ночь {SaveData.Night}" : "Играть";
        newGameButton.gameObject.SetActive(hasProgress);
        newGameLabel.text = "Новая игра";
        settingsPanel.SetActive(false);
        if (authorText != null)
            authorText.text = authorName;

        playButton.onClick.AddListener(OnPlayClicked);
        newGameButton.onClick.AddListener(OnNewGameClicked);
        settingsButton.onClick.AddListener(ToggleSettings);
        if (settingsCloseButton != null)
            settingsCloseButton.onClick.AddListener(ToggleSettings);
        if (linkButton != null)
        {
            linkButton.gameObject.SetActive(!string.IsNullOrEmpty(linkUrl));
            linkButton.onClick.AddListener(OnLinkClicked);
        }
    }

    private void OnDestroy()
    {
        playButton.onClick.RemoveListener(OnPlayClicked);
        newGameButton.onClick.RemoveListener(OnNewGameClicked);
        settingsButton.onClick.RemoveListener(ToggleSettings);
        if (settingsCloseButton != null)
            settingsCloseButton.onClick.RemoveListener(ToggleSettings);
        if (linkButton != null)
            linkButton.onClick.RemoveListener(OnLinkClicked);
    }

    private void Update()
    {
        if (confirmUntil > 0f && Time.unscaledTime > confirmUntil)
        {
            confirmUntil = 0f;
            newGameLabel.text = "Новая игра";
        }
    }

    private void OnPlayClicked()
    {
        Scenes.Load(Scenes.Game);
    }

    private void OnNewGameClicked()
    {
        if (confirmUntil > 0f)
        {
            // Сброс сделает Main в сцене Game: дерево и кошелёк живут там
            SaveData.NewGameRequested = true;
            Scenes.Load(Scenes.Game);
            return;
        }

        confirmUntil = Time.unscaledTime + confirmTime;
        newGameLabel.text = "Точно? Нажми ещё раз";
    }

    private void ToggleSettings()
    {
        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    private void OnLinkClicked()
    {
        Application.OpenURL(linkUrl);
    }
}
